using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;
using CommonMessage = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IMongoCollection<Room> _collection;
        private readonly IMongoCollection<Floor> _floorCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<ItemFee> _itemFeeCollection;
        private readonly IValidator<RoomRequest> _validator;
        private readonly IMapper _mapper;

        public RoomsController(
            MongoDbService mongoService,
            IValidator<RoomRequest> validator,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Room>("rooms");
            _floorCollection = db.GetCollection<Floor>("floors");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _userCollection = db.GetCollection<User>("users");
            _itemFeeCollection = db.GetCollection<ItemFee>("itemfees");
            _validator = validator;
            _mapper = mapper;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var users = await _userCollection.Find(u => u.Role != "Admin").ToListAsync();
            var itemFees = await _itemFeeCollection.Find(f => f.IsActive).ToListAsync();
            var activeContracts = await _contractCollection.Find(c => c.Status == ContractStatus.Active).ToListAsync();

            var floorDict = floors.GroupBy(f => f.Id).ToDictionary(g => g.Key, g => g.First());
            var userDict = users.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());
            var itemFeeDict = itemFees.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.First());
            var contractDict = activeContracts.GroupBy(c => c.RoomId).ToDictionary(g => g.Key, g => g.First());

            List<Room> rooms;
            int total;

            if (page.HasValue && limit.HasValue)
            {
                int p = page.Value < 1 ? 1 : page.Value;
                int l = limit.Value < 1 ? 10 : limit.Value;
                total = (int)await _collection.CountDocumentsAsync(_ => true);
                rooms = await _collection.Find(_ => true)
                    .Skip((p - 1) * l)
                    .Limit(l)
                    .ToListAsync();
            }
            else
            {
                rooms = await _collection.Find(_ => true).ToListAsync();
                total = rooms.Count;
            }

            var result = rooms.Select(room => MapToResponse(room, floorDict, userDict, itemFeeDict, contractDict)).ToList();

            if (page.HasValue && limit.HasValue)
            {
                return Ok(new PagedResult<RoomResponse>
                {
                    Total = total,
                    Page = page.Value,
                    Limit = limit.Value,
                    Items = result
                });
            }
            else
            {
                return Ok(result);
            }
        }

        // GET: api/Rooms/manage-init
        [HttpGet("manage-init")]
        public async Task<ActionResult> GetManageInitData()
        {
            // 1. Bulk load metadata
            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var users = await _userCollection.Find(u => u.Role != "Admin").ToListAsync();
            var itemFees = await _itemFeeCollection.Find(f => f.IsActive).ToListAsync();
            var activeContracts = await _contractCollection.Find(c => c.Status == ContractStatus.Active).ToListAsync();

            var floorDict = floors.GroupBy(f => f.Id).ToDictionary(g => g.Key, g => g.First());
            var userDict = users.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());
            var itemFeeDict = itemFees.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.First());
            var contractDict = activeContracts.GroupBy(c => c.RoomId).ToDictionary(g => g.Key, g => g.First());

            // 2. Query Rooms
            var rooms = await _collection.Find(_ => true).ToListAsync();
            var roomResponses = rooms.Select(room => MapToResponse(room, floorDict, userDict, itemFeeDict, contractDict)).ToList();

            // 3. Format floor list for response
            var floorResponses = floors.Select(floor =>
            {
                var activeRooms = rooms.Where(r => r.IsActive).ToList();
                var roomsOnFloor = activeRooms.Where(r => r.FloorId == floor.Id).ToList();
                var occupiedRooms = roomsOnFloor.Count(r => r.Status == RoomStatus.Occupied);
                var emptyRooms = roomsOnFloor.Count(r => r.Status == RoomStatus.Available);

                return new FloorItemResponse
                {
                    Id = floor.Id,
                    FloorNumber = floor.FloorNumber,
                    Name = floor.Name,
                    Description = floor.Description,
                    RoomCount = roomsOnFloor.Count,
                    OccupiedRooms = occupiedRooms,
                    EmptyRooms = emptyRooms,
                    RevenueOnFloor = roomsOnFloor
                        .Where(r => r.Status == RoomStatus.Occupied)
                        .Sum(r => r.Price)
                };
            }).ToList();

            var tenantResponses = users.Select(_mapper.Map<UserResponse>).ToList();
            var filteredItemFees = itemFees.Where(f => f.Type != "mandatory").ToList();
            var itemFeeResponses = _mapper.Map<List<ItemFeeResponse>>(filteredItemFees);

            return Ok(new
            {
                Rooms = roomResponses,
                Floors = floorResponses,
                Tenants = tenantResponses,
                ItemFees = itemFeeResponses
            });
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomResponse>> GetById(string id)
        {
            var room = await _collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            return Ok(await MapToResponse(room));
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<RoomResponse>> Create(RoomRequest request)
        {
            var errors = await ValidateRequest(request);

            var floor = await _floorCollection
                .Find(x => x.Id == request.FloorId)
                .FirstOrDefaultAsync();
            if (floor is null)
                errors.Add(CommonMessage.NotFound("Tầng"));

            // 1. Kiểm tra xem phòng với số phòng này đã tồn tại hay chưa 
            var existingRoom = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .FirstOrDefaultAsync();

            if (existingRoom != null)
            {
                if (existingRoom.IsActive && existingRoom.Status != RoomStatus.InActive)
                {
                    // Nếu phòng đang hoạt động mà trùng số thì báo lỗi đã tồn tại
                    errors.Add(CommonMessage.RoomNumberExists());
                }
            }

            if (errors.Any())
                return BadRequest(errors);

            // Nếu phòng đã tồn tại nhưng không hoạt động thì tiến hành tái sử dụng (Update) và kích hoạt lại
            if (existingRoom != null && (!existingRoom.IsActive || existingRoom.Status == RoomStatus.InActive))
            {
                // Map dữ liệu mới từ request vào phòng cũ
                _mapper.Map(request, existingRoom);

                // Kích hoạt lại phòng và cập nhật thời gian
                existingRoom.IsActive = true;
                existingRoom.Status = RoomStatus.Available;
                existingRoom.UpdatedAt = DateTime.UtcNow;

                await _collection.ReplaceOneAsync(x => x.Id == existingRoom.Id, existingRoom);

                return Ok(await MapToResponse(existingRoom));
            }

            // Nếu chưa tồn tại hoàn toàn thì thực hiện thêm mới (Insert) như bình thường
            var room = _mapper.Map<Room>(request);
            room.CreatedAt = DateTime.UtcNow;
            room.IsActive = true; // Đảm bảo tạo mới mặc định là true

            await _collection.InsertOneAsync(room);

            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                await MapToResponse(room));
        }

        // PUT: api/Rooms/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<RoomResponse>> Update(string id, RoomRequest request)
        {
            var errors = await ValidateRequest(request);

            var existingRoom = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingRoom is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            var roomNumberExists = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber && x.Id != id)
                .AnyAsync();
            if (roomNumberExists)
                errors.Add(CommonMessage.RoomNumberExists());

            var floor = await _floorCollection
                .Find(x => x.Id == request.FloorId)
                .FirstOrDefaultAsync();
            if (floor is null)
                errors.Add(CommonMessage.NotFound("Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            _mapper.Map(request, existingRoom);
            existingRoom.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, existingRoom);

            return Ok(await MapToResponse(existingRoom));
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var room = await _collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            // Kiểm tra hợp đồng đang hiệu lực theo RoomId (chính xác hơn RoomNumber)
            var activeContractExists = await _contractCollection
                .Find(c => c.RoomId == id && c.Status == ContractStatus.Active)
                .AnyAsync();

            if (activeContractExists)
                return BadRequest(CommonMessage.RoomHasActiveContract());

            var update = Builders<Room>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.Status, RoomStatus.InActive)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(x => x.Id == id, update);

            return Ok(CommonMessage.Deleted("Phòng"));
        }

        // PUT: api/Rooms/{id}/reactivate
        [HttpPut("{id}/reactivate")]
        public async Task<ActionResult<RoomResponse>> Reactivate(string id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            var update = Builders<Room>.Update
                .Set(x => x.IsActive, true)
                .Set(x => x.Status, RoomStatus.Available)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(x => x.Id == id, update);
            room.IsActive = true;
            room.Status = RoomStatus.Available;

            return Ok(await MapToResponse(room));
        }

        // ==================== HELPERS ====================

        private RoomResponse MapToResponse(
            Room room,
            Dictionary<string, Floor> floorDict,
            Dictionary<string, User> userDict,
            Dictionary<string, ItemFee> itemFeeDict,
            Dictionary<string, Contract> contractDict)
        {
            var response = _mapper.Map<RoomResponse>(room);

            if (room.Amenities != null && room.Amenities.Any())
            {
                response.Amenities = room.Amenities
                    .Select(type => itemFeeDict.TryGetValue(type, out var fee) ? fee : null)
                    .Where(fee => fee != null)
                    .Select(fee => new RoomAmenityResponse
                    {
                        Name = fee!.Name,
                        Price = fee.Price,
                        Unit = fee.Unit,
                        Type = fee.Type
                    }).ToList();
            }

            if (!string.IsNullOrEmpty(room.FloorId) && floorDict.TryGetValue(room.FloorId, out var floor))
            {
                response.FloorNumber = floor.FloorNumber;
            }

            if (!room.IsActive)
            {
                response.StatusLabel = "Không hoạt động";
            }
            else
            {
                response.StatusLabel = room.Status switch
                {
                    RoomStatus.Available => "Trống",
                    RoomStatus.Occupied => "Đã thuê",
                    RoomStatus.InActive => "Không hoạt động",
                    _ => room.Status.ToString()
                };
            }

            if (!string.IsNullOrEmpty(room.TenantId) && userDict.TryGetValue(room.TenantId, out var tenant))
            {
                response.TenantName = tenant.Name;
            }
            else if (contractDict.TryGetValue(room.Id, out var activeContract))
            {
                if (!string.IsNullOrEmpty(activeContract.TenantId) && userDict.TryGetValue(activeContract.TenantId, out var tenantFromContract))
                {
                    response.TenantName = tenantFromContract.Name;
                }
                else
                {
                    response.TenantName = activeContract.TenantName;
                }
            }

            return response;
        }

        private async Task<List<string>> ValidateRequest(RoomRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            return validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private async Task<RoomResponse> MapToResponse(Room room)
        {
            var response = _mapper.Map<RoomResponse>(room);

            if (room.Amenities != null && room.Amenities.Any())
            {
                var matchingFees = await _itemFeeCollection
                    .Find(f => room.Amenities.Contains(f.Type) && f.IsActive)
                    .ToListAsync();

                response.Amenities = matchingFees.Select(f => new RoomAmenityResponse
                {
                    Name = f.Name,
                    Price = f.Price,
                    Unit = f.Unit,
                    Type = f.Type
                }).ToList();
            }

            // Lấy Floor theo Id
            if (!string.IsNullOrEmpty(room.FloorId))
            {
                var floor = await _floorCollection
                    .Find(f => f.Id == room.FloorId)
                    .FirstOrDefaultAsync();
                response.FloorNumber = floor?.FloorNumber ?? 0;
            }

            if (!room.IsActive)
            {
                response.StatusLabel = "Không hoạt động";
            }
            else
            {
                response.StatusLabel = room.Status switch
                {
                    RoomStatus.Available => "Trống",
                    RoomStatus.Occupied => "Đã thuê",
                    RoomStatus.InActive => "Không hoạt động",
                    _ => room.Status.ToString()
                };
            }

            // Lấy Tenant theo TenantId (ưu tiên) hoặc qua hợp đồng active
            if (!string.IsNullOrEmpty(room.TenantId))
            {
                var tenant = await _userCollection
                    .Find(u => u.Id == room.TenantId)
                    .FirstOrDefaultAsync();
                response.TenantName = tenant?.Name;
            }
            else
            {
                var activeContract = await _contractCollection
                    .Find(c => c.RoomId == room.Id && c.Status == ContractStatus.Active)
                    .FirstOrDefaultAsync();

                if (activeContract != null && !string.IsNullOrEmpty(activeContract.TenantId))
                {
                    var tenant = await _userCollection
                        .Find(u => u.Id == activeContract.TenantId)
                        .FirstOrDefaultAsync();
                    response.TenantName = tenant?.Name ?? activeContract.TenantName;
                }
            }

            return response;
        }
    }
}
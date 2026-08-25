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
            if (page.HasValue && limit.HasValue)
            {
                int p = page.Value < 1 ? 1 : page.Value;
                int l = limit.Value < 1 ? 10 : limit.Value;
                var total = await _collection.CountDocumentsAsync(_ => true);
                var rooms = await _collection.Find(_ => true)
                    .Skip((p - 1) * l)
                    .Limit(l)
                    .ToListAsync();
                var result = new List<RoomResponse>();
                foreach (var room in rooms)
                {
                    result.Add(await MapToResponseAsync(room));
                }
                return Ok(new PagedResult<RoomResponse>
                {
                    Total = (int)total,
                    Page = p,
                    Limit = l,
                    Items = result
                });
            }
            else
            {
                var rooms = await _collection.Find(_=>true).ToListAsync();
                var result = new List<RoomResponse>();
                foreach (var room in rooms)
                {
                    result.Add(await MapToResponseAsync(room));
                }
                return Ok(result);
            }
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomResponse>> GetById(string id)
        {
            var room = await _collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            return Ok(await MapToResponseAsync(room));
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

                return Ok(await MapToResponseAsync(existingRoom));
            }

            // Nếu chưa tồn tại hoàn toàn thì thực hiện thêm mới (Insert) như bình thường
            var room = _mapper.Map<Room>(request);
            room.CreatedAt = DateTime.UtcNow;
            room.IsActive = true; // Đảm bảo tạo mới mặc định là true

            await _collection.InsertOneAsync(room);

            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                await MapToResponseAsync(room));
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

            return Ok(await MapToResponseAsync(existingRoom));
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

            return Ok(await MapToResponseAsync(room));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(RoomRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            return validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private async Task<RoomResponse> MapToResponseAsync(Room room)
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
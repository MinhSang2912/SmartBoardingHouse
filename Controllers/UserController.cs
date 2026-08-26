using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Models.Settings;
using SmartBoardingHouse.Services;
using static SmartBoardingHouse.Common.Enums;
using Message = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<UserRequest> _validator;
        private readonly IOptions<AdminSettings> _adminSettings;
        private readonly PhotoService _photoService;
        private readonly IMapper _mapper;
        private readonly EmailService _emailService;

        public UsersController(
            MongoDbService mongoService,
            IValidator<UserRequest> validator,
            IOptions<AdminSettings> adminSettings,
            PhotoService photoService,
            IMapper mapper,
            EmailService emailService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<User>("users");
            _roomCollection = db.GetCollection<Room>("rooms");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _validator = validator;
            _adminSettings = adminSettings;
            _photoService = photoService;
            _mapper = mapper;
            _emailService = emailService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            // 1. Fetch users
            List<User> users;
            int total = 0;
            if (page.HasValue && limit.HasValue)
            {
                int p = page.Value < 1 ? 1 : page.Value;
                int l = limit.Value < 1 ? 10 : limit.Value;
                total = (int)await _collection.CountDocumentsAsync(x => x.Role != "Admin");
                users = await _collection.Find(x => x.Role != "Admin")
                    .Skip((p - 1) * l)
                    .Limit(l)
                    .ToListAsync();
            }
            else
            {
                users = await _collection.Find(x => x.Role != "Admin").ToListAsync();
                total = users.Count;
            }

            // 2. Bulk load active contracts and rooms to solve N+1 queries
            var userIds = users.Select(u => u.Id).ToList();
            var activeContracts = await _contractCollection
                .Find(c => userIds.Contains(c.TenantId) && c.Status == ContractStatus.Active)
                .ToListAsync();

            var roomIds = activeContracts.Select(c => c.RoomId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var rooms = await _roomCollection.Find(r => roomIds.Contains(r.Id)).ToListAsync();

            var roomDict = rooms.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
            var contractGroup = activeContracts.GroupBy(c => c.TenantId).ToDictionary(g => g.Key, g => g.ToList());

            var responses = users.Select(user =>
            {
                var response = _mapper.Map<UserResponse>(user);

                if (contractGroup.TryGetValue(user.Id, out var userContracts))
                {
                    var uRoomIds = userContracts.Select(c => c.RoomId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                    var uRooms = uRoomIds
                        .Select(rid => roomDict.TryGetValue(rid, out var rm) ? rm : null)
                        .Where(rm => rm != null)
                        .ToList();

                    response.ActiveRoomIds = uRooms.Select(r => r!.Id).ToList();
                    response.ActiveRoomNumbers = uRooms
                        .Select(r => r!.RoomNumber)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();

                    response.ActiveRoomCount = response.ActiveRoomNumbers.Count;
                    response.RoomNumber = response.ActiveRoomCount > 0
                        ? string.Join(", ", response.ActiveRoomNumbers)
                        : "Chưa có phòng";
                }
                else
                {
                    response.ActiveRoomCount = 0;
                    response.ActiveRoomNumbers = new List<string>();
                    response.ActiveRoomIds = new List<string>();
                    response.RoomNumber = "Chưa có phòng";
                }

                return response;
            }).ToList();

            if (page.HasValue && limit.HasValue)
            {
                return Ok(new PagedResult<UserResponse>
                {
                    Total = total,
                    Page = page.Value,
                    Limit = limit.Value,
                    Items = responses
                });
            }
            else
            {
                return Ok(responses);
            }
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetById(string id)
        {
            var user = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound(Message.NotFound("Người dùng"));

            return Ok(await MapToResponseAsync(user));
        }

        // POST: api/Users
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<UserResponse>> Create([FromForm] UserRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var emailExists = await _collection.Find(x => x.Email == request.Email).AnyAsync();
            if (emailExists)
                errors.Add("Email đã tồn tại");

            if (errors.Any())
                return BadRequest(errors);

            var user = _mapper.Map<User>(request);
            user.Password = PasswordHelper.Hash(request.Password);
            user.Role = "Tenant";
            user.CreatedAt = DateTime.UtcNow;

            var temp = request.IDCard;
            // Upload ảnh
            if (request.FrontImage != null)
                user.FrontImageUrl = await _photoService.SaveFrontIdCardAsync(request.FrontImage, temp);
            if (request.BackImage != null)
                user.BackImageUrl = await _photoService.SaveBackIdCardAsync(request.BackImage, temp);

            await _collection.InsertOneAsync(user);

            // Gửi thông tin tài khoản qua email trong tác vụ nền (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendTenantAccountAsync(user.Email, user.Name, request.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Email Error] Lỗi gửi email tới {user.Email}: {ex.Message}");
                }
            });

            return CreatedAtAction(nameof(GetById), new { id = user.Id },
                await MapToResponseAsync(user));
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<UserResponse>> Update(string id, [FromForm] UserRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(errors);
            }

            var existingUser = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (existingUser is null)
                return NotFound(Message.NotFound("Người dùng"));

            // Map các field text (Name, Email, PhoneNumber, IDCard, Address, DateOfBirth...)
            _mapper.Map(request, existingUser);

            existingUser.UpdatedAt = DateTime.UtcNow;

            // Chỉ hash password khi client gửi mật khẩu mới
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                existingUser.Password = PasswordHelper.Hash(request.Password);
            }

            var temp = request.IDCard;
            // Xử lý ảnh riêng
            if (request.FrontImage != null)
            {
                await _photoService.DeletePhotoAsync(existingUser.FrontImageUrl);
                existingUser.FrontImageUrl = await _photoService.SaveFrontIdCardAsync(request.FrontImage, temp);
            }

            if (request.BackImage != null)
            {
                await _photoService.DeletePhotoAsync(existingUser.BackImageUrl);
                existingUser.BackImageUrl = await _photoService.SaveBackIdCardAsync(request.BackImage, temp);
            }

            await _collection.ReplaceOneAsync(x => x.Id == id, existingUser);

            return Ok(await MapToResponseAsync(existingUser));
        }

        // DELETE: api/Users/{id} 
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound(Message.NotFound("Người dùng"));

            // Kiểm tra còn hợp đồng đang hiệu lực không
            var hasActiveContract = await _contractCollection
                .Find(x => x.TenantId == id && x.Status == ContractStatus.Active)
                .AnyAsync();

            if (hasActiveContract)
                return BadRequest(Message.UserHasActiveContract());

            // Soft delete
            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<User>.Update
                    .Set(x => x.IsActive, false)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            return Ok(Message.Deleted("Người dùng"));
        }

        // PUT: api/Users/{id}/reactivate
        [HttpPut("{id}/reactivate")]
        public async Task<ActionResult<UserResponse>> Reactivate(string id)
        {
            var user = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound(Message.NotFound("Người dùng"));

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<User>.Update
                    .Set(x => x.IsActive, true)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            user.IsActive = true;

            return Ok(await MapToResponseAsync(user));
        }

        // ==================== HELPERS ====================
        private async Task<UserResponse> MapToResponseAsync(User user)
        {
            var response = _mapper.Map<UserResponse>(user);

            // Lấy tất cả hợp đồng đang Active của người này
            var activeContracts = await _contractCollection
                .Find(c => c.TenantId == user.Id && c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Any())
            {
                var roomIds = activeContracts
                    .Select(c => c.RoomId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                // Lấy thông tin phòng
                var rooms = await _roomCollection
                    .Find(r => roomIds.Contains(r.Id))
                    .ToListAsync();

                response.ActiveRoomIds = rooms.Select(r => r.Id).ToList();
                response.ActiveRoomNumbers = rooms
                    .Select(r => r.RoomNumber)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                response.ActiveRoomCount = response.ActiveRoomNumbers.Count;

                // Giữ tương thích với field cũ (nếu frontend vẫn dùng)
                response.RoomNumber = response.ActiveRoomCount > 0
                    ? string.Join(", ", response.ActiveRoomNumbers)
                    : "Chưa có phòng";
            }
            else
            {
                response.ActiveRoomCount = 0;
                response.ActiveRoomNumbers = new List<string>();
                response.ActiveRoomIds = new List<string>();
                response.RoomNumber = "Chưa có phòng";
            }

            return response;
        }
    }
}
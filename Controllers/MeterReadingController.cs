using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using static SmartBoardingHouse.Common.Enums;
using CommonMessage = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMongoCollection<MeterReading> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<MeterReadingRequest> _validator;
        private readonly IMapper _mapper;
        private readonly PhotoService _photoService;

        private const decimal ElectricUnitPrice = 3000m;
        private const decimal WaterUnitPrice = 10000m;

        public MeterReadingsController(
            MongoDbService mongoService,
            IValidator<MeterReadingRequest> validator,
            IMapper mapper,
            PhotoService photoService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<MeterReading>("meterreadings");
            _roomCollection = db.GetCollection<Room>("rooms");
            _userCollection = db.GetCollection<User>("users");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _validator = validator;
            _mapper = mapper;
            _photoService = photoService;
        }

        // GET: api/MeterReadings
        [HttpGet]
        public async Task<ActionResult<List<MeterReadingResponse>>> GetAll()
        {
            var readings = await _collection
                .Find(_ => true)
                .SortByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            var result = new List<MeterReadingResponse>();
            foreach (var reading in readings)
            {
                result.Add(await MapToResponseAsync(reading));
            }

            return Ok(result);
        }

        // GET: api/MeterReadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReadingResponse>> GetById(string id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (reading is null)
                return NotFound(CommonMessage.NotFound("Chỉ số công tơ"));

            return Ok(await MapToResponseAsync(reading));
        }

        // GET: api/MeterReadings/room/{roomId}
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<List<MeterReadingResponse>>> GetByRoom(string roomId)
        {
            var readings = await _collection
                .Find(x => x.RoomId == roomId)
                .SortByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            var result = new List<MeterReadingResponse>();
            foreach (var reading in readings)
            {
                result.Add(await MapToResponseAsync(reading));
            }

            return Ok(result);
        }

        // POST: api/MeterReadings
        //[HttpPost]
        //[Consumes("multipart/form-data")]
        //public async Task<ActionResult<MeterReadingResponse>> Create([FromForm] MeterReadingRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    // Tìm phòng theo RoomNumber
        //    var room = await _roomCollection
        //        .Find(x => x.RoomNumber == request.RoomNumber)
        //        .FirstOrDefaultAsync();

        //    if (room is null)
        //        return BadRequest(CommonMessage.NotFound("Phòng"));

        //    // Kiểm tra trùng tháng + loại
        //    var exists = await _collection
        //        .Find(x => x.RoomId == room.Id
        //                 && x.Type == request.Type
        //                 && x.Month == request.Month
        //                 && x.Year == request.Year)
        //        .AnyAsync();

        //    if (exists)
        //        errors.Add($"Đã có chỉ số {GetTypeLabel(request.Type)} tháng {request.Month}/{request.Year} cho phòng {request.RoomNumber}");

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    // Lấy chỉ số tháng trước
        //    var previous = await GetPreviousReading(room.Id, request.Type, request.Month, request.Year);
        //    var previousIndex = previous?.CurrentIndex ?? 0;

        //    if (request.CurrentIndex < previousIndex)
        //        return BadRequest($"Chỉ số mới ({request.CurrentIndex}) phải >= chỉ số tháng trước ({previousIndex})");

        //    // Xử lý ảnh
        //    string? photoUrl = null;
        //    if (request.Photo is not null)
        //    {
        //        photoUrl = await _photoService.SaveMaintenancePhotoAsync(request.Photo, room.RoomNumber, "MeterReadings");
        //    }

        //    var unitPrice = request.Type == MeterType.Electric ? ElectricUnitPrice : WaterUnitPrice;
        //    var usage = request.CurrentIndex - previousIndex;

        //    var reading = new MeterReading
        //    {
        //        RoomId = room.Id,
        //        RoomNumber = room.RoomNumber,
        //        TenantId = room.TenantId ?? string.Empty,
        //        Type = request.Type,
        //        Month = request.Month,
        //        Year = request.Year,
        //        PreviousIndex = previousIndex,
        //        CurrentIndex = request.CurrentIndex,
        //        Usage = usage,
        //        UnitPrice = unitPrice,
        //        TotalCost = (decimal)usage * unitPrice,
        //        PhotoUrl = photoUrl,
        //        ReadingDate = DateTime.UtcNow,
        //        CreatedAt = DateTime.UtcNow,
        //        IsVerified = false
        //    };

        //    await _collection.InsertOneAsync(reading);

        //    return CreatedAtAction(nameof(GetById), new { id = reading.Id },
        //        await MapToResponseAsync(reading));
        //}

        // ==================== HELPERS ====================

        //private async Task<List<string>> ValidateRequest(MeterReadingRequest request)
        //{
        //    var result = await _validator.ValidateAsync(request);
        //    return result.Errors.Select(e => e.ErrorMessage).ToList();
        //}

        //private async Task<MeterReading?> GetPreviousReading(
        //    string roomId, MeterType type, int month, int year)
        //{
        //    var prevMonth = month == 1 ? 12 : month - 1;
        //    var prevYear = month == 1 ? year - 1 : year;

        //    return await _collection
        //        .Find(x => x.RoomId == roomId
        //                 && x.Type == type
        //                 && x.Month == prevMonth
        //                 && x.Year == prevYear)
        //        .FirstOrDefaultAsync();
        //}

        private static string GetTypeLabel(MeterType type) => type switch
        {
            MeterType.Electric => "Điện",
            MeterType.Water => "Nước",
            _ => type.ToString()
        };

        private async Task<MeterReadingResponse> MapToResponseAsync(MeterReading reading)
        {
            var response = _mapper.Map<MeterReadingResponse>(reading);

            // Lấy RoomNumber từ Room theo RoomId
            if (!string.IsNullOrEmpty(reading.RoomId))
            {
                var room = await _roomCollection
                    .Find(r => r.Id == reading.RoomId)
                    .FirstOrDefaultAsync();

                if (room != null)
                    response.RoomNumber = room.RoomNumber;
            }

            // Lấy TenantName từ User theo TenantId
            if (!string.IsNullOrEmpty(reading.TenantId))
            {
                var tenant = await _userCollection
                    .Find(u => u.Id == reading.TenantId)
                    .FirstOrDefaultAsync();

                response.TenantName = tenant?.Name ?? string.Empty;
            }
            else
            {
                // Fallback: tìm hợp đồng active theo RoomId
                var contract = await _contractCollection
                    .Find(c => c.RoomId == reading.RoomId && c.Status == ContractStatus.Active)
                    .FirstOrDefaultAsync();

                response.TenantName = contract?.TenantName ?? string.Empty;
            }

            response.TypeLabel = GetTypeLabel(reading.Type);
            response.Period = reading.ReadingDate.ToString("d/M/yyyy");

            var unit = reading.Type == MeterType.Electric ? "kWh" : "m³";
            response.UnitPrice = reading.UnitPrice > 0
                ? reading.UnitPrice
                : (reading.Type == MeterType.Electric ? ElectricUnitPrice : WaterUnitPrice);

            response.Total = reading.TotalCost > 0
                ? reading.TotalCost
                : (decimal)reading.Usage * response.UnitPrice;

            response.UsageLabel = $"{reading.Usage} {unit}";

            return response;
        }
    }
}
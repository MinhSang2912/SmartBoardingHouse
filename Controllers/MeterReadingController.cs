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
        public async Task<ActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            if (page.HasValue && limit.HasValue)
            {
                int p = page.Value < 1 ? 1 : page.Value;
                int l = limit.Value < 1 ? 10 : limit.Value;
                var total = await _collection.CountDocumentsAsync(_ => true);
                var readings = await _collection.Find(_ => true)
                    .SortByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .Skip((p - 1) * l)
                    .Limit(l)
                    .ToListAsync();
                var responses = await MapToResponseListAsync(readings);
                return Ok(new PagedResult<MeterReadingResponse>
                {
                    Total = (int)total,
                    Page = p,
                    Limit = l,
                    Items = responses
                });
            }
            else
            {
                var readings = await _collection
                    .Find(_ => true)
                    .SortByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();
                return Ok(await MapToResponseListAsync(readings));
            }
        }

        // GET: api/MeterReadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReadingResponse>> GetById(string id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (reading is null)
                return NotFound(CommonMessage.NotFound("Chỉ số công tơ"));

            var result = await MapToResponseListAsync(new List<MeterReading> { reading });
            return Ok(result.First());
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

            return Ok(await MapToResponseListAsync(readings));
        }

        private static string GetTypeLabel(MeterType type) => type switch
        {
            MeterType.Electric => "Điện",
            MeterType.Water => "Nước",
            _ => type.ToString()
        };

        /// <summary>
        /// Hàm ánh xạ hàng loạt giúp giải quyết triệt để lỗi N+1 Query Problem
        /// </summary>
        private async Task<List<MeterReadingResponse>> MapToResponseListAsync(List<MeterReading> readings)
        {
            if (readings == null || readings.Count == 0)
                return new List<MeterReadingResponse>();

            // 1. Gom tất cả RoomId và TenantId cần lấy thông tin (loại bỏ giá trị null/trùng lặp)
            var roomIds = readings.Where(r => !string.IsNullOrEmpty(r.RoomId)).Select(r => r.RoomId).Distinct().ToList();
            var tenantIds = readings.Where(r => !string.IsNullOrEmpty(r.TenantId)).Select(r => r.TenantId).Distinct().ToList();

            // 2. Truy vấn Database MỘT LẦN duy nhất cho toàn bộ Rooms và Users
            var roomsTask = _roomCollection.Find(r => roomIds.Contains(r.Id)).ToListAsync();
            var tenantsTask = _userCollection.Find(u => tenantIds.Contains(u.Id)).ToListAsync();

            await Task.WhenAll(roomsTask, tenantsTask);

            // Chuyển sang Dictionary để tra cứu O(1) trong bộ nhớ RAM cực nhanh
            var roomDict = roomsTask.Result.ToDictionary(r => r.Id, r => r.RoomNumber);
            var tenantDict = tenantsTask.Result.ToDictionary(u => u.Id, u => u.Name);

            // 3. Tiến hành map dữ liệu
            var result = new List<MeterReadingResponse>(readings.Count);
            foreach (var reading in readings)
            {
                var response = _mapper.Map<MeterReadingResponse>(reading);

                if (!string.IsNullOrEmpty(reading.RoomId) && roomDict.TryGetValue(reading.RoomId, out var roomNumber))
                {
                    response.RoomNumber = roomNumber;
                }

                if (!string.IsNullOrEmpty(reading.TenantId) && tenantDict.TryGetValue(reading.TenantId, out var tenantName))
                {
                    response.TenantName = tenantName;
                }
                else
                {
                    response.TenantName = string.Empty;
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

                result.Add(response);
            }

            return result;
        }
    }
}
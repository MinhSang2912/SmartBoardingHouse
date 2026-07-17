using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common.Message;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMongoCollection<MeterReading> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
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

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            var result = readings.Select(r => MapToResponse(r, contracts)).ToList();

            return Ok(result);
        }

        // GET: api/MeterReadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReadingResponse>> GetById(string id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (reading is null)
                return NotFound(CommonMessage.NotFound("Số công tơ"));

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            return Ok(MapToResponse(reading, contracts));
        }

        // GET: api/MeterReadings/room/{roomNumber}
        [HttpGet("room/{roomNumber}")]
        public async Task<ActionResult<List<MeterReadingResponse>>> GetByRoom(string roomNumber)
        {
            var readings = await _collection
                .Find(x => x.RoomNumber == roomNumber)
                .SortByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            var result = readings.Select(r => MapToResponse(r, contracts)).ToList();

            return Ok(result);
        }

        // POST: api/MeterReadings
        //[HttpPost]
        //[Consumes("multipart/form-data")]
        //public async Task<ActionResult<MeterReadingResponse>> Create([FromForm] MeterReadingRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    // Kiểm tra phòng tồn tại
        //    var room = await _roomCollection
        //        .Find(x => x.RoomNumber == request.RoomNumber)
        //        .FirstOrDefaultAsync();
        //    if (room == null)
        //        errors.Add(CommonMessage.NotFound("Phòng"));
        //    else if (room.Status != RoomStatus.Occupied)
        //        errors.Add(CommonMessage.MeterReadingRoomNotOccupied());
            
        //    var now = DateTime.Now;

        //    // Kiểm tra đã có chỉ số cùng loại trong tháng này chưa
        //    var duplicate = await _collection
        //        .Find(x => x.RoomNumber == request.RoomNumber
        //                 && x.Type == request.Type
        //                 && x.Month == now.Month
        //                 && x.Year == now.Year)
        //        .AnyAsync();
        //    if (duplicate)
        //        errors.Add(CommonMessage.MeterReadingAlreadyExists());

        //    // Lấy chỉ số tháng trước cùng loại
        //    var prevReading = await GetPreviousReading(request.RoomNumber, request.Type, now.Month, now.Year);
        //    if (prevReading is not null && request.CurrentIndex < prevReading.CurrentIndex)
        //        errors.Add(CommonMessage.MeterReadingThisMonthMuchHighterLastMonth());

        //    // Lưu ảnh
        //    string? photoUrl = null;
        //    if (request.Photo is not null)
        //    {
        //        try
        //        {
        //            photoUrl = await _photoService.SaveMeterPhotoAsync(request.Photo, "default");
        //        }
        //        catch (ArgumentException ex)
        //        {
        //            errors.Add(ex.Message);
        //        }
        //    }

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var contract = await _contractCollection
        //        .Find(c => c.RoomNumber == request.RoomNumber && c.Status == ContractStatus.Active)
        //        .FirstOrDefaultAsync();
        //    if (contract is null)
        //        return BadRequest(CommonMessage.MaintenanceRoomNotOccupied());

        //    var reading = _mapper.Map<MeterReading>(request);
        //    reading.Month = now.Month;
        //    reading.Year = now.Year;
        //    reading.RoomId=contract.RoomId;
        //    reading.TenantId=contract.TenantId;
        //    reading.PreviousIndex = prevReading?.CurrentIndex ?? 0;
        //    reading.CurrentIndex = request.CurrentIndex;
        //    reading.Usage = request.CurrentIndex - reading.PreviousIndex;
        //    reading.UnitPrice = request.Type == MeterType.Electric ? ElectricUnitPrice : WaterUnitPrice;
        //    reading.TotalCost = (decimal)reading.Usage * reading.UnitPrice;
        //    reading.PhotoUrl = photoUrl;
        //    reading.CreatedAt = DateTime.UtcNow;

        //    await _collection.InsertOneAsync(reading);

        //    var contracts = await _contractCollection.Find(_ => true).ToListAsync();
        //    return CreatedAtAction(nameof(GetById), new { id = reading.Id },
        //        MapToResponse(reading, contracts));
        //}

        // PUT: api/MeterReadings/{id}
        //[HttpPut("{id}")]
        //[Consumes("multipart/form-data")]
        //public async Task<ActionResult<MeterReadingResponse>> Update(int id, [FromForm] MeterReadingRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var existing = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (existing is null)
        //        return NotFound(CommonMessage.NotFound("MeterReading"));

        //    // Kiểm tra chỉ số mới >= chỉ số cũ (tháng trước)
        //    if (request.MeterIndex < existing.PreviousIndex)
        //        errors.Add($"Chỉ số mới ({request.MeterIndex}) phải >= chỉ số tháng trước ({existing.PreviousIndex}).");

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    // Xử lý ảnh
        //    string? photoUrl = existing.PhotoUrl;
        //    if (request.Photo is not null)
        //    {
        //        _photoService.DeletePhoto(existing.PhotoUrl);
        //        photoUrl = await _photoService.SaveMaintenancePhotoAsync(request.Photo, "default", "MeterReadings");
        //    }

        //    var updated = _mapper.Map<MeterReading>(request);
        //    updated.Id = id;
        //    updated.Month = existing.Month;
        //    updated.Year = existing.Year;
        //    updated.PreviousIndex = existing.PreviousIndex;
        //    updated.Usage = request.MeterIndex - existing.PreviousIndex;
        //    updated.PhotoUrl = photoUrl;
        //    updated.CreatedAt = existing.CreatedAt;
        //    updated.UpdatedAt = DateTime.UtcNow;

        //    await _collection.ReplaceOneAsync(x => x.Id == id, updated);

        //    var contracts = await _contractCollection.Find(_ => true).ToListAsync();
        //    return Ok(MapToResponse(updated, contracts));
        //}

        // DELETE: api/MeterReadings/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (reading is null)
        //        return NotFound(CommonMessage.NotFound("Số công tơ"));

        //    // Delete photo from Cloudinary if exists
        //    if (!string.IsNullOrEmpty(reading.PhotoUrl))
        //    {
        //        try
        //        {
        //            await _photoService.DeletePhotoAsync(reading.PhotoUrl);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log error but continue with deletion
        //            Console.WriteLine($"Error deleting photo: {ex.Message}");
        //        }
        //    }

        //    await _collection.DeleteOneAsync(x => x.Id == id);

        //    return Ok(CommonMessage.Deleted("Số côn tơ"));
        //}

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(MeterReadingRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private async Task<MeterReading?> GetPreviousReading(
            string roomNumber, MeterType type, int month, int year)
        {
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            return await _collection
                .Find(x => x.RoomNumber == roomNumber
                         && x.Type == type
                         && x.Month == prevMonth
                         && x.Year == prevYear)
                .FirstOrDefaultAsync();
        }

        private static string GetTypeLabel(MeterType type) => type switch
        {
            MeterType.Electric => "Điện",
            MeterType.Water => "Nước",
            _ => type.ToString()
        };

        private MeterReadingResponse MapToResponse(MeterReading reading, List<Contract> contracts)
        {
            var response = _mapper.Map<MeterReadingResponse>(reading);

            // Tên người thuê
            var contract = contracts.FirstOrDefault(c =>
                c.RoomNumber == reading.RoomNumber &&
                c.Status == ContractStatus.Active);
            response.TenantName = contract?.TenantName ?? string.Empty;

            // Loại công tơ
            response.TypeLabel = GetTypeLabel(reading.Type);

            // Ngày ghi
            response.Period = reading.CreatedAt.ToString("d/M/yyyy");

            // Đơn giá và thành tiền
            var unitPrice = reading.Type == MeterType.Electric ? ElectricUnitPrice : WaterUnitPrice;
            var unit = reading.Type == MeterType.Electric ? "kWh" : "m³";

            response.UnitPrice = unitPrice;
            response.Total = (decimal)reading.Usage * unitPrice;
            response.UsageLabel = $"{reading.Usage} {unit}";

            return response;
        }
    }
}
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
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

        private const decimal ElectricUnitPrice = 3000m;
        private const decimal WaterUnitPrice = 10000m;    

        public MeterReadingsController(
            MongoDbService mongoService,
            IValidator<MeterReadingRequest> validator,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<MeterReading>("MeterReadings");
            _roomCollection = db.GetCollection<Room>("Rooms");
            _contractCollection = db.GetCollection<Contract>("Contracts");
            _validator = validator;
            _mapper = mapper;
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
            var allReadings = readings;

            var result = readings.Select(r => MapToResponse(r, contracts, allReadings)).ToList();
            return Ok(result);
        }

        // GET: api/MeterReadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReadingResponse>> GetById(int id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (reading is null)
                return NotFound(Message.NotFound("MeterReading"));

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            var allReadings = await _collection.Find(_ => true).ToListAsync();

            return Ok(MapToResponse(reading, contracts, allReadings));
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
            var allReadings = await _collection.Find(_ => true).ToListAsync();

            var result = readings.Select(r => MapToResponse(r, contracts, allReadings)).ToList();
            return Ok(result);
        }

        // POST: api/MeterReadings
        [HttpPost]
        public async Task<ActionResult<MeterReadingResponse>> Create(MeterReadingRequest request)
        {
            var errors = await ValidateRequest(request);

            // Kiểm tra phòng có tồn tại không
            var roomExists = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .AnyAsync();
            if (!roomExists)
                errors.Add(Message.NotFound("Room"));

            // Kiểm tra đã có chỉ số tháng này chưa
            var duplicate = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber
                         && x.Month == request.Month
                         && x.Year == request.Year)
                .AnyAsync();
            if (duplicate)
                errors.Add($"Phòng {request.RoomNumber} đã có chỉ số tháng {request.Month}/{request.Year}.");

            // Kiểm tra chỉ số mới phải >= chỉ số tháng trước
            var prevReading = await GetPreviousReading(request.RoomNumber, request.Month, request.Year);
            if (prevReading is not null)
            {
                if (request.ElectricityIndex < prevReading.ElectricityIndex)
                    errors.Add($"Chỉ số điện mới ({request.ElectricityIndex}) phải >= chỉ số tháng trước ({prevReading.ElectricityIndex}).");
                if (request.WaterIndex < prevReading.WaterIndex)
                    errors.Add($"Chỉ số nước mới ({request.WaterIndex}) phải >= chỉ số tháng trước ({prevReading.WaterIndex}).");
            }

            if (errors.Any())
                return BadRequest(errors);

            var reading = _mapper.Map<MeterReading>(request);
            reading.Id = await MongoIdHelper.GetNextIdAsync(_collection);
            reading.CreatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(reading);

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            var allReadings = await _collection.Find(_ => true).ToListAsync();

            return CreatedAtAction(nameof(GetById), new { id = reading.Id },
                MapToResponse(reading, contracts, allReadings));
        }

        // PUT: api/MeterReadings/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<MeterReadingResponse>> Update(int id, MeterReadingRequest request)
        {
            var errors = await ValidateRequest(request);

            var existing = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existing is null)
                return NotFound(Message.NotFound("MeterReading"));

            var roomExists = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .AnyAsync();
            if (!roomExists)
                errors.Add(Message.NotFound("Room"));

            if (errors.Any())
                return BadRequest(errors);

            var updated = _mapper.Map<MeterReading>(request);
            updated.Id = id;
            updated.CreatedAt = existing.CreatedAt;
            updated.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, updated);

            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            var allReadings = await _collection.Find(_ => true).ToListAsync();

            return Ok(MapToResponse(updated, contracts, allReadings));
        }

        // DELETE: api/MeterReadings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (reading is null)
                return NotFound(Message.NotFound("MeterReading"));

            await _collection.DeleteOneAsync(x => x.Id == id);
            return Ok(Message.Deleted("MeterReading"));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(MeterReadingRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        // Lấy chỉ số tháng trước của cùng phòng
        private async Task<MeterReading?> GetPreviousReading(string roomNumber, int month, int year)
        {
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            return await _collection
                .Find(x => x.RoomNumber == roomNumber
                         && x.Month == prevMonth
                         && x.Year == prevYear)
                .FirstOrDefaultAsync();
        }

        private MeterReadingResponse MapToResponse(
            MeterReading reading,
            List<Contract> contracts,
            List<MeterReading> allReadings)
        {
            var response = _mapper.Map<MeterReadingResponse>(reading);

            // Lấy tên người thuê từ contract active
            var contract = contracts.FirstOrDefault(c =>
                c.RoomNumber == reading.RoomNumber &&
                c.Status == ContractStatus.Active);
            response.TenantName = contract?.TenantName ?? string.Empty;

            // Ngày ghi (dùng CreatedAt)
            response.Period = reading.CreatedAt.ToString("d/M/yyyy");

            // Lấy chỉ số tháng trước
            var prevMonth = reading.Month == 1 ? 12 : reading.Month - 1;
            var prevYear = reading.Month == 1 ? reading.Year - 1 : reading.Year;

            var prevReading = allReadings.FirstOrDefault(x =>
                x.RoomNumber == reading.RoomNumber &&
                x.Month == prevMonth &&
                x.Year == prevYear);

            // Điện
            response.PreviousElectricityIndex = prevReading?.ElectricityIndex ?? 0;
            response.ElectricityUsage = reading.ElectricityIndex - response.PreviousElectricityIndex;
            response.ElectricityTotal = (decimal)response.ElectricityUsage * ElectricUnitPrice;

            // Nước
            response.PreviousWaterIndex = prevReading?.WaterIndex ?? 0;
            response.WaterUsage = reading.WaterIndex - response.PreviousWaterIndex;
            response.WaterTotal = (decimal)response.WaterUsage * WaterUnitPrice;

            return response;
        }
    }
}
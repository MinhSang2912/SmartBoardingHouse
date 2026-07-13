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
using Microsoft.AspNetCore.Authorization;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IValidator<ContractRequest> _validator;
        private readonly IMapper _mapper;
        private readonly ActivityLogService _activityLogService;

        public ContractsController(
            MongoDbService mongoService,
            IValidator<ContractRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService)
        {
            var db = mongoService.GetDatabase();
            _contractCollection = db.GetCollection<Contract>("contracts");
            _userCollection = db.GetCollection<User>("users");
            _roomCollection = db.GetCollection<Room>("rooms");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        // GET: api/Contracts
        [HttpGet]
        public async Task<ActionResult<List<ContractResponse>>> GetAll()
        {
            var contracts = await _contractCollection.Find(_ => true).ToListAsync();
            return Ok(contracts.Select(MapToResponse).ToList());
        }

        // GET: api/Contracts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractResponse>> GetById(string id)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            return Ok(MapToResponse(contract));
        }

        // POST: api/Contracts
        [HttpPost]
        public async Task<ActionResult<ContractResponse>> Create(ContractRequest request)
        {
            var errors = await ValidateRequest(request);

            // Số hợp đồng tồn tại 
            var contractExists = await _contractCollection
                .Find(x => x.ContractNumber == request.ContractNumber)
                .AnyAsync();
            if (contractExists)
                errors.Add(CommonMessage.ContractNumberExists(request.ContractNumber));

            //Số phòng tồn tại
            var roomExists = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .FirstOrDefaultAsync();
            if (roomExists is null)
                errors.Add(CommonMessage.NotFound("Phòng"));

            //Phòng đã có hợp đồng đang hiệu lực
            if (roomExists is not null)
            {
                var activeContractExists = await _contractCollection
                    .Find(x => x.RoomNumber == request.RoomNumber
                             && x.Status == ContractStatus.Active)
                    .AnyAsync();
                if (activeContractExists)
                    errors.Add(CommonMessage.ContractRoomIsExists());
            }

            // Người thuê tồn tại
            var userExists = await _userCollection
                .Find(x => x.Name == request.TenantName)
                .FirstOrDefaultAsync();

            if (userExists is null)
                errors.Add(CommonMessage.NotFound("Người thuê"));

            // Người thuê đã có hợp đồng đang hiệu lực
            if (userExists is not null)
                {
                var activeContractExistsForTenant = await _contractCollection
                    .Find(x => x.TenantName == request.TenantName
                             && x.Status == ContractStatus.Active)
                    .AnyAsync();
                if (activeContractExistsForTenant)
                    errors.Add(CommonMessage.ContractTenantIsExists());
            }

            if (errors.Any())
                return BadRequest(errors);

            if (roomExists is null || userExists is null)
            {
                return BadRequest(CommonMessage.NotFound("Phòng hoặc Người thuê"));
            }    
            var contract = _mapper.Map<Contract>(request);
            contract.CreatedAt = DateTime.Now;
            contract.Status = ContractStatus.Active;
            contract.SignedDate = DateTime.Now;
            contract.RoomId = roomExists.Id;
            contract.TenantId = userExists.Id;
            contract.RoomDeposit = roomExists.RoomDeposit;

            await _contractCollection.InsertOneAsync(contract);

            await _roomCollection.UpdateOneAsync(
                x => x.RoomNumber == request.RoomNumber,
                Builders<Room>.Update
                    .Set(x => x.Status, RoomStatus.Occupied)
                    .Set(x => x.TenantId, userExists.Id)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _userCollection.UpdateOneAsync(
                x => x.Name == request.TenantName,
                Builders<User>.Update
                    .Set(x => x.RoomNumber, request.RoomNumber)
                    .Set(x => x.RoomId, roomExists.Id)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _activityLogService.LogAsync(
                type: ActivityType.CheckIn,
                userName: contract.TenantName,
                roomNumber: contract.RoomNumber,
                description: string.Empty);

            return CreatedAtAction(nameof(GetById), new { id = contract.Id },
                MapToResponse(contract));
        }

        // PUT: api/Contracts/{id}
        //[HttpPut("{id}")]
        //public async Task<ActionResult<ContractResponse>> Update(int id, ContractRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var existingContract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (existingContract is null)
        //        return NotFound(CommonMessage.NotFound("Contract"));

        //    var contractExists = await _contractCollection
        //        .Find(x => x.ContractNumber == request.ContractNumber && x.Id != id)
        //        .AnyAsync();
        //    if (contractExists)
        //        errors.Add(CommonMessage.ContractNumberExists(request.ContractNumber));

        //    var roomExists = await _roomCollection
        //        .Find(x => x.RoomNumber == request.RoomNumber)
        //        .AnyAsync();
        //    if (!roomExists)
        //        errors.Add(CommonMessage.NotFound("Room"));

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var updatedContract = _mapper.Map<Contract>(request);
        //    updatedContract.Id = id;
        //    updatedContract.CreatedAt = existingContract.CreatedAt;
        //    updatedContract.UpdatedAt = DateTime.UtcNow;

        //    await _contractCollection.ReplaceOneAsync(x => x.Id == id, updatedContract);
        //    return Ok(MapToResponse(updatedContract));
        //}

        // PUT: api/Contracts/{id}/terminate
        [HttpPut("{id}/terminate")]
        public async Task<ActionResult<ContractResponse>> Terminate(string id)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            if (contract.Status != ContractStatus.Active)
                return BadRequest(CommonMessage.ContractStatusIsInvalid());

            await _contractCollection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Contract>.Update
                    .Set(x => x.Status, ContractStatus.Terminated)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _roomCollection.UpdateOneAsync(
                x => x.RoomNumber == contract.RoomNumber,
                Builders<Room>.Update
                    .Set(x => x.Status, RoomStatus.Available)
                    .Set(x => x.TenantId, null)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));
            await _userCollection.UpdateOneAsync(
                x => x.Name == contract.TenantName,
                Builders<User>.Update
                    .Set(x => x.RoomNumber, null)
                    .Set(x => x.RoomId, null)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _activityLogService.LogAsync(
                type: ActivityType.CheckOut,
                userName: contract.TenantName,
                roomNumber: contract.RoomNumber,
                description: string.Empty);

            contract.Status = ContractStatus.Terminated;
            return Ok(MapToResponse(contract));
        }

        // PUT: api/Contracts/{id}/extend
        [HttpPut("{id}/extend")]
        public async Task<ActionResult<ContractResponse>> Extend(string id, [FromBody] DateTime newEndDate)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            if (newEndDate <= contract.EndDate)
                return BadRequest("Ngày kết thúc mới phải sau ngày kết thúc hiện tại.");

            if (contract.Status != ContractStatus.Active)
                return BadRequest(CommonMessage.ContractStatusIsInvalid());

            await _contractCollection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Contract>.Update
                    .Set(x => x.EndDate, newEndDate)
                    .Set(x => x.Status, ContractStatus.Active)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            contract.EndDate = newEndDate;
            contract.Status = ContractStatus.Active;
            return Ok(MapToResponse(contract));
        }

        // DELETE: api/Contracts/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (contract is null)
        //        return NotFound(CommonMessage.NotFound("Contract"));

        //    await _contractCollection.DeleteOneAsync(x => x.Id == id);
        //    if (contract.Status == ContractStatus.Active)
        //    {
        //        await _roomCollection.UpdateOneAsync(
        //            x => x.RoomNumber == contract.RoomNumber,
        //            Builders<Room>.Update
        //                .Set(x => x.Status, RoomStatus.Available)
        //                .Set(x => x.UpdatedAt, DateTime.UtcNow));
        //    }

        //    return Ok(CommonMessage.Deleted("Contract"));
        //}

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(ContractRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private ContractResponse MapToResponse(Contract contract)
        {
            var response = _mapper.Map<ContractResponse>(contract);

            response.StatusLabel = contract.Status switch
            {
                ContractStatus.Active => "Đang hiệu lực",
                ContractStatus.Expired => "Hết hạn",
                ContractStatus.Terminated => "Đã chấm dứt",
                _ => contract.Status.ToString()
            };

            response.PaymentDateLabel = $"Ngày {contract.PaymentDate} hàng tháng";
            response.RemainTime = contract.RemainTime;

            return response;
        }
    }
}
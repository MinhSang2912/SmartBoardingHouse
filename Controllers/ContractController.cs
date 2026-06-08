using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController : ControllerBase
    {
        private readonly IMongoCollection<Contract> _collection;
        private readonly IValidator<Contract> _validator;

        public ContractsController(MongoDbService mongoService, IValidator<Contract> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<Contract>("Contracts");
            _validator = validator;
        }

        // GET: api/Contracts
        [HttpGet]
        public async Task<ActionResult<List<Contract>>> GetAll()
        {
            var contracts = await _collection.Find(_ => true).ToListAsync();
            return Ok(contracts);
        }

        // GET: api/Contracts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetById(int id)
        {
            var contract = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return contract is null ? NotFound(Message.NotFound("Contract")) : Ok(contract);
        }

        // POST: api/Contracts
        [HttpPost]
        public async Task<ActionResult<Contract>> Create(Contract contract)
        {
            var validationResult = await _validator.ValidateAsync(contract);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra ContractNumber đã tồn tại chưa
            var contractNumberExists = await _collection
                .Find(x => x.ContractNumber == contract.ContractNumber)
                .AnyAsync();

            if (contractNumberExists)
                errors.Add(Message.ContractNumberExists(contract.ContractNumber));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(contract);
            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
        }

        // PUT: api/Contracts/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Contract>> Update(int id, Contract updatedContract)
        {
            if (updatedContract.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedContract);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedContract);

            return result.ModifiedCount > 0
                ? Ok(updatedContract)
                : NotFound(Message.NotFound("Contract"));
        }

        // DELETE: api/Contracts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("Contract"))
                : NotFound(Message.NotFound("Contract"));
        }
    }
}
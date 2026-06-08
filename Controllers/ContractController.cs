using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractController : ControllerBase
    {
        private readonly IMongoCollection<Contract> _collection;
        private readonly IValidator<Contract> _validator;

        public ContractController(IMongoDatabase database, IValidator<Contract> validator)
        {
            _collection = database.GetCollection<Contract>("Contracts");
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<List<Contract>>> GetAll()
        {
            var contracts = await _collection.Find(_ => true).ToListAsync();
            return Ok(contracts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetById(int id)
        {
            var contract = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return contract is null ? NotFound() : Ok(contract);
        }

        [HttpPost]
        public async Task<ActionResult<Contract>> Create([FromBody] Contract contract)
        {
            var validationResult = await _validator.ValidateAsync(contract);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            await _collection.InsertOneAsync(contract);
            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Contract updatedContract)
        {
            var validationResult = await _validator.ValidateAsync(updatedContract);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedContract);
            return result.ModifiedCount > 0 ? Ok(updatedContract) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0 ? NoContent() : NotFound();
        }
    }
}
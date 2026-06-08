using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IMongoCollection<Invoice> _collection;
        private readonly IValidator<Invoice> _validator;

        public InvoicesController(MongoDbService mongoService, IValidator<Invoice> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<Invoice>("Invoices");
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetAll()
        {
            var invoices = await _collection.Find(_ => true).ToListAsync();
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetById(int id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return invoice is null ? NotFound() : Ok(invoice);
        }

        [HttpPost]
        public async Task<ActionResult<Invoice>> Create(Invoice invoice)
        {
            var validationResult = await _validator.ValidateAsync(invoice);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            await _collection.InsertOneAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Invoice>> Update(int id, Invoice updatedInvoice)
        {
            var validationResult = await _validator.ValidateAsync(updatedInvoice);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedInvoice);
            return result.ModifiedCount > 0 ? Ok(updatedInvoice) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0 ? NoContent() : NotFound();
        }
    }
}
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
    public class InvoicesController : ControllerBase
    {
        private readonly IMongoCollection<Invoice> _collection;
        private readonly IValidator<Invoice> _validator;

        public InvoicesController(MongoDbService mongoService, IValidator<Invoice> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<Invoice>("Invoices");
            _validator = validator;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<List<Invoice>>> GetAll()
        {
            var invoices = await _collection.Find(_ => true).ToListAsync();
            return Ok(invoices);
        }

        // GET: api/Invoices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetById(int id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return invoice is null ? NotFound(Message.NotFound("Invoice")) : Ok(invoice);
        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<ActionResult<Invoice>> Create(Invoice invoice)
        {
            var validationResult = await _validator.ValidateAsync(invoice);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra trùng RoomNumber + DueDate (tùy chọn)
            var invoiceExists = await _collection
                .Find(x => x.RoomNumber == invoice.RoomNumber && x.DueDate == invoice.DueDate)
                .AnyAsync();

            if (invoiceExists)
                errors.Add($"Invoice for room {invoice.RoomNumber} on {invoice.DueDate:yyyy-MM-dd} already exists.");

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        // PUT: api/Invoices/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Invoice>> Update(int id, Invoice updatedInvoice)
        {
            if (updatedInvoice.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedInvoice);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedInvoice);

            return result.ModifiedCount > 0
                ? Ok(updatedInvoice)
                : NotFound(Message.NotFound("Invoice"));
        }

        // DELETE: api/Invoices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("Invoice"))
                : NotFound(Message.NotFound("Invoice"));
        }
    }
}
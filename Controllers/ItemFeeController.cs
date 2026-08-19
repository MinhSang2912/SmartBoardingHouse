using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonMessage = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ItemFeeController : ControllerBase
    {
        private readonly IMongoCollection<ItemFee> _collection;
        private readonly IMapper _mapper;
        private readonly IValidator<ItemFeeRequest> _validator;

        public ItemFeeController(
            IMongoDatabase database,
            IMapper mapper,
            IValidator<ItemFeeRequest> validator)
        {
            _collection = database.GetCollection<ItemFee>("itemfees");
            _mapper = mapper;
            _validator = validator;
        }

        // GET: api/ItemFee
        [HttpGet]
        public async Task<ActionResult<List<ItemFeeResponse>>> GetAll()
        {
            var fees = await _collection.Find(_ => true).ToListAsync();
            var responses = _mapper.Map<List<ItemFeeResponse>>(fees);
            return Ok(responses);
        }

        // GET: api/ItemFee/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemFeeResponse>> GetById(string id)
        {
            var fee = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (fee is null)
                return NotFound(CommonMessage.NotFound("Khoản phí phụ"));

            var response = _mapper.Map<ItemFeeResponse>(fee);
            return Ok(response);
        }

        // POST: api/ItemFee
        [HttpPost]
        public async Task<ActionResult<ItemFeeResponse>> Create(ItemFeeRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
            }

            // Kiểm tra trùng tên
            var exists = await _collection.Find(x => x.Name.ToLower() == request.Name.ToLower()).AnyAsync();
            if (exists)
            {
                return BadRequest(new List<string> { CommonMessage.IsExists("Tên khoản phí") });
            }

            var fee = _mapper.Map<ItemFee>(request);
            fee.CreatedAt = DateTime.UtcNow;
            fee.UpdatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(fee);

            var response = _mapper.Map<ItemFeeResponse>(fee);
            return CreatedAtAction(nameof(GetById), new { id = fee.Id }, response);
        }

        // PUT: api/ItemFee/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ItemFeeResponse>> Update(string id, ItemFeeRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
            }

            var fee = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (fee is null)
                return NotFound(CommonMessage.NotFound("Khoản phí phụ"));

            // Kiểm tra trùng tên với phần tử khác
            var exists = await _collection.Find(x => x.Name.ToLower() == request.Name.ToLower() && x.Id != id).AnyAsync();
            if (exists)
            {
                return BadRequest(new List<string> { CommonMessage.IsExists("Tên khoản phí") });
            }

            _mapper.Map(request, fee);
            fee.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, fee);

            var response = _mapper.Map<ItemFeeResponse>(fee);
            return Ok(response);
        }

        // DELETE: api/ItemFee/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var fee = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (fee is null)
                return NotFound(CommonMessage.NotFound("Khoản phí phụ"));

            await _collection.DeleteOneAsync(x => x.Id == id);
            return Ok(CommonMessage.Deleted("Khoản phí phụ"));
        }
    }
}

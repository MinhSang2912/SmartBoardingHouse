using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models;

namespace SmartBoardingHouse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloorsController : ControllerBase
{
    private readonly IMongoCollection<Floor> _collection;
    private readonly IValidator<Floor> _validator;

    public FloorsController(MongoDbService mongoService, IValidator<Floor> validator)
    {
        _collection = mongoService.GetDatabase().GetCollection<Floor>("Floors");
        _validator = validator;
    }

    [HttpGet]
    public async Task<ActionResult<List<Floor>>> GetAll()
    {
        var floors = await _collection.Find(_ => true).ToListAsync();
        return Ok(floors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Floor>> GetById(int id)
    {
        var floor = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        return floor is null ? NotFound() : Ok(floor);
    }

    [HttpPost]
    public async Task<ActionResult<Floor>> Create(Floor floor)
    {
        var validationResult = await _validator.ValidateAsync(floor);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        await _collection.InsertOneAsync(floor);
        return CreatedAtAction(nameof(GetById), new { id = floor.Id }, floor);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Floor>> Update(int id, Floor updatedFloor)
    {
        var validationResult = await _validator.ValidateAsync(updatedFloor);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedFloor);
        return result.ModifiedCount > 0 ? Ok(updatedFloor) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0 ? NoContent() : NotFound();
    }
}
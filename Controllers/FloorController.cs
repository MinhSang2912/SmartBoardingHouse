using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models;
using SmartBoardingHouse.Services;

namespace SmartBoardingHouse.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorController : ControllerBase
    {
        private readonly IMongoCollection<Floor> _floors;

        public FloorController(MongoDbService mongoDbService)
        {
            var database = mongoDbService.GetDatabase();
            _floors = database.GetCollection<Floor>("Floors");
        }
        // GET: api/Floor
        [HttpGet]
        public async Task<ActionResult<List<Floor>>> GetAll()
        {
            var floors = await _floors.Find(_ => true).ToListAsync();
            return Ok(floors);
        }

        // GET: api/Floor/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Floor>> GetById(int id)
        {
            var floor = await _floors.Find(f => f.Id == id).FirstOrDefaultAsync();

            if (floor == null)
                return NotFound(new { message = $"Không tìm thấy tầng có ID: {id}" });

            return Ok(floor);
        }

        // POST: api/Floor
        [HttpPost]
        public async Task<ActionResult<Floor>> Create([FromBody] Floor floor)
        {
            if (string.IsNullOrWhiteSpace(floor.FloorNumber))
                return BadRequest(new { message = "Số tầng không được để trống" });

            await _floors.InsertOneAsync(floor);

            return CreatedAtAction(nameof(GetById), new { id = floor.Id }, floor);
        }

        // PUT: api/Floor/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Floor updatedFloor)
        {
            var result = await _floors.ReplaceOneAsync(f => f.Id == id, updatedFloor);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Không tìm thấy tầng có ID: {id}" });

            return Ok(new { message = "Cập nhật tầng thành công" });
        }

        // DELETE: api/Floor/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _floors.DeleteOneAsync(f => f.Id == id);

            if (result.DeletedCount == 0)
                return NotFound(new { message = $"Không tìm thấy tầng có ID: {id}" });

            return Ok(new { message = "Xóa tầng thành công" });
        }
    }
}
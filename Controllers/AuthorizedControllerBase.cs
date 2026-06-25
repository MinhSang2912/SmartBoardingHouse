using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    public abstract class AuthorizedControllerBase : ControllerBase
    {
        protected readonly IMongoCollection<User> _userCollection;

        protected AuthorizedControllerBase(MongoDbService mongoService)
        {
            var db = mongoService.GetDatabase();
            _userCollection = db.GetCollection<User>("Users");
        }

        protected async Task<User?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return null;

            return await _userCollection.Find(x => x.Id == userId.Value).FirstOrDefaultAsync();
        }

        protected int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }
}

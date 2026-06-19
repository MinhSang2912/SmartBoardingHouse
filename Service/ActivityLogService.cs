using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Services
{
    public class ActivityLogService
    {
        private readonly IMongoCollection<ActivityLog> _collection;

        public ActivityLogService(IMongoCollection<ActivityLog> collection)
        {
            _collection = collection;
        }

        public async Task LogAsync(
            ActivityType type,
            string userName,
            string roomNumber,
            string description,
            decimal? amount = null)
        {
            var log = new ActivityLog
            {
                Type = type,
                UserName = userName,
                RoomNumber = roomNumber,
                Description = description,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };

            // Id tự tăng
            var last = await _collection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Limit(1)
                .FirstOrDefaultAsync();

            log.Id = last == null ? 1 : last.Id + 1;

            await _collection.InsertOneAsync(log);
        }
    }
}
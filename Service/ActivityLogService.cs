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
                // Không gán Id nữa, để MongoDB tự sinh ObjectId
            };

            await _collection.InsertOneAsync(log);
        }

        /// <summary>
        /// Lấy lịch sử hoạt động theo phòng hoặc người dùng (tùy chọn)
        /// </summary>
        public async Task<List<ActivityLog>> GetLogsAsync(
            string? roomNumber = null,
            string? userName = null,
            int limit = 50)
        {
            var filter = Builders<ActivityLog>.Filter.Empty;

            if (!string.IsNullOrEmpty(roomNumber))
            {
                filter &= Builders<ActivityLog>.Filter.Eq(x => x.RoomNumber, roomNumber);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                filter &= Builders<ActivityLog>.Filter.Eq(x => x.UserName, userName);
            }

            return await _collection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
    }
}
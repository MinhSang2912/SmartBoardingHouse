using MongoDB.Driver;
using SmartBoardingHouse.Models;

namespace SmartBoardingHouse.Common
{
    public static class MongoIdHelper
    {
        /// <summary>
        /// Lấy Id tiếp theo (max Id hiện tại + 1) cho một collection bất kỳ
        /// kế thừa từ BaseModel (Id kiểu int).
        /// </summary>
        public static async Task<int> GetNextIdAsync<T>(IMongoCollection<T> collection) where T : BaseModel
        {
            var lastItem = await collection
                .Find(_ => true)
                .SortByDescending(x => x.Id)
                .Limit(1)
                .FirstOrDefaultAsync();

            return lastItem == null ? 1 : lastItem.Id + 1;
        }
    }
}
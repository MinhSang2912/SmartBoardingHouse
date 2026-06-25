using MongoDB.Driver;
using SmartBoardingHouse.Models;

namespace SmartBoardingHouse.Common
{
    public static class MongoIdHelper
    {
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


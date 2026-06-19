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

namespace SmartBoardingHouse.Common
{
    public static class PasswordHelper
    {
        public const string DefaultPassword = "Abc@1234";

        public static string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
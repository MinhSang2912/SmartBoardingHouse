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
        {
            // Kiểm tra hash hợp lệ (BCrypt hash luôn bắt đầu bằng $2a$, $2b$, $2x$, $2y$)
            if (string.IsNullOrEmpty(hash) || !hash.StartsWith("$2"))
            {
                // Password chưa được hash → so sánh plain text
                return password == hash;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Hash không hợp lệ → không cho đăng nhập
                return false;
            }
        }
    }
}


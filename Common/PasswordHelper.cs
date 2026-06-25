namespace SmartBoardingHouse.Common
{
    public static class PasswordHelper
    {
        public const string DefaultPassword = "Abc@1234";

        public static string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash) || !hash.StartsWith("$2"))
                return password == hash;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}

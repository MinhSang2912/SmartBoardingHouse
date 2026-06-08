namespace SmartBoardingHouse.Common
{
    public class Message
    {
        public static string Created(string entity) => $"{entity} created successfully.";
        public static string NotFound(string entity) => $"{entity} not found.";
        public static string Deleted(string entity) => $"{entity} deleted successfully.";
    }
}

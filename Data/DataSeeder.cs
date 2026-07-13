using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using static SmartBoardingHouse.Common.Enums;
using SmartBoardingHouse.Common;
using Microsoft.Extensions.Options;
using SmartBoardingHouse.Models.Settings;

namespace SmartBoardingHouse.Data
{
    public class DataSeeder
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<DataSeeder>? _logger;
        private readonly IOptions<AdminSettings> _adminSettings;

        public DataSeeder(IMongoDatabase database, IOptions<AdminSettings> adminSettings, ILogger<DataSeeder>? logger = null)
        {
            _database = database;
            _adminSettings = adminSettings;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var floorCol = _database.GetCollection<Floor>("floors");
                var roomCol = _database.GetCollection<Room>("rooms");
                var userCol = _database.GetCollection<User>("users");
                var contractCol = _database.GetCollection<Contract>("contracts");
                var invoiceCol = _database.GetCollection<Invoice>("invoices");

                // Nếu đã có dữ liệu thì bỏ qua seeding
                if (await floorCol.CountDocumentsAsync(FilterDefinition<Floor>.Empty) > 0)
                {
                    return;
                }

                Log("Bat đau seeding du lieu...");

                // 1. Seed Floors
                var floors = new List<Floor>
                {
                    new Floor { FloorNumber = 1, RoomCount = 2 },
                    new Floor { FloorNumber = 2, RoomCount = 2 }
                };

                await floorCol.InsertManyAsync(floors);
                var floorList = await floorCol.Find(_ => true).ToListAsync();
                Log($"Da tao {floorList.Count} tang.");

                // 2. Seed Users (khớp với model User mới)
                var users = new List<User>
                {
                    new User
                    {
                        Name = "Chu Nha",
                        Email = _adminSettings.Value.Email,
                        Password = PasswordHelper.Hash(_adminSettings.Value.Password),
                        PhoneNumber = "0900111222",
                        IDCard = "012345678910",          
                        AvatarUrl = "",
                        Address = "123 Đường ABC, Quận 1",
                        IsActive = true,
                        RoomNumber = "Chưa có phòng"
                    },
                    new User
                    {
                        Name = "Nguyen Van A",
                        Email = "nguyenvana@example.com",
                        Password = PasswordHelper.Hash("Abc@1234"),
                        PhoneNumber = "0912345678",
                        IDCard = "012345678901",
                        AvatarUrl = "",
                        Address = "456 Đường XYZ, Quận 7",
                        IsActive = true,
                        RoomNumber = "Chưa có phòng"
                    },
                    new User
                    {
                        Name = "Tran Thi B",
                        Email = "tranthib@example.com",
                        Password = PasswordHelper.Hash("Abc@1234"),
                        PhoneNumber = "0934567890",
                        IDCard = "098765432109",
                        AvatarUrl = "",
                        Address = "789 Đường DEF, Quận 2",
                        IsActive = true,
                        RoomNumber = "Chưa có phòng"
                    }
                };

                await userCol.InsertManyAsync(users);
                var userList = await userCol.Find(_ => true).ToListAsync();
                Log($"Da tao {userList.Count} nguoi dung.");

                // 3. Seed Rooms
                var rooms = new List<Room>
                {
                    new Room
                    {
                        RoomNumber = "P101",
                        Price = 2000000,
                        Area = 20,
                        RoomDeposit = 2000000,
                        FloorId = floorList[0].Id,
                        Status = RoomStatus.Available
                    },
                    new Room
                    {
                        RoomNumber = "P102",
                        Price = 2500000,
                        Area = 25,
                        RoomDeposit = 2500000,
                        FloorId = floorList[0].Id,
                        Status = RoomStatus.Available
                    },
                    new Room
                    {
                        RoomNumber = "P201",
                        Price = 2200000,
                        Area = 22,
                        RoomDeposit = 2200000,
                        FloorId = floorList[1].Id,
                        Status = RoomStatus.Available
                    }
                };

                await roomCol.InsertManyAsync(rooms);
                var roomList = await roomCol.Find(_ => true).ToListAsync();
                Log($"Da tao {roomList.Count} phong.");
                Log("Seeding du lieu thanh cong!");
            }
            catch (Exception ex)
            {
                Log($"❌ LỖI SEEDING: {ex.Message}", "ERROR");
                throw;
            }
        }

        private void Log(string message, string level = "INFO")
        {
            var logMessage = $"[DataSeeder] {message}";
            if (_logger != null)
            {
                if (level == "ERROR")
                    _logger.LogError(logMessage);
                else
                    _logger.LogInformation(logMessage);
            }
            else
            {
                Console.WriteLine(logMessage);
            }
        }
    }
}
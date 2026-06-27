using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using static SmartBoardingHouse.Common.Enums;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Data
{
    public class DataSeeder
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<DataSeeder>? _logger;

        public DataSeeder(IMongoDatabase database, ILogger<DataSeeder>? logger = null)
        {
            _database = database;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var floorCol = _database.GetCollection<Floor>("Floors");
                var roomCol = _database.GetCollection<Room>("Rooms");
                var userCol = _database.GetCollection<User>("Users");
                var contractCol = _database.GetCollection<Contract>("Contracts");
                var invoiceCol = _database.GetCollection<Invoice>("Invoices");

                // Nếu đã có dữ liệu thì bỏ qua seeding
                if (await floorCol.CountDocumentsAsync(FilterDefinition<Floor>.Empty) > 0)
                {
                    Log("Dữ liệu đã tồn tại, bỏ qua seeding.");
                    return;
                }

                Log("Bắt đầu seeding dữ liệu...");

                // 1. Seed Floors
                var floors = new List<Floor>
                {
                    new Floor { FloorNumber = 1, RoomCount = 2 },
                    new Floor { FloorNumber = 2, RoomCount = 2 }
                };

                await floorCol.InsertManyAsync(floors);
                var floorList = await floorCol.Find(_ => true).ToListAsync();
                Log($"Đã tạo {floorList.Count} tầng.");

                // 2. Seed Users (khớp với model User mới)
                var users = new List<User>
                {
                    new User
                    {
                        Name = "Chu Nha",
                        Email = "chunha@example.com",
                        Password = PasswordHelper.Hash("Abc@1234"),
                        PhoneNumber = "0900111222",
                        IDCard = "012345678910",           // Đổi thành IDCard
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
                Log($"Đã tạo {userList.Count} người dùng.");

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
                Log($"Đã tạo {roomList.Count} phòng.");

                // 4. Seed Contracts (ví dụ cho tenant đầu tiên)
                var tenant = userList.FirstOrDefault(u => u.Email.Contains("nguyenvana")); // Chọn tenant
                var room = roomList.FirstOrDefault();

                if (tenant != null && room != null)
                {
                    var contract = new Contract
                    {
                        ContractNumber = $"HD-{DateTime.UtcNow:yyyyMMdd}-001",
                        TenantId = tenant.Id,
                        TenantName = tenant.Name,
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(6),
                        Status = ContractStatus.Active
                    };

                    await contractCol.InsertOneAsync(contract);

                    // Cập nhật phòng và tenant
                    room.Status = RoomStatus.Occupied;
                    await roomCol.ReplaceOneAsync(r => r.Id == room.Id, room);

                    tenant.RoomId = room.Id;
                    tenant.RoomNumber = room.RoomNumber;
                    await userCol.ReplaceOneAsync(u => u.Id == tenant.Id, tenant);

                    Log("Đã tạo hợp đồng mẫu và cập nhật phòng + tenant.");
                }

                Log("✅ Seeding dữ liệu thành công!");
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
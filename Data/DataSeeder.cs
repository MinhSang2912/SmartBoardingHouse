using MongoDB.Driver;
using SmartBoardingHouse.Models;
using SmartBoardingHouse.Models.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static SmartBoardingHouse.Common.Enums;

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

                if (await floorCol.CountDocumentsAsync(FilterDefinition<Floor>.Empty) > 0) return;

                // 1. Seed Floors
                var floors = new List<Floor> {
                    new Floor { Id = 1, FloorNumber = "Tang 1", RoomCount = 2 }
                };
                await floorCol.InsertManyAsync(floors);

                // 2. Seed Users
                var users = new List<User> {
                    new User {
                        Id = 1, Name = "Chu Nha", Email = "chunha@example.com",
                        Password = "password123", Role = Role.Owner,
                        PhoneNumber = "0900111222", IDCardNumber = "012345678910"
                    },
                    new User {
                        Id = 2, Name = "Nguyen Van A", Email = "nguyenvana@example.com",
                        Password = "password123", Role = Role.Tenant,
                        PhoneNumber = "0912345678", IDCardNumber = "012345678901"
                    }
                };
                await userCol.InsertManyAsync(users);

                // 3. Seed Rooms
                var rooms = new List<Room> {
                    new Room {
                        Id = 1, RoomNumber = "P101", Price = 2000000, Area = 20,
                        RoomDeposit = 1000000, FloorId = 1, Status = RoomStatus.Occupied
                    },
                    new Room {
                        Id = 2, RoomNumber = "P102", Price = 2500000, Area = 25,
                        RoomDeposit = 1500000, FloorId = 1, Status = RoomStatus.Available
                    }
                };
                await roomCol.InsertManyAsync(rooms);

                // 4. Seed Contracts
                var contracts = new List<Contract> {
                    new Contract {
                        Id = 1, ContractNumber = "HD001", RoomNumber = "P101",
                        TenantName = "Nguyen Van A", StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(6), PaymentDate = 5, Status = ContractStatus.Active
                    }
                };
                await contractCol.InsertManyAsync(contracts);

                // 5. Seed Invoices
                var invoices = new List<Invoice> {
                    new Invoice {
                        Id = 1, RoomName = "P101", Amount = 2000000,
                        DueDate = DateTime.Now.AddDays(7), Status = InvoiceStatus.Unpaid
                    }
                };
                await invoiceCol.InsertManyAsync(invoices);

                Log("Seed du lieu thanh cong cho tat ca cac collection.");
            }
            catch (Exception ex)
            {
                Log($"LOI: {ex.Message}", "ERROR");
            }
        }

        private void Log(string message, string level = "INFO")
        {
            var logMessage = $"[DataSeeder] {message}";
            if (_logger != null)
            {
                if (level == "ERROR") _logger.LogError(logMessage);
                else _logger.LogInformation(logMessage);
            }
            else
            {
                Console.WriteLine(logMessage);
            }
        }
    }
}
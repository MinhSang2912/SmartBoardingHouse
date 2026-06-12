using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Đăng ký Validators 
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.Contract>, SmartBoardingHouse.Models.Entity.ContractValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Request.FloorRequest>, SmartBoardingHouse.Models.Request.FloorRequestValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.Invoice>, SmartBoardingHouse.Models.Entity.InvoiceValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.MaintenanceRequest>, SmartBoardingHouse.Models.Entity.MaintenanceRequestValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.MeterReading>, SmartBoardingHouse.Models.Entity.MeterReadingValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.Room>, SmartBoardingHouse.Models.Entity.RoomValidation>();
builder.Services.AddScoped<IValidator<SmartBoardingHouse.Models.Entity.User>, SmartBoardingHouse.Models.Entity.UserValidation>();

// Đăng ký AutoMapper
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<FloorMappingProfile>();
},NullLoggerFactory.Instance);

builder.Services.AddSingleton(mapperConfig.CreateMapper());

var app = builder.Build();

// ====================== SEED DATA ======================
var mongoService = app.Services.GetRequiredService<MongoDbService>();
var dataSeeder = new DataSeeder(mongoService.GetDatabase());
await dataSeeder.SeedAsync();

// ====================== MIDDLEWARE ======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartBoardingHouse API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ====================== ROUTES ======================
app.MapControllers();

app.Run();
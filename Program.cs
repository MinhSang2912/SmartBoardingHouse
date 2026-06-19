using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Mappings;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Đăng ký Validators 
builder.Services.AddScoped<IValidator<ContractRequest>, ContractRequestValidation>();
builder.Services.AddScoped<IValidator<FloorRequest>, FloorRequestValidation>();
builder.Services.AddScoped<IValidator<InvoiceRequest>, InvoiceRequestValidation>();
builder.Services.AddScoped<IValidator<MaintenanceRequestRequest>, MaintenanceRequestRequestValidation>();
builder.Services.AddScoped<IValidator<MeterReadingRequest>, MeterReadingRequestValidation>();
builder.Services.AddScoped<IValidator<RoomRequest>, RoomRequestValidation>();
builder.Services.AddScoped<IValidator<UserRequest>, UserRequestValidation>();

// Đăng ký ActivityLogService
builder.Services.AddScoped<ActivityLogService>(sp =>
{
    var mongoService = sp.GetRequiredService<MongoDbService>();
    var collection = mongoService.GetDatabase().GetCollection<ActivityLog>("ActivityLogs");
    return new ActivityLogService(collection);
});

// Đăng ký AutoMapper
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<FloorMappingProfile>();
    cfg.AddProfile<RoomMappingProfile>();
    cfg.AddProfile<UserMappingProfile>();
    cfg.AddProfile<ContractMappingProfile>();
    cfg.AddProfile<InvoiceMappingProfile>();
    cfg.AddProfile<MeterReadingMappingProfile>();

}, NullLoggerFactory.Instance);

builder.Services.AddSingleton(mapperConfig.CreateMapper());
builder.Services.AddSingleton<PhotoService>();

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
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();

// ====================== ROUTES ======================
app.MapControllers();

app.Run();
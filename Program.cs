using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Mappings;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================== SERVICES ======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

// ====================== MONGODB ======================
builder.Services.AddSingleton<MongoDbService>();

// ====================== VALIDATORS ======================
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidation>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidation>();
builder.Services.AddScoped<IValidator<ContractRequest>, ContractRequestValidation>();
builder.Services.AddScoped<IValidator<FloorRequest>, FloorRequestValidation>();
builder.Services.AddScoped<IValidator<InvoiceRequest>, InvoiceRequestValidation>();
builder.Services.AddScoped<IValidator<MaintenanceRequestRequest>, MaintenanceRequestRequestValidation>();
builder.Services.AddScoped<IValidator<MeterReadingRequest>, MeterReadingRequestValidation>();
builder.Services.AddScoped<IValidator<RoomRequest>, RoomRequestValidation>();
builder.Services.AddScoped<IValidator<UserRequest>, UserRequestValidation>();

// ====================== AUTOMAPPER ======================
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<FloorMappingProfile>();
    cfg.AddProfile<RoomMappingProfile>();
    cfg.AddProfile<UserMappingProfile>();
    cfg.AddProfile<ContractMappingProfile>();
    cfg.AddProfile<InvoiceMappingProfile>();
    cfg.AddProfile<MeterReadingMappingProfile>();
    cfg.AddProfile<MaintenanceMappingProfile>();
}, NullLoggerFactory.Instance);
builder.Services.AddSingleton(mapperConfig.CreateMapper());

// ====================== CUSTOM SERVICES ======================
// ActivityLogService - ghi lịch sử hoạt động
builder.Services.AddScoped<ActivityLogService>(sp =>
{
    var mongoService = sp.GetRequiredService<MongoDbService>();
    var collection = mongoService.GetDatabase().GetCollection<ActivityLog>("ActivityLogs");
    return new ActivityLogService(collection);
});

// PhotoService - lưu ảnh công tơ vào thư mục Images
builder.Services.AddSingleton<PhotoService>();

// JwtService - tạo và xác thực JWT token
builder.Services.AddSingleton<JwtService>();

// ====================== JWT AUTHENTICATION ======================
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "SmartBoardingHouseSecretKey2026!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// ====================== SWAGGER ======================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartBoardingHouse API",
        Version = "v1"
    });

    // Hỗ trợ Bearer token trong Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token: {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ====================== BUILD APP ======================
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

// Serve ảnh từ thư mục Images (truy cập qua /images/ten-file.jpg)
var imagesPath = Path.Combine(builder.Environment.ContentRootPath, "Images");
if (!Directory.Exists(imagesPath))
    Directory.CreateDirectory(imagesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});

app.UseHttpsRedirection();

// Authentication phải trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// ====================== ROUTES ======================
app.MapControllers();

app.Run();
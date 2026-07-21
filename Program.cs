using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Mappings;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Mapper;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Settings;
using SmartBoardingHouse.Service;
using SmartBoardingHouse.Services;
using System.Text;

BsonSerializer.RegisterSerializer(new ObjectSerializer(ObjectSerializer.AllAllowedTypes));

// Đăng ký serializer dùng chung cho toàn bộ enum trong hệ thống, để đọc/ghi đúng
// định dạng chuỗi thường mà backend Node.js/Mongoose (dùng chung DB) đang lưu.
// Xem chi tiết trong Common/LowerCaseStringEnumSerializer.cs
foreach (var enumType in new[]
{
    typeof(Enums.Role),
    typeof(Enums.ContractStatus),
    typeof(Enums.RoomStatus),
    typeof(Enums.InvoiceStatus),
    typeof(Enums.MaintenanceStatus),
    typeof(Enums.PriotyRequest),
    typeof(Enums.ActivityType),
    typeof(Enums.MeterType),
    typeof(Enums.MessageType),
    typeof(Enums.MaintenanceCategory),
    typeof(Enums.NotificationType),
})
{
    var serializerType = typeof(LowerCaseStringEnumSerializer<>).MakeGenericType(enumType);
    var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType)!;
    BsonSerializer.RegisterSerializer(enumType, serializer);
}

var builder = WebApplication.CreateBuilder(args);

// Render (và hầu hết các nền tảng hosting dạng container) cấp port động qua biến
// môi trường PORT, container phải lắng nghe đúng cổng này trên 0.0.0.0 thì mới
// nhận được traffic. Local dev không có biến PORT nên fallback về 8080.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ====================== SERVICES ======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
    });

// Đăng ký SignalR
builder.Services.AddSignalR();
//builder.Services.AddHttpClient<ChatService>();

// ====================== MONGODB ======================
builder.Services.AddSingleton<MongoDbService>();
// Đăng ký IMongoDatabase để các Controller có thể inject
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var mongoService = sp.GetRequiredService<MongoDbService>();
    return mongoService.GetDatabase();
});

// ====================== ADMIN SETTINGS ======================
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminAccount"));



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
builder.Services.AddScoped<IValidator<SendMessageRequest>, SendMessageRequestValidator>();
builder.Services.AddScoped<IValidator<NotificationRequest>, NotificationRequestValidator>();

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
    cfg.AddProfile<MessageMappingProfile>();
    cfg.AddProfile<NotificationMappingProfile>();
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

// NotificationService - tạo thông báo (tay hoặc tự động) + lưu DB + đẩy realtime
builder.Services.AddScoped<INotificationService, NotificationService>();

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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// ====================== CORS ======================
// Đọc danh sách origin được phép từ config (appsettings.json key "AllowedOrigins",
// hoặc env var "AllowedOrigins__0", "AllowedOrigins__1",... trên Render).
// Local dev không set thì fallback về localhost:5173 (Vite) như cũ.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
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
var adminSettings = app.Services.GetRequiredService<IOptions<AdminSettings>>();
var dataSeeder = new DataSeeder(mongoService.GetDatabase(), adminSettings);
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

// Render (và các platform tương tự) đã xử lý HTTPS ở tầng proxy/edge, container
// bên trong chỉ nhận HTTP thuần. Nếu vẫn bật UseHttpsRedirection ở production,
// middleware sẽ cố redirect sang cổng HTTPS không tồn tại trong container -> lỗi.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowReact");

// Authentication phải trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// ====================== ROUTES ======================
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "SmartBoardingHouse API" }));
app.MapControllers();
//app.MapHub<ChatHub>("/hubs/chat");

app.Run();
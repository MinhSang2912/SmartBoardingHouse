using SmartBoardingHouse.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
// Configure the HTTP request pipeline.
var mongoService = app.Services.GetRequiredService<MongoDbService>();
var dataSeeder = new DataSeeder(mongoService.GetDatabase());
await dataSeeder.SeedAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


IConfiguration configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Read settings from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()  // Optional: Log to console
    .CreateLogger();
// Set Serilog as the logging provider
builder.Host.UseSerilog();
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.Configure<QuickBaseSettings>(
   builder.Configuration.GetSection("quickbase"));
builder.Services.AddHttpClient();
builder.Services.Configure<Connections>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Connections>>().Value);
// Register AzureBlobService in DI
builder.Services.AddSingleton<AzureBlobService>();

// Register StreetLightsService in DI
builder.Services.AddScoped<StreetLightsService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var devCorsPolicy = "devCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(devCorsPolicy, builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        // You can further restrict origins if needed:
        builder.WithOrigins("http://localhost:4200");
        builder.WithOrigins("http://localhost:4210");
        builder.WithOrigins("https://dcelectricgroup.net");
        builder.WithOrigins("https://ui.dcelectricgroup.net");
        builder.WithOrigins("https://www.dcelectricgroup.net");
    });
});

var app = builder.Build();
app.UseCors(devCorsPolicy);
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


 
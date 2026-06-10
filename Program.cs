using System.Globalization;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.Extensions.Options;
using Serilog;
using WebSupergoo.ABCpdf12;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"Current Environment: {builder.Environment.EnvironmentName}");

// Force en-US culture so currency/number formatting (e.g. {value:C}) always
// renders "$" rather than the invariant culture's generic currency sign "¤".
// The container sets this via LANG/LC_ALL, but `dotnet run`/IDE launches have no
// locale env, so .NET defaults to the invariant culture. Setting it in code makes
// formatting correct in every environment, independent of OS locale.
var enUsCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = enUsCulture;
CultureInfo.DefaultThreadCurrentUICulture = enUsCulture;

// Load environment-specific appsettings (e.g., appsettings.Docker.json)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var dd = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"DEBUG: The connection string being used is: {dd}");

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Read settings from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()  // Optional: Log to console
    .CreateLogger();
// Set Serilog as the logging provider
builder.Host.UseSerilog();

// ABCpdf locates its Chrome (ABCChrome123) HTML-rendering engine relative to the
// current working directory. In the Cloud Run container the working directory is
// the app folder where the ABCChrome123 native engine sits, so it's found. But
// `dotnet run` and IDE launches use the project root as the working directory,
// where ABCChrome123 doesn't exist -> "Could not find ABCChrome123". Align the
// working directory with the app base directory so PDF rendering works in every
// launch mode (no-op in the container where the two are already equal).
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Install ABCpdf license for PDF generation
var key = builder.Configuration.GetSection("Websupergoo:license").Value;

var licenseInstalled = XSettings.InstallLicense(key);

Console.WriteLine($"ABCpdf license installed: {licenseInstalled}");
Console.WriteLine($"ABCpdf license status: {XSettings.LicenseDescription}");
Console.WriteLine($"ABCpdf version: {XSettings.Version}");
Log.Information("ABCpdf license installed: {LicenseInstalled}", licenseInstalled);
Log.Information("ABCpdf license status: {LicenseDescription}", XSettings.LicenseDescription);
Log.Information("ABCpdf version: {Version}", XSettings.Version);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.Configure<QuickBaseSettings>(
   builder.Configuration.GetSection("quickbase"));
builder.Services.AddHttpClient();
builder.Services.Configure<Connections>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Connections>>().Value);
// Register AzureBlobService in DI
builder.Services.AddSingleton<AzureBlobService>();

// Register Google Cloud Storage settings and services
builder.Services.Configure<GoogleCloudStorageSettings>(
    builder.Configuration.GetSection("GoogleCloudStorage"));
builder.Services.AddSingleton<GoogleCloudStorageService>();
builder.Services.AddSingleton<DualStorageService>();

builder.Services.AddScoped<DataLayerBase>();
// Register StreetLightsService in DI
builder.Services.AddScoped<StreetLightsService>();
// Register TrafficLightsService in DI
builder.Services.AddScoped<TrafficLightsService>();
 


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
        builder.WithOrigins("https://dcwebui-682935653385.us-central1.run.app");
        builder.WithOrigins("https://dcelectricgroup.net");
        builder.WithOrigins("https://ui.dcelectricgroup.net");
        builder.WithOrigins("https://www.dcelectricgroup.net");
        builder.WithOrigins("https://dce.morrisdev.com");
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


 
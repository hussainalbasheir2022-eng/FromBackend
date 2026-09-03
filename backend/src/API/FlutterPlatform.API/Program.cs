using System.Reflection;
using System.Text;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Interfaces;
using FlutterPlatform.Infrastructure.Data;
using FlutterPlatform.Infrastructure.Identity;
using FlutterPlatform.Infrastructure.Repositories;
using FlutterPlatform.Infrastructure.Services;
using FlutterPlatform.Infrastructure.SignalR;
using FlutterPlatform.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.CommandTimeout(60)));

var useLocalFallbacks = builder.Configuration.GetValue("Infrastructure:UseLocalFallbacks", false);
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

IConnectionMultiplexer? redis = null;
if (!useLocalFallbacks)
{
    try
    {
        redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton(redis);
        builder.Services.AddSingleton<IBuildQueue, BuildQueueService>();
        Log.Information("Using Redis-backed build queue");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis unavailable — falling back to in-memory build queue");
        useLocalFallbacks = true;
    }
}

if (useLocalFallbacks || redis is null)
{
    builder.Services.AddSingleton<IBuildQueue, InMemoryBuildQueue>();
    Log.Information("Using in-memory build queue (local development)");
}

if (useLocalFallbacks)
{
    var artifactsRoot = Path.Combine(builder.Environment.ContentRootPath, "artifacts");
    builder.Services.AddSingleton<IStorageService>(_ => new LocalFileStorageService(artifactsRoot));
    Log.Information("Using local file artifact storage at {Path}", artifactsRoot);
}
else
{
    builder.Services.AddSingleton<IMinioClient>(_ =>
    {
        var cfg = builder.Configuration.GetSection("MinIO");
        return new MinioClient()
            .WithEndpoint(cfg["Endpoint"] ?? "localhost:9000")
            .WithCredentials(cfg["AccessKey"] ?? "minioadmin", cfg["SecretKey"] ?? "minioadmin")
            .WithSSL(bool.Parse(cfg["UseSSL"] ?? "false"))
            .Build();
    });
    builder.Services.AddSingleton<IStorageService>(sp =>
    {
        var client = sp.GetRequiredService<IMinioClient>();
        var cfg = builder.Configuration.GetSection("MinIO");
        return new MinioStorageService(client,
            cfg["BucketName"] ?? "flutter-platform",
            cfg["PublicEndpoint"] ?? "");
    });
}

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectFileRepository, ProjectFileRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ISignalRNotifier, SignalRNotifier>();
builder.Services.AddScoped<ILocalDeviceDeployer, LocalDeviceDeployer>();
builder.Services.AddHostedService<LocalFlutterBuildWorker>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.Load("FlutterPlatform.Application")));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddCors(o => o.AddPolicy("WebDashboard", p =>
    p.WithOrigins(
            builder.Configuration["Cors:Origins"]?.Split(',') ?? ["http://localhost:5173"])
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Log.Information("Ensuring database exists...");
    await db.Database.EnsureCreatedAsync();
    Log.Information("Database ready");

    var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
    await storage.InitializeAsync();

    try
    {
        Log.Information("Seeding database...");
        await DbSeeder.SeedAsync(scope.ServiceProvider);
        Log.Information("Seed complete");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database seed skipped or failed — API will still start");
    }
}

app.MapOpenApi();
app.MapScalarApiReference(opt =>
{
    opt.Title = "Flutter Platform API";
    opt.Theme = ScalarTheme.DeepSpace;
});

app.UseSerilogRequestLogging();
app.UseCors("WebDashboard");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<BuildHub>("/hubs/build");
app.MapHub<DeploymentHub>("/hubs/deployment");

app.Run();

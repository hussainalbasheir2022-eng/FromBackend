using FlutterPlatform.BuildWorker;
using Minio;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// MinIO
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var cfg = builder.Configuration.GetSection("MinIO");
    return new MinioClient()
        .WithEndpoint(cfg["Endpoint"] ?? "localhost:9000")
        .WithCredentials(cfg["AccessKey"] ?? "minioadmin", cfg["SecretKey"] ?? "minioadmin")
        .WithSSL(bool.Parse(cfg["UseSSL"] ?? "false"))
        .Build();
});

builder.Services.AddSingleton<BuildJobProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

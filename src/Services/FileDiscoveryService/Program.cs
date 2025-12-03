using DataProcessing.FileDiscovery.Consumers;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using MongoDB.Entities;
using Prometheus;
using MassTransit;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Services.AddDataProcessingLogging(
    builder.Configuration, 
    builder.Environment, 
    "DataProcessing.FileDiscovery");

// Configure OpenTelemetry
var serviceName = "DataProcessing.FileDiscovery";
var activitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(activitySource);
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "localhost";
Console.WriteLine($"[DEBUG] FileDiscovery MongoDB ConnectionString from config: '{connectionString}'");
await DB.InitAsync("DataProcessingFileDiscovery", connectionString);

// Configure MassTransit with InMemory transport (MVP - Kafka migration pending)
builder.Services.AddMassTransit(x =>
{
    // Register FilePollingEvent consumer
    x.AddConsumer<FilePollingEventConsumer>(cfg =>
    {
        cfg.UseConcurrentMessageLimit(5); // Process 5 datasources concurrently
    });

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Register data source connectors
builder.Services.AddScoped<LocalFileConnector>();
builder.Services.AddScoped<FtpConnector>();
builder.Services.AddScoped<SftpConnector>();
builder.Services.AddScoped<IDataSourceConnector>(provider =>
{
    // Factory pattern - will be selected based on datasource type
    return provider.GetRequiredService<LocalFileConnector>();
});

// Configure health checks
builder.Services.AddDataProcessingHealthChecks(builder.Configuration, serviceName);

// Configure metrics
builder.Services.AddSingleton<DataProcessingMetrics>();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Discovery Service API v1");
        c.RoutePrefix = string.Empty;
    });
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

// Add correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Map health checks
app.UseDataProcessingHealthChecks();

// Configure Prometheus metrics endpoint
app.UseHttpMetrics();
app.MapMetrics();

app.Run();

using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using MongoDB.Entities;
using System.Diagnostics;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Services.AddDataProcessingLogging(
    builder.Configuration, 
    builder.Environment, 
    "DataProcessing.InvalidRecords");

// Configure OpenTelemetry
var serviceName = "DataProcessing.InvalidRecords";
var activitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(activitySource);
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? throw new InvalidOperationException("MongoDB connection string is required");
var databaseName = builder.Configuration.GetConnectionString("DatabaseName") ?? "DataProcessingPlatform";
await DB.InitAsync(databaseName, connectionString);

// Configure MassTransit with in-memory bus (per .clinerules - Kafka not required)
builder.Services.AddMassTransit(x =>
{
    // Use in-memory bus for development
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Register application services
builder.Services.AddScoped<InvalidRecordsService.Repositories.IInvalidRecordRepository, 
    InvalidRecordsService.Repositories.InvalidRecordRepository>();
builder.Services.AddScoped<InvalidRecordsService.Services.IInvalidRecordService, 
    InvalidRecordsService.Services.InvalidRecordService>();
builder.Services.AddScoped<InvalidRecordsService.Services.ICorrectionService, 
    InvalidRecordsService.Services.CorrectionService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invalid Records Service API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
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

app.Run();

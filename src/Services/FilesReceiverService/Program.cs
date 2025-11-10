using DataProcessing.FilesReceiver.Consumers;
using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Services;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using MongoDB.Entities;
using System.Diagnostics;
using Prometheus;
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
    "DataProcessing.FilesReceiver");

// Configure OpenTelemetry
var serviceName = "DataProcessing.FilesReceiver";
var activitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(activitySource);
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? throw new InvalidOperationException("MongoDB connection string is required");
await DB.InitAsync("DataProcessingFilesReceiver", connectionString);

// Configure MassTransit with in-memory bus (per .clinerules - Kafka replaced by in-memory)
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<FilePollingEventConsumer>();
    
    // Use in-memory bus for testing/development
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Register file readers
builder.Services.AddScoped<IFileReader, CsvFileReader>();
builder.Services.AddScoped<IFileReader, ExcelFileReader>();
builder.Services.AddScoped<IFileReader, JsonFileReader>();
builder.Services.AddScoped<IFileReader, XmlFileReader>();

// Register application services
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Files Receiver Service API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

// Add request logging - Disabled temporarily due to Serilog dependency issue
// app.UseDataProcessingRequestLogging();

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

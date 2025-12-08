using DataProcessing.FileProcessor.Consumers;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Converters;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using Hazelcast;
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
    "DataProcessing.FileProcessor");

// Configure OpenTelemetry
var serviceName = "DataProcessing.FileProcessor";
var activitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(activitySource);
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "localhost";
await DB.InitAsync("DataProcessingFileProcessor", connectionString);

// Configure Hazelcast Client
builder.Services.AddSingleton<IHazelcastClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var hazelcastHost = builder.Configuration.GetValue<string>("Hazelcast:Server") ?? "localhost:5701";
    var clusterName = builder.Configuration.GetValue<string>("Hazelcast:ClusterName") ?? "data-processing-cluster";
    
    var options = new HazelcastOptionsBuilder()
        .With(args =>
        {
            args.Networking.Addresses.Add(hazelcastHost);
            args.ClusterName = clusterName;
            args.Networking.ConnectionRetry.ClusterConnectionTimeoutMilliseconds = 30000;
        })
        .WithDefault("Logging:LogLevel:Hazelcast", "Information")
        .Build();
    
    var client = HazelcastClientFactory.StartNewClientAsync(options).GetAwaiter().GetResult();
    
    logger.LogInformation("Hazelcast client connected to cluster: {ClusterName} at {Server}", 
        clusterName, hazelcastHost);
    
    return client;
});

// Configure MassTransit with InMemory transport (MVP - Kafka migration pending)
var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "rabbitmq.ez-platform.svc.cluster.local";
var rabbitMqUser = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
var rabbitMqPass = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<FileDiscoveredEventConsumer>(cfg =>
    {
        cfg.UseConcurrentMessageLimit(10); // Process 10 files concurrently per instance
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Register data source connectors
builder.Services.AddScoped<LocalFileConnector>();
builder.Services.AddScoped<FtpConnector>();
builder.Services.AddScoped<SftpConnector>();

// Register format converters
builder.Services.AddScoped<CsvToJsonConverter>();
builder.Services.AddScoped<XmlToJsonConverter>();
builder.Services.AddScoped<ExcelToJsonConverter>();
builder.Services.AddScoped<JsonToJsonConverter>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Processor Service API v1");
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

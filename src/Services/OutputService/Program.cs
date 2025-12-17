// Program.cs - OutputService Entry Point
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: December 1, 2025

using Hazelcast;
using MassTransit;
using MongoDB.Entities;
using Serilog;
using System.Diagnostics;
using Confluent.Kafka;
using DataProcessing.Output.Consumers;
using DataProcessing.Output.Handlers;
using DataProcessing.Output.Services;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Converters;
using DataProcessing.Shared.Monitoring;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure structured logging with Serilog (OTEL log export configured in AddDataProcessingOpenTelemetry)
var serviceName = "DataProcessing.Output";
builder.Services.AddDataProcessingLogging(builder.Configuration, builder.Environment, serviceName);
// Note: Do NOT use builder.Host.UseSerilog() as it replaces logging infrastructure and bypasses OTEL

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "localhost";
var databaseName = builder.Configuration.GetConnectionString("DatabaseName") ?? "ezplatform";
await DB.InitAsync(databaseName, mongoConnectionString);

Log.Information("Connected to MongoDB: {ConnectionString}", mongoConnectionString);

// Configure Hazelcast Client
var hazelcastOptions = new HazelcastOptionsBuilder()
    .With(options =>
    {
        options.ClusterName = builder.Configuration["Hazelcast:ClusterName"] ?? "data-processing-cluster";
        options.Networking.Addresses.Add(builder.Configuration["Hazelcast:Server"] ?? "localhost:5701");
        options.Networking.ConnectionRetry.ClusterConnectionTimeoutMilliseconds = 30000;
        options.LoggerFactory.Creator = () => new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
    })
    .Build();

var hazelcastClient = await HazelcastClientFactory.StartNewClientAsync(hazelcastOptions);
builder.Services.AddSingleton<IHazelcastClient>(hazelcastClient);

Log.Information(
    "Connected to Hazelcast: {Server}, Cluster={ClusterName}",
    builder.Configuration["Hazelcast:Server"],
    builder.Configuration["Hazelcast:ClusterName"]);

// Configure Kafka Producer for KafkaOutputHandler
var kafkaServer = builder.Configuration.GetConnectionString("Kafka") ?? "localhost:9092";
var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaServer,
    Acks = Acks.All,  // Required when EnableIdempotence = true
    MessageTimeoutMs = 30000,
    RequestTimeoutMs = 30000,
    EnableIdempotence = true,
    CompressionType = CompressionType.Snappy,
    LingerMs = 10,
    BatchSize = 16384
};

var kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
builder.Services.AddSingleton<IProducer<string, string>>(kafkaProducer);

Log.Information("Kafka Producer configured: {Server}", kafkaServer);

// Register Output Handlers
builder.Services.AddScoped<IOutputHandler, KafkaOutputHandler>();
builder.Services.AddScoped<IOutputHandler, FolderOutputHandler>();

// Register Format Reconstructors (from Task-15)
builder.Services.AddScoped<JsonToCsvReconstructor>();
builder.Services.AddScoped<JsonToXmlReconstructor>();
builder.Services.AddScoped<JsonToExcelReconstructor>();

// Register Format Reconstructor Service
builder.Services.AddScoped<FormatReconstructorService>();

// Register Business Metrics for OTEL routing to Prometheus Business
builder.Services.AddBusinessMetrics();

Log.Information("Registered output handlers and format reconstructors");

// Configure MassTransit with RabbitMQ transport
var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "rabbitmq.ez-platform.svc.cluster.local";
var rabbitMqUser = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
var rabbitMqPass = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ValidationCompletedEventConsumer>();

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

// Disable MassTransit automatic health check registration
builder.Services.Configure<MassTransitHostOptions>(options =>
{
    options.WaitUntilStarted = false;
});

// Remove MassTransit from health check registrations to prevent startup issues
builder.Services.PostConfigure<HealthCheckServiceOptions>(options =>
{
    var massTransitChecks = options.Registrations
        .Where(r => r.Name.Contains("masstransit", StringComparison.OrdinalIgnoreCase))
        .ToList();

    foreach (var check in massTransitChecks)
    {
        options.Registrations.Remove(check);
    }
});

Log.Information(
    "MassTransit configured with RabbitMQ: Server={Server}",
    builder.Configuration.GetValue<string>("RabbitMQ:Host"));

// Configure OpenTelemetry (metrics, traces, and logs via OTLP)
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

Log.Information("OpenTelemetry configured with OTLP endpoint: {Endpoint}",
    builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");

// Configure health checks using shared configuration
builder.Services.AddDataProcessingHealthChecks(builder.Configuration, "DataProcessing.Output");

// Build application
var app = builder.Build();

// Configure HTTP request pipeline
app.UseDataProcessingHealthChecks();
app.MapGet("/", () => "OutputService - Running");

// Log startup information
Log.Information(
    "OutputService started - Port: 5009, MongoDB: {MongoDB}, Hazelcast: {Hazelcast}, Kafka: {Kafka}",
    mongoConnectionString,
    builder.Configuration["Hazelcast:Server"],
    kafkaServer);

app.Run();

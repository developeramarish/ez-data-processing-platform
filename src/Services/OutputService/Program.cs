// Program.cs - OutputService Entry Point
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: December 1, 2025

using Hazelcast;
using MassTransit;
using MongoDB.Entities;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Confluent.Kafka;
using DataProcessing.Output.Consumers;
using DataProcessing.Output.Handlers;
using DataProcessing.Output.Services;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Converters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "localhost";
await DB.InitAsync("ezplatform", mongoConnectionString);

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

Log.Information("Registered output handlers and format reconstructors");

// Configure MassTransit with Kafka
var useKafka = builder.Configuration.GetValue<bool>("MassTransit:UseKafka", true);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ValidationCompletedEventConsumer>();

    if (useKafka)
    {
        // Use Kafka for production cross-process communication
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });

        x.AddRider(rider =>
        {
            var kafkaServer = builder.Configuration.GetValue<string>("MassTransit:Kafka:Server")
                ?? "localhost:9092";

            rider.AddConsumer<ValidationCompletedEventConsumer>();

            rider.UsingKafka((context, kafka) =>
            {
                kafka.Host(kafkaServer);

                kafka.TopicEndpoint<DataProcessing.Shared.Messages.ValidationCompletedEvent>(
                    "validation-completed",
                    "output-service-group",
                    e =>
                    {
                        e.ConfigureConsumer<ValidationCompletedEventConsumer>(context);
                    });
            });
        });
    }
    else
    {
        // Use in-memory bus for development/testing
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
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
    "MassTransit configured with Kafka: {UseKafka}, Server={Server}",
    useKafka,
    builder.Configuration.GetValue<string>("MassTransit:Kafka:Server"));

// Configure OpenTelemetry
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OutputService"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("MassTransit")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OutputService"))
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("MassTransit")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

Log.Information("OpenTelemetry configured with OTLP endpoint: {Endpoint}", otlpEndpoint);

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

using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using DataProcessing.Validation.Consumers;
using DataProcessing.Validation.Services;
using MongoDB.Entities;
using System.Diagnostics;
using Prometheus;
using MassTransit;
using Hazelcast;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Services.AddDataProcessingLogging(
    builder.Configuration, 
    builder.Environment, 
    "DataProcessing.Validation");

// Configure OpenTelemetry
var serviceName = "DataProcessing.Validation";
var activitySource = new ActivitySource(serviceName);
builder.Services.AddSingleton(activitySource);
builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new InvalidOperationException("MongoDB connection string is required");
await DB.InitAsync("DataProcessingValidation", connectionString);

// Configure Hazelcast Client
builder.Services.AddSingleton<IHazelcastClient>(sp =>
{
    var hazelcastServer = builder.Configuration.GetValue<string>("Hazelcast:Server")
        ?? "localhost:5701";
    var clusterName = builder.Configuration.GetValue<string>("Hazelcast:ClusterName")
        ?? "data-processing-cluster";

    var options = new HazelcastOptionsBuilder()
        .With(args =>
        {
            args.Networking.Addresses.Add(hazelcastServer);
            args.ClusterName = clusterName;
            args.Networking.ConnectionRetry.ClusterConnectionTimeoutMilliseconds = 30000;
            args.LoggerFactory.Creator = () => new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
        })
        .Build();

    return HazelcastClientFactory.StartNewClientAsync(options).GetAwaiter().GetResult();
});

// Configure MassTransit with in-memory bus only (for testing - Kafka not required per .clinerules)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ValidationRequestEventConsumer>();
    
    // Use in-memory bus for testing/development
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Register application services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<DataMetricsCalculator>();

// Configure OpenTelemetry business metrics
builder.Services.AddSingleton<ValidationMetrics>();

// Configure OpenTelemetry with OTLP gRPC exporter
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: serviceName,
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics => metrics
        // Add ASP.NET Core instrumentation
        .AddAspNetCoreInstrumentation()

        // Add runtime instrumentation
        .AddRuntimeInstrumentation()

        // Add custom ValidationService meter
        .AddMeter("DataProcessing.Validation")

        // Add histogram view with custom buckets for validation duration
        .AddView(
            instrumentName: "validation.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 1000, 2500, 5000, 10000 }
            })

        // Add histogram view for business metrics
        .AddView(
            instrumentName: "business.metric.value",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000 }
            })

        // Configure OTLP exporter (gRPC to OpenTelemetry Collector)
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint")
                ?? "http://localhost:4317";

            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

            // Use batch export processor for efficiency
            options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
            options.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
            {
                MaxQueueSize = 2048,
                ScheduledDelayMilliseconds = 5000,
                ExporterTimeoutMilliseconds = 30000,
                MaxExportBatchSize = 512
            };
        }));

// Configure health checks
builder.Services.AddDataProcessingHealthChecks(builder.Configuration, serviceName);

// Configure legacy Prometheus metrics (keep for compatibility)
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Validation Service API v1");
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

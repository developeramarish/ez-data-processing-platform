using DataProcessing.Scheduling.Jobs;
using DataProcessing.Scheduling.Services;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using MongoDB.Entities;
using Quartz;
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
    "DataProcessing.Scheduling");

// Configure OpenTelemetry - Disabled temporarily due to package version conflict
// var serviceName = "DataProcessing.Scheduling";
// var activitySource = new ActivitySource(serviceName);
// builder.Services.AddSingleton(activitySource);
// builder.Services.AddDataProcessingOpenTelemetry(builder.Configuration, serviceName);
var serviceName = "DataProcessing.Scheduling";

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? throw new InvalidOperationException("MongoDB connection string is required");
await DB.InitAsync("ezplatform", connectionString);

// Configure MassTransit with Kafka for inter-service events
var kafkaServer = builder.Configuration.GetConnectionString("Kafka") ?? "kafka-0.kafka.ez-platform.svc.cluster.local:9092";
builder.Services.AddMassTransit(x =>
{
    // Register DataSource lifecycle event consumers
    x.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceCreatedConsumer>();
    x.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceUpdatedConsumer>();
    x.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceDeletedConsumer>();

    // Use in-memory bus for request/response patterns
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    // Add Kafka riders for event consumption
    x.AddRider(rider =>
    {
        rider.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceCreatedConsumer>();
        rider.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceUpdatedConsumer>();
        rider.AddConsumer<DataProcessing.Scheduling.Consumers.DataSourceDeletedConsumer>();

        rider.UsingKafka((context, kafka) =>
        {
            kafka.Host(kafkaServer);

            kafka.TopicEndpoint<DataProcessing.Shared.Messages.DataSourceCreatedEvent>("datasource-events", "scheduling-service", e =>
            {
                e.ConfigureConsumer<DataProcessing.Scheduling.Consumers.DataSourceCreatedConsumer>(context);
            });

            kafka.TopicEndpoint<DataProcessing.Shared.Messages.DataSourceUpdatedEvent>("datasource-events", "scheduling-service", e =>
            {
                e.ConfigureConsumer<DataProcessing.Scheduling.Consumers.DataSourceUpdatedConsumer>(context);
            });

            kafka.TopicEndpoint<DataProcessing.Shared.Messages.DataSourceDeletedEvent>("datasource-events", "scheduling-service", e =>
            {
                e.ConfigureConsumer<DataProcessing.Scheduling.Consumers.DataSourceDeletedConsumer>(context);
            });
        });
    });
});

// Configure Quartz.NET
builder.Services.AddQuartz(q =>
{
    // Use in-memory job store for simplicity
    q.UseInMemoryStore();
    
    // Configure scheduler
    q.SchedulerName = "DataProcessing-Scheduler";
    q.SchedulerId = Environment.MachineName;
    q.MaxBatchSize = 20;
    q.BatchTriggerAcquisitionFireAheadTimeWindow = TimeSpan.FromSeconds(15);
    
    // Jobs will be created dynamically by SchedulingManager
    // No need to pre-register jobs here
});

// Configure Quartz.NET hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Register IScheduler for dependency injection
builder.Services.AddSingleton<IScheduler>(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    return schedulerFactory.GetScheduler().GetAwaiter().GetResult();
});

// Register application services
builder.Services.AddScoped<ISchedulingManager, SchedulingManager>();
builder.Services.AddScoped<DataSourcePollingJob>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scheduling Service API v1");
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

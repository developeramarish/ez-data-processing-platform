using DataProcessing.FileDiscovery.Workers;
using DataProcessing.Shared.Configuration;
using DataProcessing.Shared.Connectors;
using DataProcessing.Shared.Middleware;
using DataProcessing.Shared.Monitoring;
using MongoDB.Entities;
using Quartz;
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
    "DataProcessing.FileDiscovery");

var serviceName = "DataProcessing.FileDiscovery";

// Configure MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? throw new InvalidOperationException("MongoDB connection string is required");
await DB.InitAsync("DataProcessingFileDiscovery", connectionString);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitMqPort = builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672);
        var rabbitMqUser = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
        var rabbitMqPassword = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

        cfg.Host(rabbitMqHost, rabbitMqPort, "/", h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPassword);
        });

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

// Configure Quartz.NET for file discovery scheduling
builder.Services.AddQuartz(q =>
{
    q.UseInMemoryStore();
    q.SchedulerName = "FileDiscovery-Scheduler";
    q.SchedulerId = Environment.MachineName;
    q.MaxBatchSize = 20;
    
    // Register the file discovery job
    var jobKey = new JobKey("FileDiscoveryJob");
    q.AddJob<FileDiscoveryWorker>(opts => opts.WithIdentity(jobKey));
    
    // Schedule to run every 30 seconds (will check all datasources)
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("FileDiscoveryTrigger")
        .WithCronSchedule("0/30 * * * * ?") // Every 30 seconds
        .StartNow());
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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

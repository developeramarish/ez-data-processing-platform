using MetricsConfigurationService.Repositories;
using MetricsConfigurationService.Services.Alerts;
using MetricsConfigurationService.Services.Collection;
using MetricsConfigurationService.Services.Prometheus;
using MongoDB.Driver;
using MongoDB.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Metrics Configuration API", Version = "v1" });
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register HTTP client for Prometheus queries
builder.Services.AddHttpClient("Prometheus", client =>
{
    var timeoutSeconds = builder.Configuration.GetValue<int>("Prometheus:TimeoutSeconds", 30);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Register repositories
builder.Services.AddScoped<IMetricRepository, MetricRepository>();

// Register Prometheus query service
builder.Services.AddScoped<IPrometheusQueryService, PrometheusQueryService>();

// Register alert evaluation service
builder.Services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();

// Register background metrics collection service
builder.Services.AddHostedService<MetricsCollectionBackgroundService>();

var app = builder.Build();

// Initialize MongoDB
var databaseName = builder.Configuration.GetValue<string>("MongoDB:DatabaseName") ?? "DataProcessing";
var connectionString = builder.Configuration.GetValue<string>("MongoDB:ConnectionString") ?? "localhost";

await DB.InitAsync(databaseName, connectionString);

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MongoDB initialized: {DatabaseName} at {ConnectionString}", databaseName, connectionString);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "MetricsConfigurationService",
    Timestamp = DateTime.UtcNow
}));

app.Run();

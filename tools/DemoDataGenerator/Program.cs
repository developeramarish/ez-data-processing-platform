using MongoDB.Entities;
using DemoDataGenerator.Services;
using DemoDataGenerator.Generators;
using DemoDataGenerator.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("    🎯 Demo Data Generator for EZ Platform    ");
Console.WriteLine("═══════════════════════════════════════════════\n");

// Parse command line arguments
bool incrementalMode = args.Contains("--incremental");
string mode = incrementalMode ? "INCREMENTAL" : "FULL RESET";

// Parse MongoDB connection string (support both Docker Compose and K8s)
string mongoConnection = "localhost"; // Default for Docker Compose
var mongoArg = args.FirstOrDefault(a => a.StartsWith("--mongodb-connection="));
if (mongoArg != null)
{
    mongoConnection = mongoArg.Split('=')[1];
}

Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"MongoDB: {mongoConnection}");
Console.WriteLine($"Seed: {DemoConfig.RandomSeed} (deterministic)\n");

try
{
    // Initialize MongoDB connection (configurable for Docker Compose or K8s)
    await DB.InitAsync("ezplatform", mongoConnection);
    Console.WriteLine("✓ Connected to MongoDB\n");
    
    // Initialize random with fixed seed for determinism
    var random = new Random(DemoConfig.RandomSeed);
    
    // Step 1: Reset database (unless incremental)
    if (!incrementalMode)
    {
        var resetService = new DatabaseResetService();
        await resetService.ResetAllCollectionsAsync();
    }
    else
    {
        Console.WriteLine("[1/7] ⏭️  Skipping reset (incremental mode)\n");
    }
    
    // Step 2: Generate DataSources
    var dsGenerator = new DataSourceGenerator(random);
    var datasources = await dsGenerator.GenerateAsync();
    
    // Step 3: Generate Schemas
    var schemaGenerator = new SchemaGenerator(random);
    await schemaGenerator.GenerateForDataSourcesAsync(datasources);
    
    // Step 4: Generate Global Metrics
    var globalMetricGenerator = new GlobalMetricGenerator(random);
    await globalMetricGenerator.GenerateAsync();
    
    // Step 5: Generate Datasource Metrics
    var dsMetricGenerator = new DatasourceMetricGenerator(random);
    await dsMetricGenerator.GenerateAsync(datasources);
    
    // Step 6: Generate Alerts
    var alertGenerator = new AlertGenerator(random);
    await alertGenerator.GenerateAsync();
    
    // Step 7: Summary
    Console.WriteLine("[7/7] 📊 Generation Summary:");
    var dsCount = await DB.CountAsync<DataProcessing.Shared.Entities.DataProcessingDataSource>(_ => true);
    var schemaCount = await DB.CountAsync<DataProcessing.Shared.Entities.DataProcessingSchema>(_ => true);
    var metricCount = await DB.CountAsync<MetricsConfigurationService.Models.MetricConfiguration>(_ => true);
    var metricsWithAlerts = await DB.Find<MetricsConfigurationService.Models.MetricConfiguration>()
        .Match(m => m.AlertRules != null && m.AlertRules.Count > 0)
        .ExecuteAsync();
    
    Console.WriteLine($"  ✅ {dsCount} DataSources");
    Console.WriteLine($"  ✅ {schemaCount} Schemas");
    Console.WriteLine($"  ✅ {metricCount} Metrics (global + datasource-specific)");
    Console.WriteLine($"  ✅ {metricsWithAlerts.Count} Metrics with alerts\n");
    
    Console.WriteLine("═══════════════════════════════════════════════");
    Console.WriteLine("  ✨ Demo data generation completed successfully!");
    Console.WriteLine("═══════════════════════════════════════════════\n");
    
    Console.WriteLine("Next steps:");
    Console.WriteLine("  1. cd tools\\ServiceOrchestrator");
    Console.WriteLine("  2. dotnet run start");
    Console.WriteLine("  3. Open http://localhost:3000\n");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    Environment.Exit(1);
}

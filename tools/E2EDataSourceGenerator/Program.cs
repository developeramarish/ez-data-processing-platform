using MongoDB.Entities;
using MongoDB.Driver;
using DataProcessing.Shared.Entities;
using E2EDataSourceGenerator.Templates;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("    🧪 E2E DataSource Generator for EZ Platform    ");
Console.WriteLine("═══════════════════════════════════════════════\n");

// Parse MongoDB connection string (support both Docker Compose and K8s)
string mongoConnection = "localhost"; // Default for Docker Compose
var mongoArg = args.FirstOrDefault(a => a.StartsWith("--mongodb-connection="));
if (mongoArg != null)
{
    mongoConnection = mongoArg.Split('=')[1];
}

Console.WriteLine($"MongoDB: {mongoConnection}\n");

try
{
    // Initialize MongoDB connection with directConnection to bypass replica set discovery
    var settings = new MongoClientSettings
    {
        Server = new MongoServerAddress(mongoConnection.Split(':')[0],
            mongoConnection.Contains(':') ? int.Parse(mongoConnection.Split(':')[1]) : 27017),
        DirectConnection = true
    };
    await DB.InitAsync("ezplatform", settings);
    Console.WriteLine("✓ Connected to MongoDB\n");

    // Delete existing E2E datasources
    Console.WriteLine("[1/3] 🗑️  Removing existing E2E datasources...");
    var existingE2E = await DB.Find<DataProcessingDataSource>()
        .Match(ds => ds.Name != null && ds.Name.StartsWith("E2E-"))
        .ExecuteAsync();

    foreach (var ds in existingE2E)
    {
        await ds.DeleteAsync();
        Console.WriteLine($"  ✓ Deleted: {ds.Name}");
    }
    Console.WriteLine($"  ✅ Removed {existingE2E.Count} existing E2E datasources\n");

    // Generate E2E-004 datasource
    Console.WriteLine("[2/4] 📊 Generating E2E-004 datasource with 4 destinations...");
    var e2e004 = E2E004Template.CreateE2E004MultiDestination();
    await e2e004.SaveAsync();
    Console.WriteLine($"  ✓ Created: {e2e004.Name}");
    Console.WriteLine($"  ✓ FilePattern: {e2e004.FilePattern}");
    Console.WriteLine($"  ✓ Path: {e2e004.FilePath}");
    Console.WriteLine($"  ✓ Cron: {e2e004.CronExpression}");
    Console.WriteLine($"  ✓ Output Destinations: {e2e004.Output?.Destinations?.Count ?? 0}");
    foreach (var dest in e2e004.Output?.Destinations ?? new List<OutputDestination>())
    {
        Console.WriteLine($"    - {dest.Name} ({dest.Type}, {dest.OutputFormat})");
    }
    Console.WriteLine($"  ✅ E2E-004 generated successfully\n");

    // Generate E2E-005 datasource
    Console.WriteLine("[3/4] 📊 Generating E2E-005 datasource for scheduled polling test...");
    var e2e005 = E2E005Template.CreateE2E005ScheduledPolling();
    await e2e005.SaveAsync();
    Console.WriteLine($"  ✓ Created: {e2e005.Name}");
    Console.WriteLine($"  ✓ FilePattern: {e2e005.FilePattern}");
    Console.WriteLine($"  ✓ Path: {e2e005.FilePath}");
    Console.WriteLine($"  ✓ Cron: {e2e005.CronExpression} (Every 1 minute)");
    Console.WriteLine($"  ✓ Output Destinations: {e2e005.Output?.Destinations?.Count ?? 0}");
    foreach (var dest in e2e005.Output?.Destinations ?? new List<OutputDestination>())
    {
        Console.WriteLine($"    - {dest.Name} ({dest.Type}, {dest.OutputFormat})");
    }
    Console.WriteLine($"  ✅ E2E-005 generated successfully\n");

    // Summary
    Console.WriteLine("[4/4] 📊 Generation Summary:");
    var e2eCount = await DB.Find<DataProcessingDataSource>()
        .Match(ds => ds.Name != null && ds.Name.StartsWith("E2E-"))
        .ExecuteAsync();
    Console.WriteLine($"  ✅ {e2eCount.Count} E2E DataSources created\n");

    Console.WriteLine("═══════════════════════════════════════════════");
    Console.WriteLine("  ✨ E2E datasource generation completed!");
    Console.WriteLine("═══════════════════════════════════════════════\n");

    Console.WriteLine("Next steps:");
    Console.WriteLine("  1. Open http://localhost:3000/datasources");
    Console.WriteLine("  2. Find E2E-004 and E2E-005 datasources");
    Console.WriteLine("  3. Click UPDATE on each to trigger DataSourceUpdatedEvent");
    Console.WriteLine("  4. E2E-004: Place test CSV in /mnt/external-test-data/E2E-004/");
    Console.WriteLine("  5. E2E-005: Place test CSV in /mnt/external-test-data/E2E-005/");
    Console.WriteLine("  6. E2E-004 executes every 5 minutes, E2E-005 every 1 minute\n");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    Environment.Exit(1);
}

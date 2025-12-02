// Program.cs - TestDataGenerator Entry Point
// Purpose: Generate systematic test data files for E2E testing
// Version: 1.0
// Date: December 2, 2025

using System.CommandLine;
using TestDataGenerator.Generators;

Console.WriteLine("====================================");
Console.WriteLine("EZ Platform - Test Data Generator");
Console.WriteLine("Version 1.0");
Console.WriteLine("====================================\n");

var rootCommand = new RootCommand("Generate test data files for EZ Platform E2E testing");

// Command: generate-all
var generateAllCommand = new Command("generate-all", "Generate all test files for all E2E scenarios");
generateAllCommand.SetHandler(() =>
{
    Console.WriteLine("Generating test files for all E2E scenarios...\n");

    var generator = new MasterGenerator();
    var results = generator.GenerateAllScenarios();

    Console.WriteLine("\n====================================");
    Console.WriteLine("Generation Complete!");
    Console.WriteLine("====================================");
    Console.WriteLine($"Total scenarios: {results.Count}");
    Console.WriteLine($"Total files generated: {results.Sum(r => r.FilesGenerated)}");
    Console.WriteLine($"Total records: {results.Sum(r => r.RecordsGenerated):N0}");
    Console.WriteLine($"\nTest files location: TestFiles/");

    foreach (var result in results)
    {
        var status = result.Success ? "✅" : "❌";
        Console.WriteLine($"{status} {result.ScenarioId}: {result.FilesGenerated} files, {result.RecordsGenerated:N0} records");
    }
});
rootCommand.AddCommand(generateAllCommand);

// Command: generate-scenario
var generateScenarioCommand = new Command("generate-scenario", "Generate test files for a specific scenario");
var scenarioArg = new Argument<string>("scenario", "Scenario ID (e.g., E2E-001)");
generateScenarioCommand.AddArgument(scenarioArg);
generateScenarioCommand.SetHandler((string scenario) =>
{
    Console.WriteLine($"Generating test files for scenario: {scenario}\n");

    var generator = new MasterGenerator();
    var result = generator.GenerateScenario(scenario);

    if (result.Success)
    {
        Console.WriteLine($"\n✅ Success!");
        Console.WriteLine($"Files generated: {result.FilesGenerated}");
        Console.WriteLine($"Records generated: {result.RecordsGenerated:N0}");
        Console.WriteLine($"Location: TestFiles/{scenario}/");
    }
    else
    {
        Console.WriteLine($"\n❌ Failed: {result.ErrorMessage}");
    }
}, scenarioArg);
rootCommand.AddCommand(generateScenarioCommand);

// Command: clean
var cleanCommand = new Command("clean", "Delete all generated test files");
cleanCommand.SetHandler(() =>
{
    Console.WriteLine("Cleaning all generated test files...\n");

    var testFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles");
    if (Directory.Exists(testFilesPath))
    {
        Directory.Delete(testFilesPath, true);
        Console.WriteLine("✅ All test files deleted.");
    }
    else
    {
        Console.WriteLine("⚠️  No test files found.");
    }
});
rootCommand.AddCommand(cleanCommand);

// Command: verify
var verifyCommand = new Command("verify", "Verify generated test files against checksums");
verifyCommand.SetHandler(() =>
{
    Console.WriteLine("Verifying test files...\n");
    Console.WriteLine("⚠️  Verification not yet implemented.");
});
rootCommand.AddCommand(verifyCommand);

// Execute command
return await rootCommand.InvokeAsync(args);

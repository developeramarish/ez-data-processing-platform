using DataProcessing.Shared.Entities;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace E2EDataSourceGenerator.Templates;

public static class E2E005Template
{
    public static DataProcessingDataSource CreateE2E005ScheduledPolling()
    {
        // Simple transaction schema (reuse from E2E-004)
        var jsonSchemaContent = JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = "Scheduled Polling Test Schema",
            properties = new Dictionary<string, object>
            {
                ["TransactionId"] = new { type = "string", pattern = "^TEST-\\d{3}$", description = "Test Transaction ID" },
                ["Amount"] = new { type = "number", minimum = 0, maximum = 10000, description = "Transaction Amount" },
                ["Timestamp"] = new { type = "string", format = "date-time", description = "Transaction Timestamp" }
            },
            required = new[] { "TransactionId", "Amount" }
        }, Formatting.None);

        var jsonSchemaBson = BsonDocument.Parse(jsonSchemaContent);

        // Single output destination (Folder JSON) - keep it simple for timing test
        var outputDestinations = new List<OutputDestination>
        {
            new OutputDestination
            {
                Id = Guid.NewGuid().ToString(),
                Name = "E2E-005-Folder-JSON",
                Description = "Folder output for scheduled polling test",
                Type = "folder",
                Enabled = true,
                OutputFormat = "json",
                FolderConfig = new FolderOutputConfig
                {
                    Path = "/mnt/external-test-data/output/E2E-005-folder-json",
                    FileNamePattern = "E2E-005_JSON_{filename}_{timestamp}.json",
                    CreateSubfolders = false
                }
            }
        };

        var outputConfig = new OutputConfiguration
        {
            DefaultOutputFormat = "original",
            IncludeInvalidRecords = false,
            Destinations = outputDestinations
        };

        // Configuration settings for frontend compatibility
        var configurationSettings = new
        {
            connectionConfig = new
            {
                type = "Local",
                path = "/mnt/external-test-data/E2E-005/",
                filePattern = "*.csv"
            },
            fileConfig = new
            {
                type = "CSV",
                delimiter = ",",
                hasHeaders = true,
                encoding = "UTF-8"
            },
            schedule = new
            {
                enabled = true,
                frequency = "Custom",
                cronExpression = "0 */1 * * * *"  // Every 1 minute (6-field Quartz format)
            },
            validationRules = new
            {
                skipInvalidRecords = false,
                maxErrorsAllowed = 100
            },
            outputConfig = new
            {
                defaultOutputFormat = "original",
                includeInvalidRecords = false,
                destinations = outputDestinations
            }
        };

        // Create DataSource entity
        var datasource = new DataProcessingDataSource
        {
            Name = "E2E-005",
            SupplierName = "Scheduled Polling Test Inc.",
            FilePath = "/mnt/external-test-data/E2E-005/",
            FilePattern = "*.csv",
            Category = "E2E-Testing",
            SchemaVersion = 1,
            IsActive = true,
            Description = "E2E-005: Scheduled polling verification - Tests 1-minute cron schedule with precise timing verification",
            JsonSchema = jsonSchemaBson,
            CronExpression = "0 */1 * * * *",  // Every 1 minute (Quartz 6-field format)
            PollingRate = TimeSpan.FromMinutes(1),  // 1 minute polling interval
            Output = outputConfig,
            AdditionalConfiguration = new BsonDocument
            {
                { "ConfigurationSettings", JsonConvert.SerializeObject(configurationSettings) },
                { "RetentionDays", 30 }
            },
            CreatedBy = "E2ETestGenerator",
            CorrelationId = Guid.NewGuid().ToString()
        };

        return datasource;
    }
}

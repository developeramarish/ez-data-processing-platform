using DataProcessing.Shared.Entities;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace E2EDataSourceGenerator.Templates;

public static class E2E004Template
{
    public static DataProcessingDataSource CreateE2E004MultiDestination()
    {
        // Simple customer transaction schema (similar to E2E-001)
        var jsonSchemaContent = JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = "Customer Transaction Schema",
            properties = new Dictionary<string, object>
            {
                ["TransactionId"] = new { type = "string", pattern = "^TXN-\\d{8}$", description = "Transaction ID" },
                ["CustomerName"] = new { type = "string", minLength = 2, maxLength = 100, description = "Customer Name" },
                ["Amount"] = new { type = "number", minimum = 0, maximum = 1000000, description = "Transaction Amount" },
                ["Date"] = new { type = "string", format = "date", description = "Transaction Date" },
                ["Status"] = new { type = "string", @enum = new[] { "Pending", "Approved", "Rejected" }, description = "Transaction Status" },
                ["Category"] = new { type = "string", description = "Transaction Category" },
                ["PaymentMethod"] = new { type = "string", @enum = new[] { "Credit Card", "Bank Transfer", "Cash" }, description = "Payment Method" }
            },
            required = new[] { "TransactionId", "CustomerName", "Amount", "Date", "Status" }
        }, Formatting.None);

        // Parse schema to BsonDocument
        var jsonSchemaBson = BsonDocument.Parse(jsonSchemaContent);

        // Output configuration with 4 destinations (2 Folder, 2 Kafka, 2 formats)
        var outputDestinations = new List<OutputDestination>
        {
            // Destination 1: Folder with JSON format
            new OutputDestination
            {
                Id = Guid.NewGuid().ToString(),
                Name = "E2E-004-Folder-JSON",
                Description = "Folder output in JSON format",
                Type = "folder",
                Enabled = true,
                OutputFormat = "json",
                FolderConfig = new FolderOutputConfig
                {
                    Path = "/mnt/external-test-data/output/E2E-004-folder-json",
                    FileNamePattern = "E2E-004_JSON_{filename}_{timestamp}.json",
                    CreateSubfolders = false
                }
            },

            // Destination 2: Folder with CSV format
            new OutputDestination
            {
                Id = Guid.NewGuid().ToString(),
                Name = "E2E-004-Folder-CSV",
                Description = "Folder output in CSV format",
                Type = "folder",
                Enabled = true,
                OutputFormat = "csv",
                FolderConfig = new FolderOutputConfig
                {
                    Path = "/mnt/external-test-data/output/E2E-004-folder-csv",
                    FileNamePattern = "E2E-004_CSV_{filename}_{timestamp}.csv",
                    CreateSubfolders = false
                }
            },

            // Destination 3: Kafka with JSON format
            new OutputDestination
            {
                Id = Guid.NewGuid().ToString(),
                Name = "E2E-004-Kafka-JSON",
                Description = "Kafka output in JSON format",
                Type = "kafka",
                Enabled = true,
                OutputFormat = "json",
                KafkaConfig = new KafkaOutputConfig
                {
                    Topic = "e2e-004-json-output",
                    BrokerServer = "localhost:9092",
                    MessageKey = "{filename}_{timestamp}",
                    Headers = new Dictionary<string, string>
                    {
                        ["format"] = "json",
                        ["test"] = "e2e-004"
                    },
                    SecurityProtocol = "PLAINTEXT"
                }
            },

            // Destination 4: Kafka with CSV format
            new OutputDestination
            {
                Id = Guid.NewGuid().ToString(),
                Name = "E2E-004-Kafka-CSV",
                Description = "Kafka output in CSV format",
                Type = "kafka",
                Enabled = true,
                OutputFormat = "csv",
                KafkaConfig = new KafkaOutputConfig
                {
                    Topic = "e2e-004-csv-output",
                    BrokerServer = "localhost:9092",
                    MessageKey = "{filename}_{timestamp}",
                    Headers = new Dictionary<string, string>
                    {
                        ["format"] = "csv",
                        ["test"] = "e2e-004"
                    },
                    SecurityProtocol = "PLAINTEXT"
                }
            }
        };

        // Output configuration
        var outputConfig = new OutputConfiguration
        {
            DefaultOutputFormat = "original",
            IncludeInvalidRecords = false,
            Destinations = outputDestinations
        };

        // Configuration settings for frontend compatibility (AdditionalConfiguration)
        var configurationSettings = new
        {
            connectionConfig = new
            {
                type = "Local",
                path = "/mnt/external-test-data/E2E-004/",
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
                frequency = "Every5Minutes",
                cronExpression = "0 */5 * * * *"  // Every 5 minutes (6-field Quartz format)
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
            Name = "E2E-004",
            SupplierName = "Multi-Destination Test Inc.",
            FilePath = "/mnt/external-test-data/E2E-004/",
            FilePattern = "*.csv",
            Category = "E2E-Testing",
            SchemaVersion = 1,
            IsActive = true,
            Description = "E2E-004: Multi-destination output test - 2 Folders (JSON+CSV) + 2 Kafka topics (JSON+CSV)",
            JsonSchema = jsonSchemaBson,
            CronExpression = "0 */5 * * * *",  // Every 5 minutes
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

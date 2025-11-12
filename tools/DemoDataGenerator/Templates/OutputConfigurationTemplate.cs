// OutputConfigurationTemplate.cs - Template for Generating Multi-Destination Output Configs
// Task-22: DemoDataGenerator Enhancement
// Version: 1.0
// Date: November 12, 2025

using DataProcessing.Shared.Entities;

namespace DemoDataGenerator.Templates;

/// <summary>
/// Template for generating realistic multi-destination output configurations
/// </summary>
public static class OutputConfigurationTemplate
{
    /// <summary>
    /// Generate output configuration based on datasource type
    /// </summary>
    public static OutputConfiguration Generate(string datasourceName, string fileType)
    {
        var config = new OutputConfiguration
        {
            DefaultOutputFormat = "original",
            IncludeInvalidRecords = false,
            Destinations = new List<OutputDestination>()
        };

        // Add 2-3 destinations per datasource (realistic enterprise scenario)
        
        // Destination 1: Real-time Kafka topic (always JSON)
        config.Destinations.Add(new OutputDestination
        {
            Name = "Real-Time Analytics",
            Type = "kafka",
            Enabled = true,
            OutputFormat = "json", // Override: Always JSON for real-time
            KafkaConfig = new KafkaOutputConfig
            {
                Topic = $"{datasourceName.ToLower().Replace(" ", "-")}-validated",
                MessageKey = "{filename}_{timestamp}",
                Headers = new Dictionary<string, string>
                {
                    ["source"] = datasourceName,
                    ["environment"] = "development",
                    ["producer"] = "output-service"
                }
            }
        });

        // Destination 2: Daily archive folder (original format)
        config.Destinations.Add(new OutputDestination
        {
            Name = "Daily Archive",
            Type = "folder",
            Enabled = true,
            FolderConfig = new FolderOutputConfig
            {
                Path = $@"C:\DataProcessing\Archive\{datasourceName.Replace(" ", "")}",
                FileNamePattern = "{filename}_{date}_valid.{ext}",
                CreateSubfolders = true,
                SubfolderPattern = "{year}/{month}/{day}",
                OverwriteExisting = false
            }
            // OutputFormat = null (use default: "original")
        });

        // Destination 3: Analytics team folder (CSV for easy analysis)
        if (fileType != "CSV") // Only add if source is not CSV
        {
            config.Destinations.Add(new OutputDestination
            {
                Name = "Analytics Team Export",
                Type = "folder",
                Enabled = true,
                OutputFormat = "csv", // Override: Always CSV for analytics
                IncludeInvalidRecords = true, // Override: Include for debugging
                FolderConfig = new FolderOutputConfig
                {
                    Path = $@"C:\DataProcessing\Analytics\{datasourceName.Replace(" ", "")}",
                    FileNamePattern = "analytics_{date}.csv",
                    CreateSubfolders = false,
                    OverwriteExisting = true // Analytics wants latest data
                }
            });
        }

        return config;
    }

    /// <summary>
    /// Generate specific configurations for different scenarios
    /// </summary>
    public static class Scenarios
    {
        /// <summary>
        /// Banking scenario: High-compliance with multiple audit destinations
        /// </summary>
        public static OutputConfiguration BankingCompliance(string datasourceName)
        {
            return new OutputConfiguration
            {
                DefaultOutputFormat = "original",
                IncludeInvalidRecords = false,
                Destinations = new List<OutputDestination>
                {
                    // Real-time fraud detection
                    new() {
                        Name = "Fraud Detection System",
                        Type = "kafka",
                        Enabled = true,
                        OutputFormat = "json",
                        KafkaConfig = new() {
                            Topic = "fraud-detection-input",
                            MessageKey = "{datasource}_{timestamp}",
                            Headers = new() { ["priority"] = "high", ["system"] = "fraud" }
                        }
                    },
                    // Regulatory archive (7 years retention)
                    new() {
                        Name = "Regulatory Archive",
                        Type = "folder",
                        Enabled = true,
                        FolderConfig = new() {
                            Path = @"C:\Compliance\Banking\Transactions",
                            FileNamePattern = "{filename}_{timestamp}_validated.{ext}",
                            CreateSubfolders = true,
                            SubfolderPattern = "{year}/{month}",
                            OverwriteExisting = false
                        }
                    },
                    // Risk analytics (CSV for Excel)
                    new() {
                        Name = "Risk Analytics",
                        Type = "folder",
                        Enabled = true,
                        OutputFormat = "csv",
                        FolderConfig = new() {
                            Path = @"C:\Analytics\Risk",
                            FileNamePattern = "risk_data_{date}.csv",
                            OverwriteExisting = true
                        }
                    },
                    // Audit log (Kafka with all records including invalid)
                    new() {
                        Name = "Audit Log",
                        Type = "kafka",
                        Enabled = true,
                        OutputFormat = "json",
                        IncludeInvalidRecords = true, // Include invalid for audit trail
                        KafkaConfig = new() {
                            Topic = "audit-log-all-records",
                            Headers = new() { ["record-type"] = "validated-and-invalid" }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Simple scenario: Basic Kafka + Archive
        /// </summary>
        public static OutputConfiguration Simple(string datasourceName)
        {
            return new OutputConfiguration
            {
                DefaultOutputFormat = "original",
                Destinations = new List<OutputDestination>
                {
                    new() {
                        Name = "Primary Kafka Topic",
                        Type = "kafka",
                        Enabled = true,
                        KafkaConfig = new() { Topic = $"{datasourceName.ToLower()}-validated" }
                    },
                    new() {
                        Name = "Backup Archive",
                        Type = "folder",
                        Enabled = true,
                        FolderConfig = new() { Path = $@"C:\Archive\{datasourceName}" }
                    }
                }
            };
        }
    }
}

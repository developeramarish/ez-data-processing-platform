using DataProcessing.Shared.Entities;
using MetricsConfigurationService.Models;
using MongoDB.Bson;
using MongoDB.Entities;
using DemoDataGenerator.Models;
using DemoDataGenerator.Templates;
using Newtonsoft.Json;

namespace DemoDataGenerator.Generators;

public class DataSourceGenerator
{
    private readonly Random _random;
    
    public DataSourceGenerator(Random random)
    {
        _random = random;
    }
    
    public async Task<List<DataProcessingDataSource>> GenerateAsync()
    {
        Console.WriteLine("[2/7] 📊 Generating 20 datasources...");
        var datasources = new List<DataProcessingDataSource>();
        
        // Various file patterns
        var filePatterns = new[] { "*.json", "*.xml", "*.csv", "*.xlsx", "*.txt" };
        
        // Various cron expressions (6-field Quartz format: second minute hour day month dayOfWeek)
        var cronExpressions = new[]
        {
            "0 */5 * * * *",      // Every 5 minutes
            "0 */15 * * * *",     // Every 15 minutes
            "0 */30 * * * *",     // Every 30 minutes
            "0 0 * * * *",        // Every hour
            "0 0 */3 * * *",      // Every 3 hours
            "0 0 */6 * * *",      // Every 6 hours
            "0 0 9 * * *",        // Daily at 9:00 AM
            "0 30 14 * * *",      // Daily at 2:30 PM
            "0 0 0 * * *",        // Daily at midnight
            "0 0 6,18 * * *",     // Twice daily (6 AM and 6 PM)
            "0 0 8 * * MON-FRI",  // Weekdays at 8 AM
            "0 0 10 1 * *",       // Monthly on 1st at 10 AM
            "*/30 * * * * *",     // Every 30 seconds
            "0 */2 * * * *"       // Every 2 minutes
        };
        
        // Various connection types matching frontend constants (Local, SFTP, FTP, HTTP, Kafka)
        var connectionTypes = new[] { "Local", "FTP", "SFTP", "HTTP", "Kafka" };
        var protocolVersions = new[] { "v1", "v2", "v3" };
        
        for (int i = 0; i < DemoConfig.DataSourceCount; i++)
        {
            var category = HebrewCategories.All[i % HebrewCategories.All.Length];
            var connType = connectionTypes[i % connectionTypes.Length];
            var filePattern = filePatterns[i % filePatterns.Length];
            var cronExpr = cronExpressions[i % cronExpressions.Length];
            
            // Create varied file paths based on connection type
            var filePath = connType switch
            {
                "Local" => $"/data/{category}/{i + 1:D3}",
                "FTP" => $"ftp://files.example.com/{category}/incoming",
                "SFTP" => $"sftp://secure.example.com:22/{category}/data",
                "HTTP" => $"https://api.example.com/{category}/files",
                "Kafka" => $"kafka://broker.example.com:9092/{category}_topic",
                _ => $"/data/{category}/{i + 1:D3}"
            };
            
            // Create AdditionalConfiguration with connection details
            var additionalConfig = new BsonDocument
            {
                { "connectionType", connType },
                { "protocol", protocolVersions[i % protocolVersions.Length] },
                { "timeout", _random.Next(30, 301) }, // 30-300 seconds
                { "retryCount", _random.Next(3, 6) }, // 3-5 retries
                { "bufferSize", _random.Next(1024, 8193) * 1024 } // 1-8 MB
            };
            
            // Add connection-specific settings
            if (connType == "FTP" || connType == "SFTP")
            {
                additionalConfig["port"] = connType == "FTP" ? 21 : 22;
                additionalConfig["username"] = "datasync_user";
                additionalConfig["passiveMode"] = connType == "FTP" && i % 2 == 0;
            }
            else if (connType == "HTTP")
            {
                additionalConfig["method"] = i % 2 == 0 ? "GET" : "POST";
                additionalConfig["headers"] = new BsonDocument { { "Authorization", "Bearer token" } };
            }
            else if (connType == "Kafka")
            {
                additionalConfig["brokers"] = "broker1.example.com:9092,broker2.example.com:9092";
                additionalConfig["topic"] = $"{category}_events";
                additionalConfig["consumerGroup"] = $"dataprocessing_{category}_group";
                additionalConfig["securityProtocol"] = i % 2 == 0 ? "PLAINTEXT" : "SASL_SSL";
            }
            
            // Create ConfigurationSettings JSON for frontend
            var configurationSettings = new
            {
                connectionConfig = new
                {
                    type = connType,
                    host = connType switch
                    {
                        "FTP" => "files.example.com",
                        "SFTP" => "secure.example.com",
                        "HTTP" => "api.example.com",
                        "Kafka" => "broker.example.com",
                        _ => "localhost"
                    },
                    port = connType switch
                    {
                        "FTP" => 21,
                        "SFTP" => 22,
                        "HTTP" => 443,
                        "Kafka" => 9092,
                        _ => 0
                    },
                    path = filePath,
                    kafkaBrokers = connType == "Kafka" ? "broker1.example.com:9092,broker2.example.com:9092" : (string?)null,
                    kafkaTopic = connType == "Kafka" ? $"{category}_events" : (string?)null,
                    kafkaConsumerGroup = connType == "Kafka" ? $"dataprocessing_{category}_group" : (string?)null,
                    kafkaSecurityProtocol = connType == "Kafka" ? (i % 2 == 0 ? "PLAINTEXT" : "SASL_SSL") : (string?)null
                },
                fileConfig = new
                {
                    type = filePattern.TrimStart('*', '.').ToUpper(),
                    delimiter = ",",
                    hasHeaders = true,
                    encoding = "UTF-8"
                },
                schedule = new
                {
                    frequency = "Custom",  // Since we're using explicit cron expressions
                    cronExpression = cronExpr,
                    enabled = true
                },
                validationRules = new
                {
                    skipInvalidRecords = false,
                    maxErrorsAllowed = 100
                },
                notificationSettings = new
                {
                    onSuccess = false,
                    onFailure = true,
                    recipients = new[] { "admin@example.com" }
                },
                outputConfig = GenerateOutputConfiguration(DemoConfig.DataSourceNames[i], filePattern, i)
            };
            
            var datasource = new DataProcessingDataSource
            {
                Name = DemoConfig.DataSourceNames[i],
                SupplierName = DemoConfig.SupplierNames[i % DemoConfig.SupplierNames.Length],
                FilePath = filePath,
                CronExpression = cronExpr,  // Use explicit cron instead of PollingRate
                JsonSchema = new BsonDocument(),
                Category = category,
                SchemaVersion = 1,
                IsActive = true,
                FilePattern = filePattern,
                AdditionalConfiguration = new BsonDocument
                {
                    { "ConfigurationSettings", JsonConvert.SerializeObject(configurationSettings) },
                    { "ConnectionType", connType },
                    { "Protocol", protocolVersions[i % protocolVersions.Length] },
                    { "Timeout", _random.Next(30, 301) },
                    { "RetryCount", _random.Next(3, 6) },
                    { "BufferSize", _random.Next(1024, 8193) * 1024 }
                },
                Description = $"מקור נתונים עבור {DemoConfig.DataSourceNames[i]} - {category} ({connType})",
                CreatedBy = "DemoGenerator",
                CorrelationId = Guid.NewGuid().ToString()
            };
            
            await datasource.SaveAsync();
            datasources.Add(datasource);
            Console.WriteLine($"  ✓ Created: {datasource.Name} ({datasource.Category}, {connType}, {filePattern})");
        }
        
        Console.WriteLine($"  ✅ Generated {datasources.Count} datasources\n");
        return datasources;
    }

    /// <summary>
    /// Generate varied output configurations for different datasources
    /// </summary>
    private static OutputConfiguration GenerateOutputConfiguration(string datasourceName, string filePattern, int index)
    {
        var fileType = filePattern.TrimStart('*', '.').ToUpper();

        // Use different scenarios for variety
        return index switch
        {
            0 or 5 or 10 or 15 => OutputConfigurationTemplate.Scenarios.BankingCompliance(datasourceName), // 4 destinations
            1 or 6 or 11 or 16 => OutputConfigurationTemplate.Scenarios.Simple(datasourceName),           // 2 destinations
            _ => OutputConfigurationTemplate.Generate(datasourceName, fileType)                           // 2-3 destinations (standard)
        };
    }
}

public class SchemaGenerator
{
    private readonly Random _random;
    
    public SchemaGenerator(Random random)
    {
        _random = random;
    }
    
    public async Task GenerateForDataSourcesAsync(List<DataProcessingDataSource> datasources)
    {
        Console.WriteLine("[3/7] 📋 Generating complex JSON schemas...");
        
        for (int i = 0; i < datasources.Count; i++)
        {
            var ds = datasources[i];
            var jsonSchemaContent = GenerateComplexJsonSchema(i, ds.Category);
            
            // Create separate DataProcessingSchema entity
            var schema = new DataProcessingSchema
            {
                Name = $"schema_{i + 1:D3}_{ds.Category}",
                DisplayName = $"סכמה עבור {ds.Name}",
                Description = $"סכמת JSON מורכבת עם אימותים מתקדמים עבור {ds.Name}",
                DataSourceId = ds.ID,
                JsonSchemaContent = jsonSchemaContent,
                Tags = new List<string> { ds.Category, "demo", "complex" },
                Status = SchemaStatus.Active,
                SchemaVersionNumber = 1,
                CreatedBy = "DemoGenerator",
                CorrelationId = Guid.NewGuid().ToString()
            };
            
            await schema.SaveAsync();
            
            // CRITICAL FIX: Also populate the embedded JsonSchema property on DataSource
            // Parse JSON string to BsonDocument for embedded storage
            ds.JsonSchema = BsonDocument.Parse(jsonSchemaContent);
            await ds.SaveAsync();
            
            Console.WriteLine($"  ✓ Schema for: {ds.Name}");
        }
        
        Console.WriteLine($"  ✅ Generated {datasources.Count} complex schemas\n");
    }
    
    private string GenerateComplexJsonSchema(int index, string category)
    {
        // Generate truly unique schemas based on index with varying complexity
        return index switch
        {
            0 => GenerateSimpleTransactionSchema(category),
            1 => GenerateUserProfileSchema(category),
            2 => GenerateProductCatalogSchema(category),
            3 => GenerateEmployeeRecordSchema(category),
            4 => GenerateFinancialReportSchema(category),
            5 => GenerateSurveyResponseSchema(category),
            6 => GenerateOrderManagementSchema(category),
            7 => GenerateInventoryItemSchema(category),
            8 => GenerateMarketingCampaignSchema(category),
            9 => GenerateCustomerSupportTicketSchema(category),
            10 => GenerateShipmentTrackingSchema(category),
            11 => GenerateSalesAnalyticsSchema(category),
            12 => GenerateOperationalPlanSchema(category),
            13 => GenerateMarketResearchSchema(category),
            14 => GenerateProcurementOrderSchema(category),
            15 => GenerateProjectManagementSchema(category),
            16 => GenerateQualityControlSchema(category),
            17 => GenerateSupplierOrderSchema(category),
            18 => GenerateCompetitorAnalysisSchema(category),
            19 => GenerateSalesTargetSchema(category),
            _ => GenerateSimpleTransactionSchema(category)
        };
    }
    
    // Simple schema - 4 fields, no nesting
    private string GenerateSimpleTransactionSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"עסקה פשוטה - {category}",
            properties = new Dictionary<string, object>
            {
                ["transactionId"] = new { type = "string", pattern = "^TXN-\\d{8}$", description = "מזהה ייחודי של העסקה" },
                ["amount"] = new { type = "number", minimum = 0, maximum = 1000000, description = "סכום העסקה בשקלים" },
                ["date"] = new { type = "string", format = "date", description = "תאריך ביצוע העסקה" },
                ["status"] = new { type = "string", @enum = new[] { "ממתין", "אושר", "נדחה" }, description = "סטטוס העסקה" }
            },
            required = new[] { "transactionId", "amount", "date" }
        }, Formatting.None);
    
    // Medium complexity - 6 fields, 1 level nesting
    private string GenerateUserProfileSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"פרופיל משתמש - {category}",
            properties = new Dictionary<string, object>
            {
                ["userId"] = new { type = "string", format = "uuid", description = "מזהה ייחודי של המשתמש" },
                ["email"] = new { type = "string", format = "email", description = "כתובת דוא\"ל" },
                ["fullName"] = new { type = "string", minLength = 2, maxLength = 100, description = "שם מלא" },
                ["birthDate"] = new { type = "string", format = "date", description = "תאריך לידה" },
                ["preferences"] = new
                {
                    type = "object",
                    description = "העדפות משתמש",
                    properties = new Dictionary<string, object>
                    {
                        ["language"] = new { type = "string", @enum = new[] { "he", "en", "ar" }, description = "שפת ממשק" },
                        ["notifications"] = new { type = "boolean", description = "קבלת התראות" }
                    }
                },
                ["tags"] = new { type = "array", items = new { type = "string" }, description = "תגיות משתמש" }
            },
            required = new[] { "userId", "email", "fullName" }
        }, Formatting.None);
    
    // Complex - 8 fields, 2 levels nesting, arrays
    private string GenerateProductCatalogSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"קטלוג מוצרים - {category}",
            properties = new Dictionary<string, object>
            {
                ["productId"] = new { type = "string", pattern = "^PRD-[0-9]{6}$", description = "מזהה מוצר ייחודי" },
                ["name"] = new { type = "string", maxLength = 200, description = "שם המוצר" },
                ["description"] = new { type = "string", description = "תיאור מפורט של המוצר" },
                ["price"] = new { type = "number", minimum = 0, multipleOf = 0.01, description = "מחיר המוצר" },
                ["stock"] = new { type = "integer", minimum = 0, description = "כמות במלאי" },
                ["category"] = new { type = "string", @enum = HebrewCategories.All, description = "קטגוריית המוצר" },
                ["variants"] = new
                {
                    type = "array",
                    description = "וריאציות של המוצר",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["sku"] = new { type = "string", description = "קוד מלאי" },
                            ["color"] = new { type = "string", description = "צבע" },
                            ["size"] = new { type = "string", description = "גודל" },
                            ["additionalPrice"] = new { type = "number", description = "תוספת מחיר" }
                        }
                    }
                },
                ["metadata"] = new
                {
                    type = "object",
                    description = "מידע נוסף",
                    additionalProperties = new { type = "string" }
                }
            },
            required = new[] { "productId", "name", "price", "category" }
        }, Formatting.None);
    
    // Advanced - Uses if/then/else, dependencies
    private string GenerateEmployeeRecordSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"רשומת עובד - {category}",
            properties = new Dictionary<string, object>
            {
                ["employeeId"] = new { type = "string", pattern = "^EMP\\d{5}$", description = "מספר עובד" },
                ["firstName"] = new { type = "string", description = "שם פרטי" },
                ["lastName"] = new { type = "string", description = "שם משפחה" },
                ["department"] = new { type = "string", description = "מחלקה" },
                ["position"] = new { type = "string", description = "תפקיד" },
                ["salary"] = new { type = "number", minimum = 5000, description = "שכר חודשי" },
                ["isManager"] = new { type = "boolean", description = "האם מנהל" },
                ["managedTeam"] = new
                {
                    type = "array",
                    description = "רשימת עובדים בניהול",
                    items = new { type = "string" }
                }
            },
            required = new[] { "employeeId", "firstName", "lastName", "department" },
            @if = new { properties = new Dictionary<string, object> { ["isManager"] = new { @const = true } } },
            then = new { required = new[] { "employeeId", "firstName", "lastName", "department", "managedTeam" } }
        }, Formatting.None);
    
    // Very complex - 12 fields, 3 levels nesting, oneOf
    private string GenerateFinancialReportSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"דוח כספי - {category}",
            properties = new Dictionary<string, object>
            {
                ["reportId"] = new { type = "string", description = "מזהה דוח" },
                ["fiscalYear"] = new { type = "integer", minimum = 2000, maximum = 2100, description = "שנת כספים" },
                ["quarter"] = new { type = "integer", minimum = 1, maximum = 4, description = "רבעון" },
                ["revenue"] = new { type = "number", minimum = 0, description = "הכנסות" },
                ["expenses"] = new { type = "number", minimum = 0, description = "הוצאות" },
                ["profit"] = new { type = "number", description = "רווח" },
                ["currency"] = new { type = "string", @enum = new[] { "ILS", "USD", "EUR" }, description = "מטבע" },
                ["breakdown"] = new
                {
                    type = "object",
                    description = "פירוט הכנסות",
                    properties = new Dictionary<string, object>
                    {
                        ["sales"] = new { type = "number", description = "מכירות" },
                        ["services"] = new { type = "number", description = "שירותים" },
                        ["other"] = new { type = "number", description = "אחר" }
                    }
                },
                ["comparisons"] = new
                {
                    type = "object",
                    description = "השוואות",
                    properties = new Dictionary<string, object>
                    {
                        ["previousQuarter"] = new
                        {
                            type = "object",
                            description = "רבעון קודם",
                            properties = new Dictionary<string, object>
                            {
                                ["revenue"] = new { type = "number", description = "הכנסות" },
                                ["growth"] = new { type = "number", description = "צמיחה באחוזים" }
                            }
                        }
                    }
                },
                ["approved"] = new { type = "boolean", description = "האם אושר" },
                ["approver"] = new { type = "string", description = "מאשר" },
                ["notes"] = new { type = "array", items = new { type = "string" }, description = "הערות" }
            },
            required = new[] { "reportId", "fiscalYear", "revenue", "expenses" }
        }, Formatting.None);
    
    // Medium with patterns - 5 fields, regex patterns
    private string GenerateSurveyResponseSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"תשובת סקר - {category}",
            properties = new Dictionary<string, object>
            {
                ["responseId"] = new { type = "string", format = "uuid" },
                ["surveyName"] = new { type = "string" },
                ["respondentEmail"] = new { type = "string", format = "email" },
                ["rating"] = new { type = "integer", minimum = 1, maximum = 5 },
                ["feedback"] = new { type = "string", maxLength = 1000 },
                ["submittedAt"] = new { type = "string", format = "date-time" }
            },
            required = new[] { "responseId", "rating" }
        }, Formatting.None);
    
    // Complex arrays - 7 fields with array validations
    private string GenerateOrderManagementSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"ניהול הזמנה - {category}",
            properties = new Dictionary<string, object>
            {
                ["orderId"] = new { type = "string" },
                ["customerId"] = new { type = "string" },
                ["orderDate"] = new { type = "string", format = "date-time" },
                ["items"] = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = 20,
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["productId"] = new { type = "string" },
                            ["quantity"] = new { type = "integer", minimum = 1 },
                            ["unitPrice"] = new { type = "number", minimum = 0 }
                        },
                        required = new[] { "productId", "quantity", "unitPrice" }
                    }
                },
                ["shippingAddress"] = new { type = "string" },
                ["totalAmount"] = new { type = "number", minimum = 0 },
                ["paymentMethod"] = new { type = "string", @enum = new[] { "כרטיס אשראי", "העברה בנקאית", "מזומן" } }
            },
            required = new[] { "orderId", "customerId", "items", "totalAmount" }
        }, Formatting.None);
    
    // Simple with numbers - 6 fields, number validations
    private string GenerateInventoryItemSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"פריט במלאי - {category}",
            properties = new Dictionary<string, object>
            {
                ["sku"] = new { type = "string", pattern = "^SKU-[A-Z0-9]{8}$" },
                ["itemName"] = new { type = "string" },
                ["quantity"] = new { type = "integer", minimum = 0 },
                ["reorderLevel"] = new { type = "integer", minimum = 0 },
                ["location"] = new { type = "string" },
                ["lastStockUpdate"] = new { type = "string", format = "date-time" }
            },
            required = new[] { "sku", "itemName", "quantity" }
        }, Formatting.None);
    
    // Medium with dependencies
    private string GenerateMarketingCampaignSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"קמפיין שיווקי - {category}",
            properties = new Dictionary<string, object>
            {
                ["campaignId"] = new { type = "string" },
                ["name"] = new { type = "string" },
                ["startDate"] = new { type = "string", format = "date" },
                ["endDate"] = new { type = "string", format = "date" },
                ["budget"] = new { type = "number", minimum = 0 },
                ["channels"] = new { type = "array", items = new { type = "string", @enum = new[] { "אימייל", "SMS", "רשתות חברתיות", "מודעות" } } },
                ["targetAudience"] = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["ageRange"] = new { type = "string" },
                        ["location"] = new { type = "string" }
                    }
                }
            },
            required = new[] { "campaignId", "name", "startDate", "budget" }
        }, Formatting.None);
    
    // Advanced - Uses allOf, anyOf
    private string GenerateCustomerSupportTicketSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"פניית תמיכה - {category}",
            properties = new Dictionary<string, object>
            {
                ["ticketId"] = new { type = "string" },
                ["priority"] = new { type = "string", @enum = new[] { "נמוכה", "בינונית", "גבוהה", "קריטית" } },
                ["status"] = new { type = "string", @enum = new[] { "פתוח", "בטיפול", "ממתין", "נסגר" } },
                ["category"] = new { type = "string" },
                ["description"] = new { type = "string", minLength = 10 },
                ["assignedTo"] = new { type = "string" },
                ["resolution"] = new { type = "string" }
            },
            required = new[] { "ticketId", "priority", "description" },
            allOf = new[]
            {
                new
                {
                    @if = new { properties = new Dictionary<string, object> { ["status"] = new { @const = "נסגר" } } },
                    then = new { required = new[] { "resolution" } }
                }
            }
        }, Formatting.None);
    
    // Continue with more unique schemas for remaining datasources...
    private string GenerateShipmentTrackingSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"מעקב משלוח - {category}",
            properties = new Dictionary<string, object>
            {
                ["trackingNumber"] = new { type = "string", pattern = "^SHIP[0-9]{10}$" },
                ["origin"] = new { type = "string" },
                ["destination"] = new { type = "string" },
                ["currentLocation"] = new { type = "string" },
                ["estimatedDelivery"] = new { type = "string", format = "date-time" },
                ["status"] = new { type = "string", @enum = new[] { "בהכנה", "בדרך", "נמסר", "חזר לשולח" } },
                ["weight"] = new { type = "number", minimum = 0, maximum = 1000 }
            },
            required = new[] { "trackingNumber", "origin", "destination" }
        }, Formatting.None);
    
    private string GenerateSalesAnalyticsSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"ניתוח מכירות - {category}",
            properties = new Dictionary<string, object>
            {
                ["analysisId"] = new { type = "string" },
                ["period"] = new { type = "string", @enum = new[] { "יומי", "שבועי", "חודשי", "שנתי" } },
                ["totalSales"] = new { type = "number" },
                ["averageOrderValue"] = new { type = "number" },
                ["conversionRate"] = new { type = "number", minimum = 0, maximum = 100 },
                ["topProducts"] = new
                {
                    type = "array",
                    maxItems = 10,
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["productId"] = new { type = "string" },
                            ["salesCount"] = new { type = "integer" }
                        }
                    }
                }
            },
            required = new[] { "analysisId", "period", "totalSales" }
        }, Formatting.None);
    
    private string GenerateOperationalPlanSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"תוכנית תפעולית - {category}",
            properties = new Dictionary<string, object>
            {
                ["planId"] = new { type = "string" },
                ["title"] = new { type = "string", maxLength = 150 },
                ["objectives"] = new { type = "array", minItems = 1, items = new { type = "string" } },
                ["startDate"] = new { type = "string", format = "date" },
                ["endDate"] = new { type = "string", format = "date" },
                ["budget"] = new { type = "number", minimum = 0 },
                ["responsible"] = new { type = "string" }
            },
            required = new[] { "planId", "title", "objectives" }
        }, Formatting.None);
    
    private string GenerateMarketResearchSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"מחקר שוק - {category}",
            properties = new Dictionary<string, object>
            {
                ["researchId"] = new { type = "string" },
                ["marketSegment"] = new { type = "string" },
                ["sampleSize"] = new { type = "integer", minimum = 1 },
                ["findings"] = new { type = "array", items = new { type = "string" } },
                ["methodology"] = new { type = "string" },
                ["confidenceLevel"] = new { type = "number", minimum = 0, maximum = 100 }
            },
            required = new[] { "researchId", "marketSegment", "sampleSize" }
        }, Formatting.None);
    
    private string GenerateProcurementOrderSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"הזמנת רכש - {category}",
            properties = new Dictionary<string, object>
            {
                ["poNumber"] = new { type = "string", pattern = "^PO-\\d{8}$" },
                ["supplierId"] = new { type = "string" },
                ["items"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["itemCode"] = new { type = "string" },
                            ["description"] = new { type = "string" },
                            ["quantity"] = new { type = "integer" },
                            ["unitPrice"] = new { type = "number" }
                        }
                    }
                },
                ["deliveryDate"] = new { type = "string", format = "date" },
                ["totalValue"] = new { type = "number" }
            },
            required = new[] { "poNumber", "supplierId", "items" }
        }, Formatting.None);
    
    private string GenerateProjectManagementSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"ניהול פרויקט - {category}",
            properties = new Dictionary<string, object>
            {
                ["projectId"] = new { type = "string" },
                ["projectName"] = new { type = "string", maxLength = 200 },
                ["phase"] = new { type = "string", @enum = new[] { "תכנון", "ביצוע", "ניטור", "סגירה" } },
                ["milestones"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["name"] = new { type = "string" },
                            ["dueDate"] = new { type = "string", format = "date" },
                            ["completed"] = new { type = "boolean" }
                        }
                    }
                },
                ["riskLevel"] = new { type = "string", @enum = new[] { "נמוך", "בינוני", "גבוה" } }
            },
            required = new[] { "projectId", "projectName", "phase" }
        }, Formatting.None);
    
    private string GenerateQualityControlSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"בקרת איכות - {category}",
            properties = new Dictionary<string, object>
            {
                ["inspectionId"] = new { type = "string" },
                ["productBatch"] = new { type = "string" },
                ["inspectionDate"] = new { type = "string", format = "date" },
                ["passed"] = new { type = "boolean" },
                ["defectCount"] = new { type = "integer", minimum = 0 },
                ["inspector"] = new { type = "string" },
                ["notes"] = new { type = "string" }
            },
            required = new[] { "inspectionId", "productBatch", "passed" }
        }, Formatting.None);
    
    private string GenerateSupplierOrderSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"הזמנת ספק - {category}",
            properties = new Dictionary<string, object>
            {
                ["orderId"] = new { type = "string" },
                ["supplierId"] = new { type = "string", format = "uuid" },
                ["orderType"] = new { type = "string", @enum = new[] { "רגיל", "דחוף", "מתוכנן" } },
                ["deliveryTerms"] = new { type = "string" },
                ["paymentTerms"] = new { type = "string" },
                ["amount"] = new { type = "number", minimum = 0 }
            },
            required = new[] { "orderId", "supplierId", "amount" }
        }, Formatting.None);
    
    private string GenerateCompetitorAnalysisSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"ניתוח מתחרים - {category}",
            properties = new Dictionary<string, object>
            {
                ["analysisId"] = new { type = "string" },
                ["competitorName"] = new { type = "string" },
                ["marketShare"] = new { type = "number", minimum = 0, maximum = 100 },
                ["strengths"] = new { type = "array", items = new { type = "string" } },
                ["weaknesses"] = new { type = "array", items = new { type = "string" } },
                ["pricing"] = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["average"] = new { type = "number" },
                        ["minimum"] = new { type = "number" },
                        ["maximum"] = new { type = "number" }
                    }
                }
            },
            required = new[] { "analysisId", "competitorName" }
        }, Formatting.None);
    
    private string GenerateSalesTargetSchema(string category) =>
        JsonConvert.SerializeObject(new
        {
            schema = "https://json-schema.org/draft/2020-12/schema",
            type = "object",
            title = $"יעדי מכירות - {category}",
            properties = new Dictionary<string, object>
            {
                ["targetId"] = new { type = "string" },
                ["period"] = new { type = "string", @enum = new[] { "חודשי", "רבעוני", "שנתי" } },
                ["targetAmount"] = new { type = "number", minimum = 0 },
                ["actualAmount"] = new { type = "number", minimum = 0 },
                ["achievementRate"] = new { type = "number", minimum = 0, maximum = 200 },
                ["region"] = new { type = "string" },
                ["salesPerson"] = new { type = "string" },
                ["notes"] = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "targetId", "period", "targetAmount" }
        }, Formatting.None);
}

public class GlobalMetricGenerator
{
    private readonly Random _random;

    public GlobalMetricGenerator(Random random)
    {
        _random = random;
    }

    /// <summary>
    /// IMPORTANT: Global metrics are NOT generated by demo data because:
    ///
    /// 1. GLOBAL METRICS require field paths that exist in ALL datasource schemas
    /// 2. Each datasource in the demo has a DIFFERENT schema with unique field names:
    ///    - Transactions: $.transactionId, $.amount, $.status
    ///    - Users: $.userId, $.email, $.fullName
    ///    - Products: $.productId, $.name, $.price
    ///    - etc.
    /// 3. There's no common field path across all schemas
    ///
    /// HOW TO CREATE VALID GLOBAL METRICS:
    /// - Use the Metrics Configuration API/UI to create global metrics
    /// - Choose field paths that exist in ALL your datasources
    /// - Example: If all datasources have a "status" field, use $.Status
    /// - FormulaType must be "Simple" (JSON path extraction, not PromQL)
    ///
    /// DATASOURCE-SPECIFIC METRICS are generated for each datasource with
    /// valid field paths based on that datasource's actual schema.
    /// </summary>
    public Task GenerateAsync()
    {
        Console.WriteLine("[4/7] 📈 Global metrics generation...");
        Console.WriteLine("  ⚠️  Skipping global metrics generation - see documentation:");
        Console.WriteLine("      Global metrics require field paths common to ALL datasources.");
        Console.WriteLine("      Since each datasource has a unique schema (transactionId vs userId vs productId),");
        Console.WriteLine("      global metrics must be created manually based on your actual data model.");
        Console.WriteLine("      Use the Metrics Configuration API/UI to create global metrics with valid field paths.");
        Console.WriteLine("  ✅ No global metrics generated (use API/UI to create them)\n");

        return Task.CompletedTask;
    }
}

public class DatasourceMetricGenerator
{
    private readonly Random _random;
    
    public DatasourceMetricGenerator(Random random)
    {
        _random = random;
    }
    
    public async Task GenerateAsync(List<DataProcessingDataSource> datasources)
    {
        Console.WriteLine("[5/7] 📊 Generating datasource-specific metrics...");
        int totalMetrics = 0;
        
        for (int dsIndex = 0; dsIndex < datasources.Count; dsIndex++)
        {
            var ds = datasources[dsIndex];
            int metricsCount = _random.Next(2, 5); // 2-4 metrics per datasource
            
            // Convert Hebrew category to ASCII for Prometheus-compliant metric names
            var categoryAscii = HebrewCategories.ToAscii(ds.Category);
            
            // Get schema-specific field paths
            var fields = SchemaFieldMappings.GetFieldPaths(dsIndex);
            
            for (int i = 0; i < metricsCount; i++)
            {
                // Use actual fields from the schema for this datasource
                // IMPORTANT: For Simple formula type, formula should equal fieldPath
                var metricTypes = new[]
                {
                    ($"count_{categoryAscii}_{totalMetrics}", $"ספירה - {ds.Name}", fields.count, "counter", fields.count, FormulaType.Simple),
                    ($"sum_{categoryAscii}_{totalMetrics}", $"סיכום - {ds.Name}", fields.sum, "counter", fields.sum, FormulaType.Simple),
                    ($"avg_{categoryAscii}_{totalMetrics}", $"ממוצע - {ds.Name}", fields.avg, "gauge", fields.avg, FormulaType.Simple)
                };
                
                var (name, displayName, formula, promType, fieldPath, formulaType) = metricTypes[i % metricTypes.Length];
                
                var metric = new MetricConfiguration
                {
                    Name = name.Replace(" ", "_").Replace(",", ""),
                    DisplayName = displayName,
                    Description = $"מדד ספציפי עבור {ds.Name}",
                    Category = ds.Category,
                    Scope = "datasource-specific",
                    DataSourceId = ds.ID,
                    DataSourceName = ds.Name,  // Add datasource name for frontend display
                    Formula = formula,
                    FormulaType = formulaType,  // Use explicit FormulaType from tuple
                    FieldPath = fieldPath,
                    PrometheusType = promType,
                    Status = 1,
                    CreatedBy = "DemoGenerator"
                };
                
                await metric.SaveAsync();
                totalMetrics++;
            }
            
            Console.WriteLine($"  ✓ {metricsCount} metrics for: {ds.Name}");
        }
        
        Console.WriteLine($"  ✅ Generated {totalMetrics} datasource-specific metrics\n");
    }
}

public class AlertGenerator
{
    private readonly Random _random;

    // Global business metrics (from BusinessMetrics.cs - already scraped by Prometheus)
    private static readonly string[] GLOBAL_BUSINESS_METRICS = new[]
    {
        "business_records_processed_total",
        "business_invalid_records_total",
        "business_records_skipped_total",
        "business_output_records_total",
        "business_dead_letter_records_total",
        "business_files_processed_total",
        "business_files_pending",
        "business_file_size_bytes",
        "business_bytes_processed_total",
        "business_output_bytes_total",
        "business_active_jobs",
        "business_jobs_completed_total",
        "business_jobs_failed_total",
        "business_batches_processed_total",
        "business_processing_duration_seconds",
        "business_end_to_end_latency_seconds",
        "business_queue_wait_time_seconds",
        "business_validation_latency_seconds",
        "business_validation_error_rate",
        "business_retry_attempts_total"
    };

    // System/infrastructure metrics (standard Prometheus metrics)
    private static readonly string[] SYSTEM_METRICS = new[]
    {
        "process_cpu_seconds_total",
        "process_cpu_usage",
        "process_resident_memory_bytes",
        "process_virtual_memory_bytes",
        "dotnet_gc_heap_size_bytes",
        "dotnet_gc_collections_total",
        "http_requests_total",
        "http_request_duration_seconds",
        "http_requests_in_progress",
        "up"
    };

    public AlertGenerator(Random random)
    {
        _random = random;
    }

    public async Task GenerateAsync()
    {
        Console.WriteLine("[6/7] 🚨 Generating alerts for metrics...");

        var metrics = await DB.Find<MetricConfiguration>().ExecuteAsync();
        int simpleAlertCount = 0;
        int complexAlertCount = 0;

        // Add simple alerts to ~30% of datasource-specific metrics
        var metricsWithSimpleAlerts = metrics.OrderBy(_ => _random.Next()).Take(metrics.Count / 3).ToList();

        foreach (var metric in metricsWithSimpleAlerts)
        {
            var thresholds = new[] { 100, 1000, 5000, 10000, 50000 };
            var threshold = thresholds[_random.Next(thresholds.Length)];

            metric.AlertRules = new List<AlertRule>
            {
                new AlertRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"alert_{metric.Name}",
                    Expression = $"{metric.Name} > {threshold}",
                    Description = $"התראה: {metric.DisplayName} עבר את הסף ({threshold})",
                    Severity = _random.Next(2) == 0 ? "warning" : "critical",
                    For = "5m",
                    Annotations = new Dictionary<string, string>
                    {
                        ["summary"] = $"התראה: {metric.DisplayName}",
                        ["description"] = $"הערך עבר את {threshold}"
                    },
                    Labels = new Dictionary<string, string>
                    {
                        ["alert_type"] = "simple",
                        ["datasource_id"] = metric.DataSourceId ?? ""
                    },
                    IsEnabled = true
                }
            };

            await metric.SaveAsync();
            simpleAlertCount++;
        }

        // Generate complex multi-metric alerts for some metrics
        var metricsForComplexAlerts = metrics
            .Where(m => !metricsWithSimpleAlerts.Contains(m))
            .OrderBy(_ => _random.Next())
            .Take(Math.Min(10, metrics.Count / 4))
            .ToList();

        foreach (var metric in metricsForComplexAlerts)
        {
            var complexAlert = GenerateComplexAlert(metric, complexAlertCount);

            metric.AlertRules = new List<AlertRule> { complexAlert };
            await metric.SaveAsync();
            complexAlertCount++;
        }

        Console.WriteLine($"  ✅ Generated {simpleAlertCount} simple alerts");
        Console.WriteLine($"  ✅ Generated {complexAlertCount} complex multi-metric alerts\n");
    }

    private AlertRule GenerateComplexAlert(MetricConfiguration metric, int index)
    {
        // Select a complex alert pattern based on index
        var pattern = index % 10;

        return pattern switch
        {
            0 => GenerateProcessingRateAndCpuAlert(metric),
            1 => GenerateLatencyAndMemoryAlert(metric),
            2 => GenerateErrorRateAndJobsAlert(metric),
            3 => GenerateFilesAndRecordsAlert(metric),
            4 => GenerateQueueAndLatencyAlert(metric),
            5 => GenerateHttpAndBusinessAlert(metric),
            6 => GenerateRetryAndDeadLetterAlert(metric),
            7 => GenerateValidationAndProcessingAlert(metric),
            8 => GenerateJobsAndSystemAlert(metric),
            _ => GenerateBytesAndFilesAlert(metric)
        };
    }

    /// <summary>
    /// Complex alert: Low processing rate OR high CPU usage
    /// </summary>
    private AlertRule GenerateProcessingRateAndCpuAlert(MetricConfiguration metric)
    {
        var businessMetric = "business_records_processed_total";
        var systemMetric = "process_cpu_seconds_total";

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_processing_cpu_{metric.Name}",
            Expression = $"rate({businessMetric}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) < 10 OR rate({systemMetric}[1m]) > 0.8",
            Description = "התראה מורכבת: קצב עיבוד נמוך או שימוש גבוה ב-CPU",
            Severity = "critical",
            For = "5m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "קצב עיבוד נמוך או עומס CPU גבוה",
                ["description"] = $"קצב העיבוד של {metric.DisplayName} ירד מתחת ל-10 לשנייה או שימוש ה-CPU עלה מעל 80%",
                ["runbook_url"] = "https://wiki.example.com/runbooks/processing-cpu-alert"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = businessMetric,
                ["system_metrics"] = systemMetric
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = businessMetric,
                ["systemMetricIds"] = systemMetric
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: High latency AND high memory usage
    /// </summary>
    private AlertRule GenerateLatencyAndMemoryAlert(MetricConfiguration metric)
    {
        var businessMetric = "business_processing_duration_seconds";
        var systemMetric = "process_resident_memory_bytes";

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_latency_memory_{metric.Name}",
            Expression = $"histogram_quantile(0.95, rate({businessMetric}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 5 AND {systemMetric} > 1073741824",
            Description = "התראה מורכבת: חביון גבוה וזיכרון גבוה",
            Severity = "warning",
            For = "10m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "חביון גבוה בעיבוד עם שימוש זיכרון גבוה",
                ["description"] = $"P95 של זמן עיבוד {metric.DisplayName} עולה על 5 שניות ושימוש הזיכרון מעל 1GB"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = businessMetric,
                ["system_metrics"] = systemMetric
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = businessMetric,
                ["systemMetricIds"] = systemMetric
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Error rate spike OR jobs failing
    /// </summary>
    private AlertRule GenerateErrorRateAndJobsAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_invalid_records_total", "business_jobs_failed_total" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_errors_jobs_{metric.Name}",
            Expression = $"rate({businessMetrics[0]}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) > 5 OR increase({businessMetrics[1]}{{datasource_id=\"{metric.DataSourceId}\"}}[15m]) > 3",
            Description = "התראה מורכבת: קצב שגיאות גבוה או עבודות נכשלות",
            Severity = "critical",
            For = "3m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "שגיאות בעיבוד או כשלון עבודות",
                ["description"] = $"יותר מ-5 רשומות לא תקינות לשנייה או יותר מ-3 עבודות שנכשלו ב-15 דקות האחרונות"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Files pending AND low records processed
    /// </summary>
    private AlertRule GenerateFilesAndRecordsAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_files_pending", "business_records_processed_total" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_files_records_{metric.Name}",
            Expression = $"{businessMetrics[0]}{{datasource_id=\"{metric.DataSourceId}\"}} > 100 AND rate({businessMetrics[1]}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) < 5",
            Description = "התראה מורכבת: קבצים ממתינים עם קצב עיבוד נמוך",
            Severity = "warning",
            For = "15m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "צבירת קבצים - קצב עיבוד איטי",
                ["description"] = "יותר מ-100 קבצים ממתינים וקצב העיבוד נמוך מ-5 רשומות לשנייה"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Queue wait time AND end-to-end latency
    /// </summary>
    private AlertRule GenerateQueueAndLatencyAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_queue_wait_time_seconds", "business_end_to_end_latency_seconds" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_queue_latency_{metric.Name}",
            Expression = $"histogram_quantile(0.90, rate({businessMetrics[0]}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 30 AND histogram_quantile(0.90, rate({businessMetrics[1]}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 60",
            Description = "התראה מורכבת: זמן המתנה בתור וחביון כולל גבוהים",
            Severity = "warning",
            For = "10m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "חביון גבוה בתור ובעיבוד כולל",
                ["description"] = "P90 של זמן המתנה בתור מעל 30 שניות וחביון כולל מעל 60 שניות"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: HTTP errors AND business processing slowdown
    /// </summary>
    private AlertRule GenerateHttpAndBusinessAlert(MetricConfiguration metric)
    {
        var businessMetric = "business_processing_duration_seconds";
        var systemMetric = "http_requests_total";

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_http_processing_{metric.Name}",
            Expression = $"rate({systemMetric}{{status=~\"5..\"}}[5m]) > 1 OR histogram_quantile(0.99, rate({businessMetric}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 10",
            Description = "התראה מורכבת: שגיאות HTTP או עיבוד איטי מאוד",
            Severity = "critical",
            For = "5m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "בעיות HTTP או עיבוד איטי",
                ["description"] = "יותר משגיאת HTTP 5xx לשנייה או P99 של זמן עיבוד מעל 10 שניות"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = businessMetric,
                ["system_metrics"] = systemMetric
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = businessMetric,
                ["systemMetricIds"] = systemMetric
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Retry attempts AND dead letter records
    /// </summary>
    private AlertRule GenerateRetryAndDeadLetterAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_retry_attempts_total", "business_dead_letter_records_total" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_retry_deadletter_{metric.Name}",
            Expression = $"increase({businessMetrics[0]}{{datasource_id=\"{metric.DataSourceId}\"}}[10m]) > 50 AND increase({businessMetrics[1]}{{datasource_id=\"{metric.DataSourceId}\"}}[10m]) > 10",
            Description = "התראה מורכבת: ניסיונות חוזרים רבים ורשומות בתור מתים",
            Severity = "critical",
            For = "5m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "כשלונות חוזרים ורשומות אבודות",
                ["description"] = "יותר מ-50 ניסיונות חוזרים ויותר מ-10 רשומות בתור מתים ב-10 דקות"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Validation latency AND processing duration
    /// </summary>
    private AlertRule GenerateValidationAndProcessingAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_validation_latency_seconds", "business_processing_duration_seconds" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_validation_processing_{metric.Name}",
            Expression = $"histogram_quantile(0.95, rate({businessMetrics[0]}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 2 AND histogram_quantile(0.95, rate({businessMetrics[1]}_bucket{{datasource_id=\"{metric.DataSourceId}\"}}[5m])) > 3",
            Description = "התראה מורכבת: ולידציה ועיבוד איטיים",
            Severity = "warning",
            For = "10m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "ולידציה ועיבוד איטיים",
                ["description"] = "P95 של זמן ולידציה מעל 2 שניות ועיבוד מעל 3 שניות"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: Jobs failing AND system not healthy
    /// </summary>
    private AlertRule GenerateJobsAndSystemAlert(MetricConfiguration metric)
    {
        var businessMetric = "business_jobs_failed_total";
        var systemMetric = "up";

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_jobs_system_{metric.Name}",
            Expression = $"increase({businessMetric}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) > 2 AND {systemMetric}{{job=\"dataprocessing\"}} < 1",
            Description = "התראה מורכבת: עבודות נכשלות ושירות לא זמין",
            Severity = "critical",
            For = "2m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "כשלון עבודות ובעיות זמינות",
                ["description"] = "עבודות נכשלות והשירות לא מדווח כפעיל"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = businessMetric,
                ["system_metrics"] = systemMetric
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = businessMetric,
                ["systemMetricIds"] = systemMetric
            },
            IsEnabled = true
        };
    }

    /// <summary>
    /// Complex alert: High bytes processed AND high file count
    /// </summary>
    private AlertRule GenerateBytesAndFilesAlert(MetricConfiguration metric)
    {
        var businessMetrics = new[] { "business_bytes_processed_total", "business_files_processed_total" };

        return new AlertRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"complex_bytes_files_{metric.Name}",
            Expression = $"rate({businessMetrics[0]}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) > 104857600 AND rate({businessMetrics[1]}{{datasource_id=\"{metric.DataSourceId}\"}}[5m]) > 10",
            Description = "התראה מורכבת: עומס גבוה - נפח וקבצים",
            Severity = "info",
            For = "15m",
            Annotations = new Dictionary<string, string>
            {
                ["summary"] = "עומס עיבוד גבוה",
                ["description"] = "יותר מ-100MB לשנייה ויותר מ-10 קבצים לשנייה - עומס גבוה"
            },
            Labels = new Dictionary<string, string>
            {
                ["alert_type"] = "complex",
                ["datasource_id"] = metric.DataSourceId ?? "",
                ["business_metrics"] = string.Join(",", businessMetrics)
            },
            TemplateParameters = new Dictionary<string, string>
            {
                ["businessMetricIds"] = string.Join(",", businessMetrics)
            },
            IsEnabled = true
        };
    }
}

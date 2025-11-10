using DataProcessing.Shared.Entities;
using MetricsConfigurationService.Models;
using MongoDB.Bson;
using MongoDB.Entities;
using DemoDataGenerator.Models;
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
                }
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
    
    public async Task GenerateAsync()
    {
        Console.WriteLine("[4/7] 📈 Generating 20 global metrics...");
        
        var globalMetrics = new[]
        {
            // Simple formulas - just field paths (FormulaType = Simple)
            ("total_records_count", "ספירת רשומות כוללת", "$.recordId", "counter", "$.recordId", FormulaType.Simple),
            ("avg_amount_daily", "ממוצע סכומים יומי", "$.totalAmount", "gauge", "$.totalAmount", FormulaType.Simple),
            ("revenue_total", "הכנסות כוללות", "$.totalAmount", "counter", "$.totalAmount", FormulaType.Simple),
            ("processing_duration", "משך עיבוד", "$.metadata.processingTime", "gauge", "$.metadata.processingTime", FormulaType.Simple),
            ("customer_satisfaction", "שביעות רצון לקוחות", "$.metadata.rating", "gauge", "$.metadata.rating", FormulaType.Simple),
            ("active_users_count", "מספר משתמשים פעילים", "$.customerInfo.customerId", "gauge", "$.customerInfo.customerId", FormulaType.Simple),
            ("unique_customers", "לקוחות ייחודיים", "$.customerInfo.email", "gauge", "$.customerInfo.email", FormulaType.Simple),
            
            // PromQL formulas - advanced calculations (FormulaType = PromQL)
            ("error_rate_5m", "שיעור שגיאות 5 דקות", "rate(errors_total[5m])", "gauge", "$.status", FormulaType.PromQL),
            ("requests_per_second", "בקשות לשנייה", "rate(requests_total[1m])", "counter", "$.recordId", FormulaType.PromQL),
            ("response_time_p95", "זמן תגובה P95", "histogram_quantile(0.95, response_time)", "histogram", "$.timestamp", FormulaType.PromQL),
            ("items_per_transaction", "פריטים לעסקה", "$.items", "gauge", "$.items", FormulaType.Simple),
            ("failed_transactions", "עסקאות כושלות", "$.status", "counter", "$.status", FormulaType.Simple),
            ("high_value_transactions", "עסקאות בעלות ערך גבוה", "$.totalAmount", "counter", "$.totalAmount", FormulaType.Simple),
            ("avg_items_price", "מחיר ממוצע לפריט", "$.items[*].price", "gauge", "$.items[*].price", FormulaType.Simple),
            ("transactions_by_city", "עסקאות לפי עיר", "$.customerInfo.address.city", "counter", "$.customerInfo.address.city", FormulaType.Simple),
            ("completion_rate", "שיעור השלמה", "$.status", "gauge", "$.status", FormulaType.Simple),
            ("system_throughput", "תפוקת מערכת", "increase(total_records[1h])", "counter", "$.recordId", FormulaType.PromQL),
            ("error_spike_detector", "זיהוי עליית שגיאות", "rate(errors[5m]) > rate(errors[1h])", "gauge", "$.status", FormulaType.PromQL),
            ("success_rate", "שיעור הצלחה", "$.status", "gauge", "$.status", FormulaType.Simple),
            ("peak_hour_load", "עומס בשעת שיא", "max_over_time(requests[1h])", "gauge", "$.timestamp", FormulaType.PromQL)
        };
        
        for (int i = 0; i < globalMetrics.Length; i++)
        {
            var (name, displayName, formula, prometheusType, fieldPath, formulaType) = globalMetrics[i];
            
            var metric = new MetricConfiguration
            {
                Name = name,
                DisplayName = displayName,
                Description = $"מדד גלובלי: {displayName}",
                Category = "business",  // Use 'business' instead of Hebrew
                Scope = "global",
                DataSourceId = null,
                Formula = formula,
                FormulaType = formulaType,  // Use the explicit FormulaType from tuple
                FieldPath = fieldPath,
                PrometheusType = prometheusType,
                Status = 1, // Active
                CreatedBy = "DemoGenerator"
            };
            
            await metric.SaveAsync();
            Console.WriteLine($"  ✓ {displayName}");
        }
        
        Console.WriteLine($"  ✅ Generated {globalMetrics.Length} global metrics\n");
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
    
    public AlertGenerator(Random random)
    {
        _random = random;
    }
    
    public async Task GenerateAsync()
    {
        Console.WriteLine("[6/7] 🚨 Generating alerts for metrics...");
        
        var metrics = await DB.Find<MetricConfiguration>().ExecuteAsync();
        int alertCount = 0;
        
        // Add alerts to ~30% of metrics
        var metricsWithAlerts = metrics.OrderBy(_ => _random.Next()).Take(metrics.Count / 3).ToList();
        
        foreach (var metric in metricsWithAlerts)
        {
            var thresholds = new[] { 100, 1000, 5000, 10000, 50000 };
            var threshold = thresholds[_random.Next(thresholds.Length)];
            
            metric.AlertRules = new List<AlertRule>
            {
                new AlertRule
                {
                    Name = $"alert_{metric.Name}",
                    Expression = $"{metric.Name} > {threshold}",
                    Description = $"התראה: {metric.DisplayName} עבר את הסף ({threshold})",
                    Severity = _random.Next(2) == 0 ? "warning" : "critical",
                    Annotations = new Dictionary<string, string>
                    {
                        ["summary"] = $"התראה: {metric.DisplayName}",
                        ["description"] = $"הערך עבר את {threshold}"
                    },
                    IsEnabled = true
                }
            };
            
            await metric.SaveAsync();
            alertCount++;
        }
        
        Console.WriteLine($"  ✅ Generated {alertCount} alerts\n");
    }
}

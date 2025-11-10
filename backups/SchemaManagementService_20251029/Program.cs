using MongoDB.Entities;
using SchemaManagementService.Entities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add MongoDB configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "mongodb://localhost:27017/?authSource=admin";

// Register application services
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<ISchemaRepository, SchemaRepository>();
builder.Services.AddScoped<ISchemaValidationService, SchemaValidationService>();

var app = builder.Build();

// Initialize MongoDB.Entities after app is built
try
{
    // MongoDB.Entities uses default localhost:27017 if no connection string provided
    await DB.InitAsync("dataprocessing_schemas");
    Log.Information("MongoDB initialized successfully with default connection");
    
    // Seed comprehensive test data
    await SeedTestDataAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to initialize MongoDB: {ErrorMessage}", ex.Message);
    throw;
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "SchemaManagementService", Timestamp = DateTime.UtcNow }));

try
{
    Log.Information("Starting SchemaManagementService on port 5050");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SchemaManagementService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Seed comprehensive test data method
static async Task SeedTestDataAsync()
{
    var existingSchemas = await DB.Find<DataProcessingSchema>().ExecuteAsync();
    if (existingSchemas.Any())
    {
        Log.Information("Test schemas already exist, skipping seed data");
        return;
    }

    Log.Information("Seeding comprehensive test schema data...");

    var testSchemas = new List<DataProcessingSchema>
    {
        // Schema 1: Simple User Profile (8 fields)
        new DataProcessingSchema
        {
            Name = "user_profile_simple",
            DisplayName = "פרופיל משתמש פשוט",
            Description = "סכמה פשוטה לפרופיל משתמש בסיסי",
            DataSourceId = "ds001",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "User Profile Simple",
              "properties": {
                "userId": { "type": "string", "description": "מזהה משתמש" },
                "firstName": { "type": "string", "description": "שם פרטי" },
                "lastName": { "type": "string", "description": "שם משפחה" },
                "email": { "type": "string", "format": "email", "description": "כתובת אימייל" },
                "phone": { "type": "string", "pattern": "^05[0-9]-[0-9]{7}$", "description": "מספר טלפון" },
                "birthDate": { "type": "string", "format": "date", "description": "תאריך לידה" },
                "gender": { "type": "string", "enum": ["male", "female", "other"], "description": "מגדר" },
                "isActive": { "type": "boolean", "description": "פעיל?" }
              },
              "required": ["userId", "firstName", "lastName", "email"]
            }
            """,
            Tags = new List<string> { "user", "profile", "basic" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        },

        // Schema 2: Sales Transaction Complex (12 fields)
        new DataProcessingSchema
        {
            Name = "sales_transaction_complex",
            DisplayName = "עסקת מכירות מורכבת",
            Description = "סכמה מורכבת לעסקות מכירות עם פרטי לקוח",
            DataSourceId = "ds002",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "Sales Transaction Complex",
              "properties": {
                "transactionId": { "type": "string", "description": "מזהה עסקה" },
                "customerId": { "type": "string", "description": "מזהה לקוח" },
                "customer": {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "שם לקוח" },
                    "email": { "type": "string", "format": "email" },
                    "phone": { "type": "string", "pattern": "^05[0-9]-[0-9]{7}$" }
                  }
                },
                "products": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "productId": { "type": "string" },
                      "name": { "type": "string" },
                      "price": { "type": "number" },
                      "quantity": { "type": "integer" }
                    }
                  }
                },
                "totalAmount": { "type": "number", "minimum": 0, "description": "סכום כולל" },
                "vatAmount": { "type": "number", "minimum": 0, "description": "מע״מ" },
                "discountAmount": { "type": "number", "minimum": 0, "description": "הנחה" },
                "paymentMethod": { "type": "string", "enum": ["cash", "credit", "debit", "transfer"], "description": "אמצעי תשלום" },
                "transactionDate": { "type": "string", "format": "date-time", "description": "תאריך עסקה" },
                "branchId": { "type": "string", "description": "מזהה סניף" },
                "salesPersonId": { "type": "string", "description": "מזהה מוכר" },
                "status": { "type": "string", "enum": ["pending", "completed", "cancelled"], "description": "סטטוס" }
              },
              "required": ["transactionId", "customerId", "totalAmount", "transactionDate"]
            }
            """,
            Tags = new List<string> { "sales", "transaction", "complex", "nested" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        },

        // Schema 3: Product Basic (5 fields)
        new DataProcessingSchema
        {
            Name = "product_basic",
            DisplayName = "מוצר בסיסי",
            Description = "סכמה בסיסית למוצר",
            DataSourceId = "ds003",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "Product Basic",
              "properties": {
                "productId": { "type": "string", "description": "מזהה מוצר" },
                "name": { "type": "string", "description": "שם מוצר" },
                "price": { "type": "number", "minimum": 0, "description": "מחיר" },
                "category": { "type": "string", "description": "קטגוריה" },
                "inStock": { "type": "boolean", "description": "במלאי?" }
              },
              "required": ["productId", "name", "price"]
            }
            """,
            Tags = new List<string> { "product", "basic", "inventory" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        },

        // Schema 4: Employee Record Comprehensive (15 fields)
        new DataProcessingSchema
        {
            Name = "employee_record_comprehensive",
            DisplayName = "רשומת עובד מקיפה",
            Description = "סכמה מקיפה לרשומת עובד עם כל הפרטים",
            DataSourceId = "ds004",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "Employee Record Comprehensive",
              "properties": {
                "employeeId": { "type": "string", "description": "מזהה עובד" },
                "personalInfo": {
                  "type": "object",
                  "properties": {
                    "firstName": { "type": "string", "description": "שם פרטי" },
                    "lastName": { "type": "string", "description": "שם משפחה" },
                    "idNumber": { "type": "string", "pattern": "^[0-9]{9}$", "description": "מספר זהות" },
                    "birthDate": { "type": "string", "format": "date", "description": "תאריך לידה" },
                    "gender": { "type": "string", "enum": ["male", "female", "other"] }
                  }
                },
                "contactInfo": {
                  "type": "object",
                  "properties": {
                    "email": { "type": "string", "format": "email" },
                    "phone": { "type": "string", "pattern": "^05[0-9]-[0-9]{7}$" },
                    "address": { "type": "string" },
                    "city": { "type": "string" },
                    "zipCode": { "type": "string", "pattern": "^[0-9]{5,7}$" }
                  }
                },
                "jobInfo": {
                  "type": "object",
                  "properties": {
                    "position": { "type": "string", "description": "תפקיד" },
                    "department": { "type": "string", "description": "מחלקה" },
                    "managerId": { "type": "string", "description": "מזהה מנהל" },
                    "startDate": { "type": "string", "format": "date", "description": "תאריך תחילת עבודה" },
                    "salary": { "type": "number", "minimum": 0, "description": "שכר" }
                  }
                },
                "status": { "type": "string", "enum": ["active", "inactive", "terminated"], "description": "סטטוס" },
                "lastUpdated": { "type": "string", "format": "date-time", "description": "עודכן לאחרונה" }
              },
              "required": ["employeeId", "personalInfo", "contactInfo", "jobInfo"]
            }
            """,
            Tags = new List<string> { "employee", "hr", "comprehensive", "personal" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        },

        // Schema 5: Financial Report Extended (20 fields)
        new DataProcessingSchema
        {
            Name = "financial_report_extended",
            DisplayName = "דו״ח כספי מורחב",
            Description = "סכמה מורחבת לדו״ח כספי עם פירוט מלא",
            DataSourceId = "ds005",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "Financial Report Extended",
              "properties": {
                "reportId": { "type": "string", "description": "מזהה דו״ח" },
                "reportDate": { "type": "string", "format": "date", "description": "תאריך דו״ח" },
                "reportPeriod": {
                  "type": "object",
                  "properties": {
                    "startDate": { "type": "string", "format": "date" },
                    "endDate": { "type": "string", "format": "date" }
                  }
                },
                "revenue": {
                  "type": "object",
                  "properties": {
                    "grossRevenue": { "type": "number", "description": "הכנסות ברוטו" },
                    "netRevenue": { "type": "number", "description": "הכנסות נטו" },
                    "salesRevenue": { "type": "number", "description": "הכנסות מכירות" },
                    "serviceRevenue": { "type": "number", "description": "הכנסות שירותים" },
                    "otherRevenue": { "type": "number", "description": "הכנסות אחרות" }
                  }
                },
                "expenses": {
                  "type": "object",
                  "properties": {
                    "totalExpenses": { "type": "number", "description": "הוצאות כוללות" },
                    "operatingExpenses": { "type": "number", "description": "הוצאות תפעוליות" },
                    "marketingExpenses": { "type": "number", "description": "הוצאות שיווק" },
                    "salaryExpenses": { "type": "number", "description": "הוצאות שכר" },
                    "rentExpenses": { "type": "number", "description": "הוצאות שכירות" },
                    "utilityExpenses": { "type": "number", "description": "הוצאות שירותים" },
                    "otherExpenses": { "type": "number", "description": "הוצאות אחרות" }
                  }
                },
                "profitLoss": {
                  "type": "object",
                  "properties": {
                    "grossProfit": { "type": "number", "description": "רווח ברוטו" },
                    "netProfit": { "type": "number", "description": "רווח נטו" },
                    "profitMargin": { "type": "number", "description": "שולי רווח" }
                  }
                },
                "taxes": {
                  "type": "object",
                  "properties": {
                    "incomeTax": { "type": "number", "description": "מס הכנסה" },
                    "vatPayable": { "type": "number", "description": "מע״מ לתשלום" },
                    "vatReceivable": { "type": "number", "description": "מע״מ לקבלה" }
                  }
                },
                "companyId": { "type": "string", "description": "מזהה חברה" },
                "preparedBy": { "type": "string", "description": "הוכן על ידי" },
                "approvedBy": { "type": "string", "description": "אושר על ידי" },
                "status": { "type": "string", "enum": ["draft", "pending", "approved", "published"] }
              },
              "required": ["reportId", "reportDate", "reportPeriod", "revenue", "expenses", "companyId"]
            }
            """,
            Tags = new List<string> { "financial", "report", "extended", "accounting" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        },

        // Schema 6: Customer Survey Advanced (10 fields)
        new DataProcessingSchema
        {
            Name = "customer_survey_advanced",
            DisplayName = "סקר לקוחות מתקדם",
            Description = "סכמה מתקדמת לסקר לקוחות עם שדות מורכבים",
            DataSourceId = "ds006",
            JsonSchemaContent = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "title": "Customer Survey Advanced",
              "properties": {
                "surveyId": { "type": "string", "description": "מזהה סקר" },
                "customerId": { "type": "string", "description": "מזהה לקוח" },
                "surveyDate": { "type": "string", "format": "date-time", "description": "תאריך סקר" },
                "ratings": {
                  "type": "object",
                  "properties": {
                    "overallSatisfaction": { "type": "integer", "minimum": 1, "maximum": 5, "description": "שביעות רצון כללית" },
                    "productQuality": { "type": "integer", "minimum": 1, "maximum": 5, "description": "איכות מוצר" },
                    "customerService": { "type": "integer", "minimum": 1, "maximum": 5, "description": "שירות לקוחות" },
                    "valueForMoney": { "type": "integer", "minimum": 1, "maximum": 5, "description": "יחס מחיר-איכות" }
                  }
                },
                "feedback": {
                  "type": "object",
                  "properties": {
                    "positiveComments": { "type": "string", "description": "הערות חיוביות" },
                    "improvements": { "type": "string", "description": "הצעות לשיפור" },
                    "additionalComments": { "type": "string", "description": "הערות נוספות" }
                  }
                },
                "demographics": {
                  "type": "object",
                  "properties": {
                    "ageGroup": { "type": "string", "enum": ["18-25", "26-35", "36-45", "46-55", "56-65", "65+"] },
                    "gender": { "type": "string", "enum": ["male", "female", "other", "prefer-not-to-say"] }
                  }
                },
                "recommendationScore": { "type": "integer", "minimum": 0, "maximum": 10, "description": "ציון המלצה NPS" },
                "followUpRequired": { "type": "boolean", "description": "נדרש מעקב?" },
                "surveyVersion": { "type": "string", "description": "גרסת סקר" },
                "completionTime": { "type": "integer", "minimum": 0, "description": "זמן מילוי בדקות" }
              },
              "required": ["surveyId", "customerId", "surveyDate", "ratings"]
            }
            """,
            Tags = new List<string> { "survey", "customer", "feedback", "advanced" },
            Status = SchemaStatus.Active,
            CreatedBy = "System",
            CorrelationId = Guid.NewGuid().ToString()
        }
    };

    // Save all schemas
    foreach (var schema in testSchemas)
    {
        await schema.SaveAsync();
        Log.Information("Created schema: {SchemaName} with {FieldCount} fields", schema.DisplayName, CountJsonFields(schema.JsonSchemaContent));
    }

    Log.Information("Successfully seeded {SchemaCount} comprehensive test schemas", testSchemas.Count);
}

static int CountJsonFields(string jsonSchema)
{
    try
    {
        // Simple field count estimation
        return jsonSchema.Split("\"type\"").Length - 1;
    }
    catch
    {
        return 0;
    }
}

// Service interfaces (will be moved to separate files)
public interface ISchemaService
{
    Task<List<DataProcessingSchema>> GetSchemasAsync();
    Task<DataProcessingSchema?> GetSchemaByIdAsync(string id);
    Task<DataProcessingSchema> CreateSchemaAsync(CreateSchemaRequest request);
    Task<DataProcessingSchema> UpdateSchemaAsync(string id, UpdateSchemaRequest request);
    Task DeleteSchemaAsync(string id);
}

public interface ISchemaRepository
{
    Task<List<DataProcessingSchema>> GetAllAsync();
    Task<DataProcessingSchema?> GetByIdAsync(string id);
    Task<DataProcessingSchema> CreateAsync(DataProcessingSchema schema);
    Task<DataProcessingSchema> UpdateAsync(DataProcessingSchema schema);
    Task DeleteAsync(string id);
}

public interface ISchemaValidationService
{
    Task<ValidationResult> ValidateJsonSchemaAsync(string jsonSchemaContent);
    Task<DataValidationResult> ValidateDataAgainstSchemaAsync(string schemaId, object data);
}

// Request/Response models (will be moved to separate files)
public class CreateSchemaRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DataSourceId { get; set; }
    public string JsonSchemaContent { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public class UpdateSchemaRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DataSourceId { get; set; }
    public string JsonSchemaContent { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public SchemaStatus Status { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DataValidationResult
{
    public bool IsValid { get; set; }
    public List<FieldValidationError> Errors { get; set; } = new();
}

public class FieldValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageHebrew { get; set; } = string.Empty;
    public object? ExpectedValue { get; set; }
    public object? ActualValue { get; set; }
}

// Basic service implementations (will be moved to separate files)
public class SchemaService : ISchemaService
{
    private readonly ISchemaRepository _repository;

    public SchemaService(ISchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DataProcessingSchema>> GetSchemasAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DataProcessingSchema?> GetSchemaByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DataProcessingSchema> CreateSchemaAsync(CreateSchemaRequest request)
    {
        var schema = new DataProcessingSchema
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            DataSourceId = request.DataSourceId,
            JsonSchemaContent = request.JsonSchemaContent,
            Tags = request.Tags,
            Status = SchemaStatus.Draft,
            CreatedBy = "User", // TODO: Get from authentication context
            CorrelationId = Guid.NewGuid().ToString()
        };

        return await _repository.CreateAsync(schema);
    }

    public async Task<DataProcessingSchema> UpdateSchemaAsync(string id, UpdateSchemaRequest request)
    {
        var schema = await _repository.GetByIdAsync(id);
        if (schema == null)
            throw new ArgumentException($"Schema with ID {id} not found");

        schema.DisplayName = request.DisplayName;
        schema.Description = request.Description;
        schema.DataSourceId = request.DataSourceId;
        schema.JsonSchemaContent = request.JsonSchemaContent;
        schema.Tags = request.Tags;
        schema.Status = request.Status;
        schema.MarkAsModified("User"); // TODO: Get from authentication context

        return await _repository.UpdateAsync(schema);
    }

    public async Task DeleteSchemaAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }
}

public class SchemaRepository : ISchemaRepository
{
    public async Task<List<DataProcessingSchema>> GetAllAsync()
    {
        return await DB.Find<DataProcessingSchema>()
            .Match(s => !s.IsDeleted)
            .ExecuteAsync();
    }

    public async Task<DataProcessingSchema?> GetByIdAsync(string id)
    {
        return await DB.Find<DataProcessingSchema>()
            .Match(s => s.ID == id && !s.IsDeleted)
            .ExecuteFirstAsync();
    }

    public async Task<DataProcessingSchema> CreateAsync(DataProcessingSchema schema)
    {
        await schema.SaveAsync();
        return schema;
    }

    public async Task<DataProcessingSchema> UpdateAsync(DataProcessingSchema schema)
    {
        await schema.SaveAsync();
        return schema;
    }

    public async Task DeleteAsync(string id)
    {
        await DB.Update<DataProcessingSchema>()
            .Match(s => s.ID == id)
            .Modify(s => s.IsDeleted, true)
            .Modify(s => s.UpdatedAt, DateTime.UtcNow)
            .ExecuteAsync();
    }
}

public class SchemaValidationService : ISchemaValidationService
{
    public async Task<ValidationResult> ValidateJsonSchemaAsync(string jsonSchemaContent)
    {
        // TODO: Implement with Newtonsoft.Json.Schema
        return await Task.FromResult(new ValidationResult { IsValid = true });
    }

    public async Task<DataValidationResult> ValidateDataAgainstSchemaAsync(string schemaId, object data)
    {
        // TODO: Implement schema validation against data
        return await Task.FromResult(new DataValidationResult { IsValid = true });
    }
}

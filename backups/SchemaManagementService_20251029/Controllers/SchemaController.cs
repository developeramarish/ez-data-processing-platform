using Microsoft.AspNetCore.Mvc;
using SchemaManagementService.Entities;
using SchemaManagementService.Models.Requests;
using SchemaManagementService.Models.Responses;
using SchemaManagementService.Services;

namespace SchemaManagementService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SchemaController : ControllerBase
{
    private readonly ISchemaService _schemaService;
    private readonly ISchemaValidationService _validationService;
    private readonly ILogger<SchemaController> _logger;

    public SchemaController(
        ISchemaService schemaService,
        ISchemaValidationService validationService,
        ILogger<SchemaController> logger)
    {
        _schemaService = schemaService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all schemas with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSchemas(
        [FromQuery] int page = 1,
        [FromQuery] int size = 25,
        [FromQuery] string? search = null,
        [FromQuery] SchemaStatus? status = null,
        [FromQuery] string? dataSourceId = null)
    {
        try
        {
            var schemas = await _schemaService.GetSchemasAsync();
            
            // Apply filters (TODO: move to service layer)
            if (!string.IsNullOrEmpty(search))
            {
                schemas = schemas.Where(s => 
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (status.HasValue)
            {
                schemas = schemas.Where(s => s.Status == status.Value).ToList();
            }

            if (!string.IsNullOrEmpty(dataSourceId))
            {
                schemas = schemas.Where(s => s.DataSourceId == dataSourceId).ToList();
            }

            // Apply pagination
            var total = schemas.Count;
            var pagedSchemas = schemas
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return Ok(new
            {
                IsSuccess = true,
                Data = pagedSchemas,
                Total = total,
                Page = page,
                Size = size,
                TotalPages = (int)Math.Ceiling(total / (double)size)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schemas");
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בטעינת Schemas", MessageEnglish = "Error loading schemas" }
            });
        }
    }

    /// <summary>
    /// Get schema by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSchema(string id)
    {
        try
        {
            var schema = await _schemaService.GetSchemaByIdAsync(id);
            if (schema == null)
            {
                return NotFound(new { 
                    IsSuccess = false, 
                    Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
                });
            }

            return Ok(new { IsSuccess = true, Data = schema });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בטעינת Schema", MessageEnglish = "Error loading schema" }
            });
        }
    }

    /// <summary>
    /// Create new schema
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSchema([FromBody] CreateSchemaRequest request)
    {
        try
        {
            // Validate JSON Schema content
            var validationResult = await _validationService.ValidateJsonSchemaAsync(request.JsonSchemaContent);
            if (!validationResult.IsValid)
            {
                return BadRequest(new {
                    IsSuccess = false,
                    Error = new { 
                        Message = "JSON Schema לא תקין", 
                        MessageEnglish = "Invalid JSON Schema",
                        ValidationErrors = validationResult.Errors
                    }
                });
            }

            var schema = await _schemaService.CreateSchemaAsync(request);
            
            _logger.LogInformation("Schema created successfully: {SchemaName}", request.Name);
            
            return CreatedAtAction(
                nameof(GetSchema), 
                new { id = schema.ID }, 
                new { IsSuccess = true, Data = schema });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schema {SchemaName}", request.Name);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה ביצירת Schema", MessageEnglish = "Error creating schema" }
            });
        }
    }

    /// <summary>
    /// Update existing schema
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchema(string id, [FromBody] UpdateSchemaRequest request)
    {
        try
        {
            // Validate JSON Schema content
            var validationResult = await _validationService.ValidateJsonSchemaAsync(request.JsonSchemaContent);
            if (!validationResult.IsValid)
            {
                return BadRequest(new {
                    IsSuccess = false,
                    Error = new { 
                        Message = "JSON Schema לא תקין", 
                        MessageEnglish = "Invalid JSON Schema",
                        ValidationErrors = validationResult.Errors
                    }
                });
            }

            var schema = await _schemaService.UpdateSchemaAsync(id, request);
            
            _logger.LogInformation("Schema updated successfully: {SchemaId}", id);
            
            return Ok(new { IsSuccess = true, Data = schema });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Schema not found: {SchemaId}", id);
            return NotFound(new { 
                IsSuccess = false, 
                Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בעדכון Schema", MessageEnglish = "Error updating schema" }
            });
        }
    }

    /// <summary>
    /// Delete schema (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchema(string id, [FromQuery] string deletedBy = "User")
    {
        try
        {
            var schema = await _schemaService.GetSchemaByIdAsync(id);
            if (schema == null)
            {
                return NotFound(new { 
                    IsSuccess = false, 
                    Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
                });
            }

            // Check if schema is in use
            if (schema.UsageCount > 0)
            {
                return BadRequest(new {
                    IsSuccess = false,
                    Error = new { 
                        Message = "לא ניתן למחוק Schema בשימוש", 
                        MessageEnglish = "Cannot delete schema in use",
                        UsageCount = schema.UsageCount
                    }
                });
            }

            await _schemaService.DeleteSchemaAsync(id);
            
            _logger.LogInformation("Schema deleted successfully: {SchemaId}", id);
            
            return Ok(new { IsSuccess = true, Message = "Schema נמחק בהצלחה" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה במחיקת Schema", MessageEnglish = "Error deleting schema" }
            });
        }
    }

    /// <summary>
    /// Validate sample data against schema
    /// </summary>
    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateSampleData(string id, [FromBody] object data)
    {
        try
        {
            var result = await _validationService.ValidateDataAgainstSchemaAsync(id, data);
            
            return Ok(new { IsSuccess = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data against schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה באימות נתונים", MessageEnglish = "Error validating data" }
            });
        }
    }

    /// <summary>
    /// Publish draft schema to active status
    /// </summary>
    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishSchema(string id)
    {
        try
        {
            var schema = await _schemaService.GetSchemaByIdAsync(id);
            if (schema == null)
            {
                return NotFound(new { 
                    IsSuccess = false, 
                    Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
                });
            }

            if (schema.Status != SchemaStatus.Draft)
            {
                return BadRequest(new {
                    IsSuccess = false,
                    Error = new { 
                        Message = "ניתן לפרסם רק Schema בסטטוס טיוטה", 
                        MessageEnglish = "Can only publish draft schemas"
                    }
                });
            }

            var updateRequest = new UpdateSchemaRequest
            {
                DisplayName = schema.DisplayName,
                Description = schema.Description,
                DataSourceId = schema.DataSourceId,
                JsonSchemaContent = schema.JsonSchemaContent,
                Tags = schema.Tags,
                Status = SchemaStatus.Active
            };

            var updatedSchema = await _schemaService.UpdateSchemaAsync(id, updateRequest);
            updatedSchema.PublishedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Schema published successfully: {SchemaId}", id);
            
            return Ok(new { IsSuccess = true, Data = updatedSchema });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בפרסום Schema", MessageEnglish = "Error publishing schema" }
            });
        }
    }

    /// <summary>
    /// Duplicate existing schema
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateSchema(string id, [FromBody] DuplicateSchemaRequest request)
    {
        try
        {
            var originalSchema = await _schemaService.GetSchemaByIdAsync(id);
            if (originalSchema == null)
            {
                return NotFound(new { 
                    IsSuccess = false, 
                    Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
                });
            }

            // TODO: Check if new name already exists (implement in service layer)
            var tags = request.CopyTags ? originalSchema.Tags.ToList() : new List<string>();
            tags.AddRange(request.AdditionalTags);

            var createRequest = new CreateSchemaRequest
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = $"העתק של {originalSchema.DisplayName} - {request.Description}",
                DataSourceId = null, // New duplicate should not be assigned to any data source
                JsonSchemaContent = originalSchema.JsonSchemaContent,
                Tags = tags
            };

            var duplicatedSchema = await _schemaService.CreateSchemaAsync(createRequest);
            
            _logger.LogInformation("Schema duplicated successfully: {OriginalId} -> {NewId}", id, duplicatedSchema.ID);
            
            return CreatedAtAction(
                nameof(GetSchema), 
                new { id = duplicatedSchema.ID }, 
                new { IsSuccess = true, Data = duplicatedSchema });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בשכפול Schema", MessageEnglish = "Error duplicating schema" }
            });
        }
    }

    /// <summary>
    /// Get usage statistics for schema
    /// </summary>
    [HttpGet("{id}/usage")]
    public async Task<IActionResult> GetSchemaUsageStatistics(string id)
    {
        try
        {
            var schema = await _schemaService.GetSchemaByIdAsync(id);
            if (schema == null)
            {
                return NotFound(new { 
                    IsSuccess = false, 
                    Error = new { Message = "Schema לא נמצא", MessageEnglish = "Schema not found" }
                });
            }

            // TODO: Implement actual statistics collection from metrics/database
            // For now, return mock data structure
            var statistics = new SchemaUsageStatistics
            {
                SchemaId = schema.ID,
                SchemaName = schema.Name,
                DisplayName = schema.DisplayName,
                CurrentUsageCount = schema.UsageCount,
                CreatedAt = schema.CreatedAt,
                CollectedAt = DateTime.UtcNow,
                // Mock data - replace with real metrics collection
                TotalRecordsProcessed = schema.UsageCount * 100,
                TotalValidationAttempts = schema.UsageCount * 120,
                SuccessfulValidations = schema.UsageCount * 110,
                FailedValidations = schema.UsageCount * 10,
                SuccessRate = 91.7,
                AverageValidationTimeMs = 45.2
            };

            return Ok(new { IsSuccess = true, Data = statistics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics for schema {SchemaId}", id);
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בטעינת סטטיסטיקות שימוש", MessageEnglish = "Error loading usage statistics" }
            });
        }
    }

    /// <summary>
    /// Validate JSON Schema syntax
    /// </summary>
    [HttpPost("validate-json")]
    public async Task<IActionResult> ValidateJsonSchema([FromBody] ValidateJsonSchemaRequest request)
    {
        try
        {
            var result = await _validationService.ValidateJsonSchemaAsync(request.JsonSchemaContent);
            
            return Ok(new { IsSuccess = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JSON Schema syntax");
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה באימות JSON Schema", MessageEnglish = "Error validating JSON Schema" }
            });
        }
    }

    /// <summary>
    /// Get available schema templates
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetSchemaTemplates()
    {
        try
        {
            // TODO: Replace with actual template service
            var templates = GetBuiltInTemplates();
            
            return Ok(new { IsSuccess = true, Data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema templates");
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בטעינת תבניות Schema", MessageEnglish = "Error loading schema templates" }
            });
        }
    }

    /// <summary>
    /// Test regex pattern against strings
    /// </summary>
    [HttpPost("regex/test")]
    public IActionResult TestRegexPattern([FromBody] TestRegexRequest request)
    {
        try
        {
            // TODO: Implement when service layer is ready
            var result = new RegexTestResult
            {
                IsValidPattern = true,
                Pattern = request.Pattern,
                TestResults = request.TestStrings.Select(s => new RegexStringTestResult
                {
                    TestString = s,
                    IsMatch = System.Text.RegularExpressions.Regex.IsMatch(s, request.Pattern)
                }).ToList(),
                Summary = new RegexTestSummary
                {
                    TotalTests = request.TestStrings.Count,
                    MatchCount = request.TestStrings.Count(s => System.Text.RegularExpressions.Regex.IsMatch(s, request.Pattern))
                }
            };
            result.Summary.NoMatchCount = result.Summary.TotalTests - result.Summary.MatchCount;
            result.Summary.SuccessRate = result.Summary.TotalTests > 0 ? (double)result.Summary.MatchCount / result.Summary.TotalTests * 100 : 0;
            
            return Ok(new { IsSuccess = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing regex pattern");
            return StatusCode(500, new { 
                IsSuccess = false, 
                Error = new { Message = "שגיאה בבדיקת תבנית Regex", MessageEnglish = "Error testing regex pattern" }
            });
        }
    }

    /// <summary>
    /// Get built-in schema templates
    /// </summary>
    private List<SchemaTemplate> GetBuiltInTemplates()
    {
        return new List<SchemaTemplate>
        {
            new SchemaTemplate
            {
                Id = "israeli_customer",
                Name = "Israeli Customer",
                NameHebrew = "לקוח ישראלי",
                Description = "Schema for Israeli customer data with ID, phone, address",
                DescriptionHebrew = "Schema לנתוני לקוח ישראלי עם ת.ז., טלפון וכתובת",
                Category = "israeli",
                FieldsCount = 8,
                Complexity = "medium",
                JsonSchema = new
                {
                    type = "object",
                    title = "Israeli Customer Schema",
                    properties = new
                    {
                        customerId = new { type = "string", pattern = @"^\d{9}$", description = "Israeli ID number" },
                        firstName = new { type = "string", minLength = 2, maxLength = 50 },
                        lastName = new { type = "string", minLength = 2, maxLength = 50 },
                        email = new { type = "string", format = "email" },
                        phone = new { type = "string", pattern = @"^05[0-9]\d{7}$", description = "Israeli mobile number" },
                        address = new
                        {
                            type = "object",
                            properties = new
                            {
                                street = new { type = "string" },
                                city = new { type = "string" },
                                zipCode = new { type = "string", pattern = @"^\d{5,7}$" }
                            },
                            required = new[] { "street", "city", "zipCode" }
                        },
                        dateOfBirth = new { type = "string", format = "date" },
                        isActive = new { type = "boolean", @default = true }
                    },
                    required = new[] { "customerId", "firstName", "lastName", "email", "phone" }
                },
                Tags = new[] { "customer", "israeli", "identity" }
            },
            new SchemaTemplate
            {
                Id = "financial_transaction",
                Name = "Financial Transaction",
                NameHebrew = "עסקה פיננסית",
                Description = "Schema for financial transactions with amounts, currencies, taxes",
                DescriptionHebrew = "Schema לעסקאות פיננסיות עם סכומים, מטבעות ומסים",
                Category = "israeli",
                FieldsCount = 12,
                Complexity = "complex",
                JsonSchema = new
                {
                    type = "object",
                    title = "Financial Transaction Schema",
                    properties = new
                    {
                        transactionId = new { type = "string", pattern = @"^TXN-\d{10}$" },
                        amount = new { type = "number", minimum = 0.01, maximum = 1000000 },
                        currency = new { type = "string", @enum = new[] { "ILS", "USD", "EUR" }, @default = "ILS" },
                        vatAmount = new { type = "number", minimum = 0, description = "VAT amount in ILS" },
                        vatRate = new { type = "number", minimum = 0, maximum = 0.25, @default = 0.17 },
                        transactionType = new { type = "string", @enum = new[] { "debit", "credit", "transfer" } },
                        description = new { type = "string", maxLength = 200 },
                        merchantId = new { type = "string", pattern = @"^MER-\d{6}$" },
                        timestamp = new { type = "string", format = "date-time" },
                        reference = new { type = "string", maxLength = 50 },
                        status = new { type = "string", @enum = new[] { "pending", "completed", "failed", "cancelled" } },
                        fees = new
                        {
                            type = "object",
                            properties = new
                            {
                                processing = new { type = "number", minimum = 0 },
                                interchange = new { type = "number", minimum = 0 }
                            }
                        }
                    },
                    required = new[] { "transactionId", "amount", "currency", "transactionType", "timestamp", "status" }
                },
                Tags = new[] { "finance", "transaction", "vat", "israeli" }
            },
            new SchemaTemplate
            {
                Id = "product_catalog",
                Name = "Product Catalog",
                NameHebrew = "קטלוג מוצרים",
                Description = "Schema for product catalog with SKU, pricing, inventory",
                DescriptionHebrew = "Schema לקטלוג מוצרים עם מק''ט, תמחור ומלאי",
                Category = "business",
                FieldsCount = 10,
                Complexity = "medium",
                JsonSchema = new
                {
                    type = "object",
                    title = "Product Catalog Schema",
                    properties = new
                    {
                        sku = new { type = "string", pattern = @"^[A-Z]{2,4}-\d{4,8}$" },
                        name = new { type = "string", minLength = 3, maxLength = 100 },
                        nameHebrew = new { type = "string", minLength = 3, maxLength = 100 },
                        description = new { type = "string", maxLength = 500 },
                        price = new { type = "number", minimum = 0 },
                        currency = new { type = "string", @enum = new[] { "ILS", "USD", "EUR" }, @default = "ILS" },
                        category = new { type = "string", minLength = 3, maxLength = 50 },
                        inStock = new { type = "integer", minimum = 0 },
                        weight = new { type = "number", minimum = 0, description = "Weight in grams" },
                        isActive = new { type = "boolean", @default = true }
                    },
                    required = new[] { "sku", "name", "price", "category", "inStock" }
                },
                Tags = new[] { "product", "catalog", "inventory", "ecommerce" }
            },
            new SchemaTemplate
            {
                Id = "user_account",
                Name = "User Account",
                NameHebrew = "חשבון משתמש",
                Description = "Schema for user account with authentication and preferences",
                DescriptionHebrew = "Schema לחשבון משתמש עם אימות והעדפות",
                Category = "technical",
                FieldsCount = 9,
                Complexity = "simple",
                JsonSchema = new
                {
                    type = "object",
                    title = "User Account Schema",
                    properties = new
                    {
                        userId = new { type = "string", format = "uuid" },
                        username = new { type = "string", pattern = @"^[a-zA-Z0-9_]{3,20}$" },
                        email = new { type = "string", format = "email" },
                        firstName = new { type = "string", minLength = 2, maxLength = 50 },
                        lastName = new { type = "string", minLength = 2, maxLength = 50 },
                        role = new { type = "string", @enum = new[] { "admin", "user", "viewer" }, @default = "user" },
                        preferences = new
                        {
                            type = "object",
                            properties = new
                            {
                                language = new { type = "string", @enum = new[] { "he", "en" }, @default = "he" },
                                theme = new { type = "string", @enum = new[] { "light", "dark" }, @default = "light" }
                            }
                        },
                        createdAt = new { type = "string", format = "date-time" },
                        lastLoginAt = new { type = "string", format = "date-time" }
                    },
                    required = new[] { "userId", "username", "email", "firstName", "lastName", "createdAt" }
                },
                Tags = new[] { "user", "account", "authentication" }
            },
            new SchemaTemplate
            {
                Id = "api_response",
                Name = "API Response",
                NameHebrew = "תגובת API",
                Description = "Standardized API response schema",
                DescriptionHebrew = "Schema סטנדרטי לתגובות API",
                Category = "technical",
                FieldsCount = 5,
                Complexity = "simple",
                JsonSchema = new
                {
                    type = "object",
                    title = "API Response Schema",
                    properties = new
                    {
                        success = new { type = "boolean" },
                        data = new { type = "object", description = "Response payload" },
                        error = new
                        {
                            type = "object",
                            properties = new
                            {
                                code = new { type = "string" },
                                message = new { type = "string" },
                                messageHebrew = new { type = "string" }
                            }
                        },
                        timestamp = new { type = "string", format = "date-time" },
                        requestId = new { type = "string", format = "uuid" }
                    },
                    required = new[] { "success", "timestamp" }
                },
                Tags = new[] { "api", "response", "standard" }
            }
        };
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            Service = "SchemaManagementService",
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

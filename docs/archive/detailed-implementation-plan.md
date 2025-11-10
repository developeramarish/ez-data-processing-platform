# Data Processing Platform - Detailed Implementation Plan

## Executive Summary
This document provides a comprehensive implementation plan for the Data Processing Platform, a microservices-based system built with .NET 9.0, supporting Hebrew UI with RTL layout, and featuring automated data processing capabilities.

## Project Architecture Overview

### System Architecture
```
┌─────────────────────────────────────────────────────────────────┐
│                    Data Processing Platform                     │
├─────────────────────────────────────────────────────────────────┤
│  Frontend Layer (Hebrew UI with RTL)                          │
│  ┌─────────────────────┐ ┌─────────────────────┐              │
│  │   React Frontend    │ │   AI Chat Service   │              │
│  │   (Hebrew/RTL)     │ │   (LLM Integration) │              │
│  └─────────────────────┘ └─────────────────────┘              │
├─────────────────────────────────────────────────────────────────┤
│  Service Layer (.NET 9.0 Microservices)                      │
│  ┌──────────────────┐ ┌──────────────────┐ ┌─────────────────┐│
│  │ Data Source      │ │ Scheduling       │ │ Files Receiver  ││
│  │ Management       │ │ Service          │ │ Service         ││
│  │ Service          │ │ (Quartz.NET)     │ │                 ││
│  └──────────────────┘ └──────────────────┘ └─────────────────┘│
│  ┌──────────────────┐ ┌──────────────────┐                    │
│  │ Validation       │ │ Shared Libraries │                    │
│  │ Service          │ │ (Common Utils)   │                    │
│  └──────────────────┘ └──────────────────┘                    │
├─────────────────────────────────────────────────────────────────┤
│  Message Layer (MassTransit + Kafka)                          │
│  Topics: dataprocessing.[service].[event]                     │
├─────────────────────────────────────────────────────────────────┤
│  Data Layer (MongoDB with MongoDB.Entities)                   │
│  Collections: data_sources, validation_results, invalid_records│
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure Layer (Docker + Kubernetes)                   │
│  Monitoring: Prometheus + Grafana + Elasticsearch             │
└─────────────────────────────────────────────────────────────────┘
```

### Technology Stack Details
- **Runtime**: .NET 9.0 with ASP.NET Core Web API
- **Database**: MongoDB with MongoDB.Entities ORM
- **Messaging**: Apache Kafka with MassTransit abstraction
- **Scheduling**: Quartz.NET for job orchestration
- **Frontend**: React with Hebrew/RTL support libraries
- **Monitoring**: Prometheus (metrics), OpenTelemetry (tracing), Elasticsearch (logging)
- **Deployment**: Docker containers with Kubernetes orchestration
- **Package Management**: Helm charts for environment management

## Implementation Phases

### Phase 1: Foundation Infrastructure (Weeks 1-3)

#### Task 1: Project Foundation Structure
**Duration**: 3-4 days  
**Owner**: Senior Backend Developer  
**Dependencies**: None  

**Detailed Steps**:
1. **Directory Structure Creation**:
   ```
   DataProcessingPlatform/
   ├── src/
   │   ├── Services/
   │   │   ├── SchedulingService/
   │   │   ├── FilesReceiverService/
   │   │   ├── ValidationService/
   │   │   ├── DataSourceManagementService/
   │   │   ├── DataSourceChatService/
   │   │   └── Shared/
   │   └── Frontend/
   ├── deploy/
   │   ├── docker/
   │   ├── kubernetes/
   │   └── helm/
   ├── docs/
   ├── tests/
   │   ├── Unit/
   │   ├── Integration/
   │   └── Load/
   └── scripts/
   ```

2. **Solution File Configuration**:
   ```xml
   <Solution>
     <Project Include="src/Services/Shared/DataProcessing.Shared.csproj" />
     <Project Include="src/Services/DataSourceManagementService/DataProcessing.DataSourceManagement.csproj" />
     <!-- Additional projects... -->
   </Solution>
   ```

3. **Global Configuration**:
   - `global.json`: Specify .NET 9.0 SDK version
   - `Directory.Build.props`: Common MSBuild properties
   - `.editorconfig`: Code style standards
   - `nuget.config`: Package source configuration

**Deliverables**:
- Complete directory structure
- Buildable solution file
- Development environment setup scripts
- Basic CI/CD pipeline configuration

#### Task 2: MongoDB Integration & Base Entities
**Duration**: 4-5 days  
**Owner**: Senior Backend Developer  
**Dependencies**: Task 1 completion  

**Detailed Steps**:
1. **MongoDB.Entities Integration**:
   ```csharp
   // Base entity class
   public abstract class DataProcessingBaseEntity : Entity
   {
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
       public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
       public bool IsDeleted { get; set; } = false;
       public string CorrelationId { get; set; } = string.Empty;
   }
   ```

2. **Core Entity Definitions**:
   ```csharp
   // Data source entity
   public class DataProcessingDataSource : DataProcessingBaseEntity
   {
       public string Name { get; set; }
       public string SupplierName { get; set; }
       public string FilePath { get; set; }
       public TimeSpan PollingRate { get; set; }
       public BsonDocument JsonSchema { get; set; }
       public string Category { get; set; }
       public int SchemaVersion { get; set; } = 1;
   }

   // Validation result entity
   public class DataProcessingValidationResult : DataProcessingBaseEntity
   {
       public string DataSourceId { get; set; }
       public string FileName { get; set; }
       public int TotalRecords { get; set; }
       public int ValidRecords { get; set; }
       public int InvalidRecords { get; set; }
       public TimeSpan ProcessingDuration { get; set; }
   }

   // Invalid record entity
   public class DataProcessingInvalidRecord : DataProcessingBaseEntity
   {
       public string DataSourceId { get; set; }
       public string FileName { get; set; }
       public BsonDocument OriginalRecord { get; set; }
       public List<string> ValidationErrors { get; set; }
       public string ErrorType { get; set; }
   }
   ```

3. **Database Configuration**:
   ```csharp
   public static class DatabaseConfiguration
   {
       public static void Initialize(string connectionString, string environment)
       {
           var databaseName = $"data_processing_{environment}";
           DB.InitAsync(databaseName, connectionString);
           
           // Configure indexes
           DB.Index<DataProcessingDataSource>()
             .Key(x => x.Name, KeyType.Ascending)
             .Option(o => o.Unique = true)
             .CreateAsync();
       }
   }
   ```

**Deliverables**:
- MongoDB.Entities integration
- Base entity classes with audit fields
- Core domain entities
- Database initialization and configuration
- Entity relationship mappings
- Soft delete query extensions

#### Task 3: MassTransit & Kafka Infrastructure
**Duration**: 5-6 days  
**Owner**: Senior Backend Developer  
**Dependencies**: Task 1 completion  

**Detailed Steps**:
1. **Message Contracts Definition**:
   ```csharp
   // Base message interface
   public interface IDataProcessingMessage
   {
       string CorrelationId { get; set; }
       DateTime Timestamp { get; set; }
   }

   // File polling event
   public class FilePollingEvent : IDataProcessingMessage
   {
       public string CorrelationId { get; set; }
       public DateTime Timestamp { get; set; }
       public string DataSourceId { get; set; }
       public string FilePath { get; set; }
   }

   // Validation completed event
   public class ValidationCompletedEvent : IDataProcessingMessage
   {
       public string CorrelationId { get; set; }
       public DateTime Timestamp { get; set; }
       public string DataSourceId { get; set; }
       public string FileName { get; set; }
       public int TotalRecords { get; set; }
       public int ValidRecords { get; set; }
       public int InvalidRecords { get; set; }
   }
   ```

2. **MassTransit Configuration**:
   ```csharp
   public static class MassTransitConfiguration
   {
       public static void AddMassTransit(this IServiceCollection services, 
           IConfiguration configuration)
       {
           services.AddMassTransit(x =>
           {
               x.UsingKafka((context, cfg) =>
               {
                   cfg.Host(configuration.GetConnectionString("Kafka"));
                   
                   cfg.TopicEndpoint<FilePollingEvent>(
                       "dataprocessing.scheduling.filepolling", 
                       c => {
                           c.AutoDeleteOnIdle = TimeSpan.FromDays(1);
                           c.DefaultMessageTimeToLive = TimeSpan.FromHours(24);
                       });
               });
           });
       }
   }
   ```

3. **Base Consumer Class**:
   ```csharp
   public abstract class DataProcessingConsumerBase<T> : IConsumer<T> 
       where T : class, IDataProcessingMessage
   {
       protected readonly ILogger Logger;
       protected readonly IMetrics Metrics;

       public async Task Consume(ConsumeContext<T> context)
       {
           using var activity = Activity.StartActivity($"Consume{typeof(T).Name}");
           activity?.SetTag("correlation-id", context.Message.CorrelationId);
           
           Logger.LogInformation("Processing message", 
               context.Message.CorrelationId, typeof(T).Name);

           await ProcessMessage(context);
       }

       protected abstract Task ProcessMessage(ConsumeContext<T> context);
   }
   ```

**Deliverables**:
- MassTransit Kafka integration
- Message contract definitions
- Base consumer classes
- Correlation ID propagation middleware
- Topic naming convention implementation
- Message serialization configuration

#### Task 4: Monitoring & Observability Infrastructure
**Duration**: 4-5 days  
**Owner**: DevOps Engineer + Senior Backend Developer  
**Dependencies**: Task 1 completion  

**Detailed Steps**:
1. **Prometheus Metrics Integration**:
   ```csharp
   public class DataProcessingMetrics
   {
       private readonly IMetricsFactory _metricsFactory;
       
       // Standard metrics
       public readonly ICounter FilesProcessed;
       public readonly IHistogram ProcessingDuration;
       public readonly IGauge ValidationErrorRate;
       
       public DataProcessingMetrics(IMetricsFactory metricsFactory)
       {
           _metricsFactory = metricsFactory;
           
           FilesProcessed = _metricsFactory
               .CreateCounter("dataprocessing_files_processed_total",
                   "Total number of files processed",
                   new[] { "data_source", "service", "status" });
                   
           ProcessingDuration = _metricsFactory
               .CreateHistogram("dataprocessing_processing_duration_seconds",
                   "File processing duration in seconds",
                   new[] { "data_source", "service" });
       }
   }
   ```

2. **OpenTelemetry Configuration**:
   ```csharp
   public static class OpenTelemetryConfiguration
   {
       public static void AddOpenTelemetry(this IServiceCollection services,
           IConfiguration configuration)
       {
           services.AddOpenTelemetry()
               .WithTracing(builder =>
               {
                   builder.AddAspNetCoreInstrumentation()
                          .AddHttpClientInstrumentation()
                          .AddSource("DataProcessing.*")
                          .SetSampler(new TraceIdRatioBasedSampler(1.0))
                          .AddJaegerExporter(options =>
                          {
                              options.Endpoint = new Uri(configuration["Jaeger:Endpoint"]);
                          });
               });
       }
   }
   ```

3. **Structured Logging Setup**:
   ```csharp
   public static class LoggingConfiguration
   {
       public static void ConfigureLogging(this IServiceCollection services,
           IConfiguration configuration)
       {
           Log.Logger = new LoggerConfiguration()
               .Enrich.WithCorrelationId()
               .Enrich.WithProperty("Service", "DataProcessing")
               .WriteTo.Console()
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
                   new Uri(configuration["Elasticsearch:Uri"]))
               {
                   IndexFormat = "dataprocessing-logs-{0:yyyy.MM.dd}",
                   AutoRegisterTemplate = true
               })
               .CreateLogger();
               
           services.AddLogging(builder => builder.AddSerilog());
       }
   }
   ```

**Deliverables**:
- Prometheus metrics collection
- OpenTelemetry distributed tracing
- Structured logging with Elasticsearch
- Health check endpoints
- Correlation ID middleware
- Base monitoring classes

### Phase 2: Core Services (Weeks 4-8)

#### Task 5: Data Source Management Service
**Duration**: 6-7 days  
**Owner**: Backend Developer  
**Dependencies**: Tasks 2, 3, 4 completion  

**Detailed Implementation**:
1. **Clean Architecture Structure**:
   ```
   DataSourceManagementService/
   ├── Controllers/
   │   └── DataSourceController.cs
   ├── Services/
   │   ├── IDataSourceService.cs
   │   └── DataSourceService.cs
   ├── Repositories/
   │   ├── IDataSourceRepository.cs
   │   └── DataSourceRepository.cs
   ├── Models/
   │   ├── Requests/
   │   └── Responses/
   └── Validators/
   ```

2. **REST API Implementation**:
   ```csharp
   [ApiController]
   [Route("api/v1/datasource")]
   public class DataSourceController : ControllerBase
   {
       [HttpGet]
       public async Task<ActionResult<PagedResult<DataSourceResponse>>> GetDataSources(
           [FromQuery] DataSourceQuery query)
       {
           var correlationId = HttpContext.GetCorrelationId();
           using var activity = Activity.StartActivity("GetDataSources");
           activity?.SetTag("correlation-id", correlationId);
           
           try
           {
               var result = await _dataSourceService.GetDataSourcesAsync(query);
               return Ok(result);
           }
           catch (Exception ex)
           {
               return BadRequest(new ErrorResponse 
               { 
                   CorrelationId = correlationId,
                   Error = new ErrorDetail
                   {
                       Code = "GET_DATASOURCES_FAILED",
                       Message = "שגיאה בקבלת מקורות הנתונים", // Hebrew for UI
                       Details = ex.Message // English for logs
                   }
               });
           }
       }

       [HttpPost]
       public async Task<ActionResult<DataSourceResponse>> CreateDataSource(
           [FromBody] CreateDataSourceRequest request)
       {
           // Implementation with Hebrew error messages
       }
   }
   ```

3. **Schema Management**:
   ```csharp
   public class SchemaManager
   {
       public async Task<ValidationResult> ValidateSchema(BsonDocument schema)
       {
           var validator = new JsonSchemaValidator();
           return await validator.ValidateAsync(schema);
       }
       
       public async Task<bool> IsBackwardCompatible(BsonDocument oldSchema, 
           BsonDocument newSchema)
       {
           // Implement backward compatibility checks
           return await CompatibilityChecker.CheckAsync(oldSchema, newSchema);
       }
   }
   ```

**Deliverables**:
- Complete CRUD API operations
- Schema validation and versioning
- Hebrew error message responses
- OpenAPI/Swagger documentation
- Unit and integration tests

#### Task 6: Scheduling Service (Quartz.NET)
**Duration**: 5-6 days  
**Owner**: Senior Backend Developer  
**Dependencies**: Tasks 3, 4, 5 completion  

**Detailed Implementation**:
1. **Quartz.NET Job Configuration**:
   ```csharp
   [DisallowConcurrentExecution]
   public class DataSourcePollingJob : IJob
   {
       private readonly IDataSourceRepository _dataSourceRepository;
       private readonly IPublishEndpoint _publishEndpoint;
       private readonly ILogger<DataSourcePollingJob> _logger;
       private readonly DataProcessingMetrics _metrics;

       public async Task Execute(IJobExecutionContext context)
       {
           var correlationId = Guid.NewGuid().ToString();
           using var activity = Activity.StartActivity("DataSourcePolling");
           activity?.SetTag("correlation-id", correlationId);

           _logger.LogInformation("Starting data source polling", correlationId);

           try
           {
               var dataSources = await _dataSourceRepository
                   .GetActiveDataSourcesAsync();

               foreach (var dataSource in dataSources)
               {
                   await _publishEndpoint.Publish(new FilePollingEvent
                   {
                       CorrelationId = correlationId,
                       Timestamp = DateTime.UtcNow,
                       DataSourceId = dataSource.ID,
                       FilePath = dataSource.FilePath
                   });
               }

               _metrics.FilesProcessed.Inc(dataSources.Count, 
                   new[] { "all", "scheduling", "success" });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error during data source polling", correlationId);
               throw;
           }
       }
   }
   ```

2. **Dynamic Scheduling Management**:
   ```csharp
   public class SchedulingManager
   {
       private readonly IScheduler _scheduler;
       
       public async Task UpdateDataSourceSchedule(string dataSourceId, 
           TimeSpan pollingRate)
       {
           var jobKey = new JobKey($"polling-{dataSourceId}", "datasource");
           var triggerKey = new TriggerKey($"trigger-{dataSourceId}", "datasource");
           
           // Remove existing job if present
           await _scheduler.DeleteJob(jobKey);
           
           // Create new job with updated schedule
           var job = JobBuilder.Create<DataSourcePollingJob>()
               .WithIdentity(jobKey)
               .UsingJobData("dataSourceId", dataSourceId)
               .Build();

           var trigger = TriggerBuilder.Create()
               .WithIdentity(triggerKey)
               .WithSimpleSchedule(x => x.WithInterval(pollingRate).RepeatForever())
               .Build();

           await _scheduler.ScheduleJob(job, trigger);
       }
   }
   ```

**Deliverables**:
- Quartz.NET job implementation
- Dynamic schedule management
- Job monitoring and health checks
- Overlap prevention mechanisms
- Job failure handling and retry logic

#### Task 7: Files Receiver Service
**Duration**: 5-6 days  
**Owner**: Backend Developer  
**Dependencies**: Task 6 completion  

**Detailed Implementation**:
1. **File Polling Event Consumer**:
   ```csharp
   public class FilePollingEventConsumer : DataProcessingConsumerBase<FilePollingEvent>
   {
       private readonly IFileProcessor _fileProcessor;
       private readonly IPublishEndpoint _publishEndpoint;

       protected override async Task ProcessMessage(ConsumeContext<FilePollingEvent> context)
       {
           var message = context.Message;
           var correlationId = message.CorrelationId;

           try
           {
               var files = await _fileProcessor.DiscoverNewFilesAsync(
                   message.FilePath, correlationId);

               foreach (var file in files)
               {
                   var fileContent = await _fileProcessor.ReadFileAsync(file, correlationId);
                   
                   await _publishEndpoint.Publish(new ValidationRequestEvent
                   {
                       CorrelationId = correlationId,
                       DataSourceId = message.DataSourceId,
                       FileName = Path.GetFileName(file),
                       FileContent = fileContent,
                       Timestamp = DateTime.UtcNow
                   });
               }

               _metrics.FilesProcessed.Inc(files.Count, 
                   new[] { message.DataSourceId, "receiver", "success" });
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error processing files for data source {DataSourceId}", 
                   message.DataSourceId, correlationId);
               
               await _publishEndpoint.Publish(new FileProcessingFailedEvent
               {
                   CorrelationId = correlationId,
                   DataSourceId = message.DataSourceId,
                   Error = ex.Message,
                   Timestamp = DateTime.UtcNow
               });
           }
       }
   }
   ```

2. **File Processing with Retry Logic**:
   ```csharp
   public class FileProcessor : IFileProcessor
   {
       private readonly RetryPolicy _retryPolicy;

       public async Task<List<string>> DiscoverNewFilesAsync(string path, string correlationId)
       {
           return await _retryPolicy.ExecuteAsync(async () =>
           {
               if (!Directory.Exists(path))
               {
                   throw new DirectoryNotFoundException($"Path not found: {path}");
               }

               var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                   .Where(f => IsNewFile(f))
                   .ToList();

               _logger.LogInformation("Discovered {FileCount} new files in {Path}", 
                   files.Count, path, correlationId);

               return files;
           });
       }

       public async Task<byte[]> ReadFileAsync(string filePath, string correlationId)
       {
           return await _retryPolicy.ExecuteAsync(async () =>
           {
               using var activity = Activity.StartActivity("ReadFile");
               activity?.SetTag("file-path", filePath);
               activity?.SetTag("correlation-id", correlationId);

               var fileInfo = new FileInfo(filePath);
               _metrics.FileSizeBytes.Observe(fileInfo.Length, 
                   new[] { Path.GetExtension(filePath) });

               return await File.ReadAllBytesAsync(filePath);
           });
       }
   }
   ```

**Deliverables**:
- File discovery and ingestion logic
- Multiple file format support
- Retry mechanisms with exponential backoff
- File processing metrics collection
- Error handling and notification

#### Task 8: Validation Service
**Duration**: 7-8 days  
**Owner**: Senior Backend Developer  
**Dependencies**: Task 7 completion  

**Detailed Implementation**:
1. **Validation Request Consumer**:
   ```csharp
   public class ValidationRequestEventConsumer : DataProcessingConsumerBase<ValidationRequestEvent>
   {
       private readonly ISchemaValidator _schemaValidator;
       private readonly IInvalidRecordRepository _invalidRecordRepository;

       protected override async Task ProcessMessage(ConsumeContext<ValidationRequestEvent> context)
       {
           var message = context.Message;
           var stopwatch = Stopwatch.StartNew();

           try
           {
               var dataSource = await _dataSourceRepository
                   .GetByIdAsync(message.DataSourceId);
               
               var validationResult = await _schemaValidator
                   .ValidateAsync(message.FileContent, dataSource.JsonSchema, 
                                  message.CorrelationId);

               // Store invalid records
               if (validationResult.InvalidRecords.Any())
               {
                   await _invalidRecordRepository
                       .BulkInsertAsync(validationResult.InvalidRecords);
               }

               // Publish validation completed event
               await _publishEndpoint.Publish(new ValidationCompletedEvent
               {
                   CorrelationId = message.CorrelationId,
                   DataSourceId = message.DataSourceId,
                   FileName = message.FileName,
                   TotalRecords = validationResult.TotalRecords,
                   ValidRecords = validationResult.ValidRecords,
                   InvalidRecords = validationResult.InvalidRecords.Count,
                   ProcessingDuration = stopwatch.Elapsed,
                   Timestamp = DateTime.UtcNow
               });

               // Update metrics
               _metrics.ValidationErrorRate.Set(
                   (double)validationResult.InvalidRecords.Count / validationResult.TotalRecords,
                   new[] { message.DataSourceId });

           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Validation failed for file {FileName}", 
                   message.FileName, message.CorrelationId);
           }
           finally
           {
               stopwatch.Stop();
               _metrics.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds,
                   new[] { message.DataSourceId, "validation" });
           }
       }
   }
   ```

2. **Schema Validation Implementation**:
   ```csharp
   public class SchemaValidator : ISchemaValidator
   {
       private readonly IMemoryCache _schemaCache;

       public async Task<ValidationResult> ValidateAsync(byte[] fileContent, 
           BsonDocument jsonSchema, string correlationId)
       {
           using var activity = Activity.StartActivity("SchemaValidation");
           
           var schema = await GetCachedSchemaAsync(jsonSchema);
           var records = DeserializeRecords(fileContent);
           var result = new ValidationResult();

           foreach (var record in records)
           {
               result.TotalRecords++;
               
               var validationErrors = ValidateRecord(record, schema);
               if (validationErrors.Any())
               {
                   result.InvalidRecords.Add(new DataProcessingInvalidRecord
                   {
                       OriginalRecord = record,
                       ValidationErrors = validationErrors,
                       CorrelationId = correlationId,
                       ErrorType = DetermineErrorType(validationErrors)
                   });
               }
               else
               {
                   result.ValidRecords++;
               }
           }

           return result;
       }
   }
   ```

**Deliverables**:
- Schema-based validation engine
- Invalid record storage and management
- Validation metrics and reporting
- Schema caching for performance
- Detailed error tracking and classification

### Phase 3: User Interface & AI Services (Weeks 9-12)

#### Task 9: Hebrew Frontend Application
**Duration**: 10-12 days  
**Owner**: Frontend Developer + UI/UX Designer  
**Dependencies**: Tasks 5, 8 completion  

**Detailed Implementation**:
1. **React Application Structure**:
   ```
   src/Frontend/
   ├── public/
   │   ├── index.html (with dir="rtl" lang="he")
   │   └── locales/
   │       ├── he.json
   │       └── en.json
   ├── src/
   │   ├── components/
   │   │   ├── common/
   │   │   │   ├── Layout.tsx
   │   │   │   └── Navigation.tsx
   │   │   ├── datasources/
   │   │   │   ├── DataSourceList.tsx
   │   │   │   ├── DataSourceForm.tsx
   │   │   │   └── SchemaBuilder.tsx
   │   │   ├── validation/
   │   │   │   ├── InvalidRecords.tsx
   │   │   │   └── ValidationMetrics.tsx
   │   │   └── dashboard/
   │   │       ├── Dashboard.tsx
   │   │       └── MetricsCards.tsx
   │   ├── services/
   │   │   ├── api.ts
   │   │   └── websocket.ts
   │   ├── hooks/
   │   │   ├── useDataSources.ts
   │   │   └── useValidation.ts
   │   ├── styles/
   │   │   ├── globals.css
   │   │   └── rtl.css
   │   └── utils/
   │       ├── i18n.ts
   │       └── rtl-helpers.ts
   ```

2. **RTL Layout Implementation**:
   ```tsx
   // Layout component with RTL support
   const Layout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
     return (
       <div className="layout-container" dir="rtl" lang="he">
         <Header />
         <nav className="sidebar">
           <NavigationMenu />
         </nav>
         <main className="content">
           {children}
         </main>
       </div>
     );
   };

   // CSS for RTL support
   .layout-container[dir="rtl"] {
     text-align: right;
     direction: rtl;
   }

   .layout-container[dir="rtl"] .sidebar {
     right: 0;
     left: auto;
   }

   .layout-container[dir="rtl"] .content {
     margin-right: 250px;
     margin-left: 0;
   }
   ```

3. **Data Source Management Interface**:
   ```tsx
   const DataSourceList: React.FC = () => {
     const { dataSources, loading, error } = useDataSources();
     const { t } = useTranslation();

     return (
       <div className="data-source-list" dir="rtl">
         <div className="page-header">
           <h1>{t('datasources.title')}</h1> {/* מקורות נתונים */}
           <Button onClick={() => setShowCreateForm(true)}>
             {t('datasources.create')} {/* + הוסף מקור נתונים חדש */}
           </Button>
         </div>
         
         <div className="filters">
           <SearchInput 
             placeholder={t('datasources.search')} 
             dir="rtl" 
             style={{ textAlign: 'right' }}
           />
           <Select placeholder={t('datasources.category')}>
             <Option value="financial">{t('categories.financial')}</Option>
             <Option value="customers">{t('categories.customers')}</Option>
           </Select>
         </div>

         <Table 
           dataSource={dataSources}
           columns={[
             { title: t('datasources.name'), dataIndex: 'name', align: 'right' },
             { title: t('datasources.supplier'), dataIndex: 'supplier', align: 'right' },
             { title: t('datasources.status'), dataIndex: 'status', align: 'right' }
           ]}
           className="rtl-table"
         />
       </div>
     );
   };
   ```

4. **Schema Builder Interface**:
   ```tsx
   const SchemaBuilder: React.FC = () => {
     const [schema, setSchema] = useState<JsonSchema>({});
     const { t } = useTranslation();

     return (
       <div className="schema-builder" dir="rtl">
         <Tabs defaultActiveKey="visual">
           <TabPane tab={t('schema.visual')} key="visual">
             <VisualSchemaEditor schema={schema} onChange={setSchema} />
           </TabPane>
           <TabPane tab={t('schema.json')} key="json">
             <JsonSchemaEditor schema={schema} onChange={setSchema} />
           </TabPane>
           <TabPane tab={t('schema.validation')} key="validation">
             <SchemaValidationTester schema={schema} />
           </TabPane>
         </Tabs>
       </div>
     );
   };
   ```

**Deliverables**:
- Complete Hebrew UI matching mockup design
- RTL layout implementation across all components
- Data source management with CRUD operations
- Schema builder with visual and JSON editors
- Invalid records management interface
- Dashboard with metrics visualization
- Responsive design for multiple screen sizes

#### Task 10: AI Chat Service Implementation
**Duration**: 8-9 days  
**Owner**: Senior Backend Developer + AI Integration Specialist  
**Dependencies**: Tasks 5, 8 completion  

**Detailed Implementation**:
1. **AI Service Architecture**:
   ```csharp
   public class DataSourceChatService
   {
       private readonly ILLMProvider _llmProvider;
       private readonly IMCPServerClient _grafanaMCP;
       private readonly IMCPServerClient _mongoMCP;
       private readonly IChatHistoryRepository _chatHistory;

       public async Task<ChatResponse> ProcessChatMessage(ChatMessage message)
       {
           using var activity = Activity.StartActivity("ProcessChatMessage");
           activity?.SetTag("correlation-id", message.CorrelationId);
           activity?.SetTag("language", "hebrew");

           var context = await BuildContextFromMessage(message);
           var response = await _llmProvider.GenerateResponse(context);
           
           if (response.RequiresDataQuery)
           {
               var data = await ExecuteDataQuery(response.Query);
               response = await _llmProvider.EnrichResponseWithData(response, data);
           }

           if (response.RequiresDashboard)
           {
               var dashboardUrl = await CreateGrafanaDashboard(response.DashboardConfig);
               response.DashboardUrl = dashboardUrl;
           }

           await _chatHistory.SaveChatExchangeAsync(message, response);
           return response;
       }
   }
   ```

2. **MCP Server Integration**:
   ```csharp
   public class MCPServerClient : IMCPServerClient
   {
       public async Task<QueryResult> ExecuteMongoQuery(string query, string correlationId)
       {
           var request = new MCPRequest
           {
               Method = "query.execute",
               Parameters = new { query, correlationId }
           };

           var response = await _httpClient.PostAsync(_mongoMCPEndpoint, 
               JsonContent.Create(request));

           return await response.Content.ReadFromJsonAsync<QueryResult>();
       }

       public async Task<string> CreateGrafanaDashboard(DashboardConfig config)
       {
           var request = new MCPRequest
           {
               Method = "dashboard.create",
               Parameters = config
           };

           var response = await _httpClient.PostAsync(_grafanaMCPEndpoint,
               JsonContent.Create(request));

           var result = await response.Content.ReadFromJsonAsync<DashboardResult>();
           return result.Url;
       }
   }
   ```

3. **Hebrew Language Support**:
   ```csharp
   public class HebrewChatProcessor
   {
       public async Task<ProcessedMessage> ProcessHebrewMessage(string message)
       {
           // Handle RTL text processing
           var normalizedText = NormalizeHebrewText(message);
           
           // Extract entities and intent in Hebrew context
           var entities = await ExtractHebrewEntities(normalizedText);
           var intent = await ClassifyHebrewIntent(normalizedText);

           return new ProcessedMessage
           {
               NormalizedText = normalizedText,
               Entities = entities,
               Intent = intent,
               Language = "hebrew"
           };
       }
   }
   ```

**Deliverables**:
- AI chat service with Hebrew language support
- MCP server integration for Grafana and MongoDB
- Natural language query processing
- Dashboard generation capabilities
- Chat history management
- Contextual conversation handling

### Phase 4: Deployment & Production (Weeks 13-15)

#### Task 11: Docker & Kubernetes Deployment
**Duration**: 6-7 days  
**Owner**: DevOps Engineer + Senior Backend Developer  
**Dependencies**: Tasks 9, 10 completion  

**Detailed Implementation**:
1. **Multi-stage Dockerfile Template**:
   ```dockerfile
   # Base build stage
   FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
   WORKDIR /src
   COPY ["*.csproj", "./"]
   RUN dotnet restore
   COPY . .
   RUN dotnet build -c Release -o /app/build

   # Publish stage
   FROM build AS publish
   RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

   # Runtime stage
   FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
   WORKDIR /app
   
   # Create non-root user
   RUN adduser --disabled-password --home /app --gecos '' appuser && \
       chown -R appuser /app
   USER appuser

   COPY --from=publish /app/publish .
   
   # Health check
   HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
       CMD curl -f http://localhost:8080/health || exit 1

   ENTRYPOINT ["dotnet", "DataProcessing.Service.dll"]
   ```

2. **Kubernetes Deployment Templates**:
   ```yaml
   # Service deployment template
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: {{ .Values.service.name }}
     namespace: {{ .Values.namespace }}
   spec:
     replicas: {{ .Values.replicaCount }}
     selector:
       matchLabels:
         app: {{ .Values.service.name }}
     template:
       metadata:
         labels:
           app: {{ .Values.service.name }}
       spec:
         containers:
         - name: {{ .Values.service.name }}
           image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
           ports:
           - containerPort: 8080
           env:
           - name: ASPNETCORE_ENVIRONMENT
             value: {{ .Values.environment }}
           - name: ConnectionStrings__MongoDB
             valueFrom:
               secretKeyRef:
                 name: mongodb-secret
                 key: connection-string
           resources:
             limits:
               cpu: {{ .Values.resources.limits.cpu }}
               memory: {{ .Values.resources.limits.memory }}
             requests:
               cpu: {{ .Values.resources.requests.cpu }}
               memory: {{ .Values.resources.requests.memory }}
           readinessProbe:
             httpGet:
               path: /health/ready
               port: 8080
             initialDelaySeconds: 30
             periodSeconds: 10
           livenessProbe:
             httpGet:
               path: /health/live
               port: 8080
             initialDelaySeconds: 60
             periodSeconds: 30
   ```

3. **Helm Chart Values**:
   ```yaml
   # values.yaml
   replicaCount: 3
   
   image:
     repository: dataprocessing
     tag: latest
     pullPolicy: IfNotPresent
   
   service:
     type: ClusterIP
     port: 80
   
   resources:
     limits:
       cpu: 1000m
       memory: 1Gi
     requests:
       cpu: 500m
       memory: 512Mi
   
   autoscaling:
     enabled: true
     minReplicas: 2
     maxReplicas: 10
     targetCPUUtilizationPercentage: 70
     targetMemoryUtilizationPercentage: 80
   ```

**Deliverables**:
- Docker images for all services
- Kubernetes deployment manifests
- Helm charts with environment-specific values
- Auto-scaling configurations
- Resource limits and health checks
- Production-ready deployment scripts

#### Task 12: Monitoring Stack Setup
**Duration**: 5-6 days  
**Owner**: DevOps Engineer  
**Dependencies**: Task 11 completion  

**Detailed Implementation**:
1. **Prometheus Configuration**:
   ```yaml
   # prometheus.yml
   global:
     scrape_interval: 15s
     evaluation_interval: 15s
   
   rule_files:
     - "dataprocessing_rules.yml"
   
   scrape_configs:
   - job_name: 'dataprocessing-services'
     kubernetes_sd_configs:
     - role: endpoints
     relabel_configs:
     - source_labels: [__meta_kubernetes_service_annotation_prometheus_io_scrape]
       action: keep
       regex: true
     - source_labels: [__meta_kubernetes_service_annotation_prometheus_io_path]
       action: replace
       target_label: __metrics_path__
       regex: (.+)
   ```

2. **Grafana Dashboard Templates**:
   ```json
   {
     "dashboard": {
       "title": "Data Processing Platform Overview",
       "panels": [
         {
           "title": "Files Processed",
           "type": "stat",
           "targets": [
             {
               "expr": "sum(rate(dataprocessing_files_processed_total[5m]))",
               "legendFormat": "Files/sec"
             }
           ]
         },
         {
           "title": "Validation Error Rate",
           "type": "gauge",
           "targets": [
             {
               "expr": "sum(dataprocessing_validation_error_rate) / sum(dataprocessing_files_processed_total) * 100",
               "legendFormat": "Error Rate %"
             }
           ]
         }
       ]
     }
   }
   ```

3. **Alerting Rules**:
   ```yaml
   groups:
   - name: dataprocessing.rules
     rules:
     - alert: HighErrorRate
       expr: sum(rate(dataprocessing_validation_errors_total[5m])) / sum(rate(dataprocessing_files_processed_total[5m])) > 0.05
       for: 2m
       labels:
         severity: critical
       annotations:
         summary: "High validation error rate detected"
         description: "Error rate is {{ $value | humanizePercentage }}"
     
     - alert: ServiceDown
       expr: up{job="dataprocessing-services"} == 0
       for: 1m
       labels:
         severity: critical
       annotations:
         summary: "Service {{ $labels.instance }} is down"
   ```

**Deliverables**:
- Prometheus deployment with service discovery
- Grafana dashboards for all services
- Alerting rules for critical conditions
- Elasticsearch cluster for log aggregation
- Log retention and cleanup policies

#### Task 13: Integration Testing & System Validation
**Duration**: 7-8 days  
**Owner**: QA Engineer + Backend Developers  
**Dependencies**: Task 12 completion  

**Detailed Implementation**:
1. **End-to-End Test Framework**:
   ```csharp
   [TestClass]
   public class EndToEndWorkflowTests
   {
       private TestcontainersBuilder _containers;
       private ITestOutputHelper _output;

       [TestInitialize]
       public async Task Setup()
       {
           _containers = new TestcontainersBuilder()
               .WithMongoDB()
               .WithKafka()
               .WithElasticsearch();
           
           await _containers.StartAsync();
       }

       [TestMethod]
       public async Task CompleteFileProcessingWorkflow_ShouldProcessSuccessfully()
       {
           // Arrange
           var correlationId = Guid.NewGuid().ToString();
           var testFile = CreateTestDataFile();
           var dataSource = CreateTestDataSource();
           
           // Act - Trigger the complete workflow
           await TriggerFilePollingEvent(dataSource.ID, testFile.Path, correlationId);
           
           // Assert - Verify end-to-end processing
           await VerifyFileProcessed(correlationId);
           await VerifyValidationCompleted(correlationId);
           await VerifyMetricsRecorded(correlationId);
           await VerifyUIUpdated(correlationId);
       }
   }
   ```

2. **Hebrew UI Automated Tests**:
   ```csharp
   [TestClass]
   public class HebrewUITests
   {
       private IWebDriver _driver;

       [TestMethod]
       public async Task DataSourceList_ShouldDisplayHebrewLabelsWithRTL()
       {
           // Navigate to data sources page
           _driver.Navigate().GoToUrl($"{BaseUrl}/datasources");
           
           // Verify RTL layout
           var container = _driver.FindElement(By.ClassName("data-source-list"));
           Assert.AreEqual("rtl", container.GetAttribute("dir"));
           
           // Verify Hebrew labels
           var title = _driver.FindElement(By.TagName("h1"));
           Assert.AreEqual("מקורות נתונים", title.Text);
           
           // Verify table alignment
           var table = _driver.FindElement(By.ClassName("rtl-table"));
           var headers = table.FindElements(By.TagName("th"));
           foreach (var header in headers)
           {
               Assert.AreEqual("right", header.GetCSSValue("text-align"));
           }
       }
   }
   ```

3. **Performance and Load Tests**:
   ```csharp
   [TestClass]
   public class LoadTests
   {
       [TestMethod]
       public async Task FileProcessing_ShouldHandleHighVolumeLoad()
       {
           var tasks = new List<Task>();
           var fileCount = 1000;
           var concurrency = 10;

           for (int i = 0; i < concurrency; i++)
           {
               tasks.Add(ProcessFilesAsync(fileCount / concurrency));
           }

           var stopwatch = Stopwatch.StartNew();
           await Task.WhenAll(tasks);
           stopwatch.Stop();

           // Assert performance requirements
           Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMinutes(10));
           
           // Verify no data loss
           var processedCount = await GetProcessedFileCount();
           Assert.AreEqual(fileCount, processedCount);
       }
   }
   ```

**Deliverables**:
- Complete integration test suite
- Hebrew UI automated tests
- Performance and load testing framework
- Correlation ID tracking validation
- Error scenario testing
- Production deployment validation

## Risk Mitigation Strategies

### Technical Risks
1. **Hebrew RTL Complexity**: Comprehensive testing framework for RTL layouts
2. **Microservices Communication**: Correlation ID tracking and circuit breakers
3. **Data Consistency**: MongoDB transactions and event sourcing patterns
4. **Performance Bottlenecks**: Auto-scaling and caching strategies

### Operational Risks
1. **Deployment Complexity**: Helm charts and automated deployment pipelines
2. **Monitoring Blind Spots**: Comprehensive observability stack
3. **Data Loss**: Backup strategies and disaster recovery procedures

## Success Metrics

### Performance Indicators
- **Throughput**: Process 10,000+ files per hour
- **Availability**: 99.9% uptime for all critical services
- **Response Time**: Sub-second API response times
- **Error Rate**: Less than 0.1% data validation errors

### Quality Indicators
- **Test Coverage**: 95%+ code coverage
- **Hebrew UI Compliance**: 100% RTL layout consistency
- **Correlation Tracking**: 100% request traceability
- **Documentation Coverage**: Complete API and deployment documentation

## Conclusion

This detailed implementation plan provides a comprehensive roadmap for building the Data Processing Platform with all specified requirements including Hebrew UI support, microservices architecture, and comprehensive monitoring. The phased approach ensures systematic development with proper testing and validation at each stage.

The plan emphasizes:
- **Quality First**: Comprehensive testing and monitoring from the foundation
- **Hebrew UI Excellence**: Consistent RTL implementation and user experience
- **Operational Excellence**: Production-ready deployment and monitoring
- **Developer Experience**: Clear patterns and reusable components

Total estimated delivery time: **15 weeks** with a team of 6-8 developers.

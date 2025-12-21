using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessing.Shared.Monitoring;

/// <summary>
/// Helper class for emitting system/infrastructure metrics that will be routed to System Prometheus.
/// All metrics use standard Prometheus naming conventions for infrastructure monitoring.
///
/// These metrics are explicitly embedded in each microservice for consistent observability
/// and correlation with business metrics in alerts.
///
/// SYSTEM METRICS CATEGORIES:
/// - Process: CPU, memory, thread usage
/// - .NET Runtime: GC, heap, JIT
/// - HTTP: Request counts, durations, in-progress
/// - Application: Uptime, version, health
/// </summary>
public class SystemMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly string _serviceName;
    private bool _disposed;
    private readonly DateTime _startTime;

    #region Process Metrics

    // Observable gauges for process metrics
    private ObservableGauge<double>? _processCpuUsage;
    private ObservableCounter<double>? _processCpuSecondsTotal;
    private ObservableGauge<long>? _processResidentMemoryBytes;
    private ObservableGauge<long>? _processVirtualMemoryBytes;
    private ObservableGauge<int>? _processThreadsTotal;
    private ObservableGauge<int>? _processHandlesTotal;

    #endregion

    #region .NET Runtime Metrics

    // GC and heap metrics
    private ObservableGauge<long>? _dotnetGcHeapSizeBytes;
    private ObservableCounter<long>? _dotnetGcCollectionsTotal;
    private ObservableGauge<long>? _dotnetGcGen0Size;
    private ObservableGauge<long>? _dotnetGcGen1Size;
    private ObservableGauge<long>? _dotnetGcGen2Size;
    private ObservableGauge<long>? _dotnetGcLohSize;

    // Thread pool metrics
    private ObservableGauge<int>? _dotnetThreadPoolThreadsTotal;
    private ObservableGauge<long>? _dotnetThreadPoolQueueLength;
    private ObservableGauge<int>? _dotnetThreadPoolCompletedItemsTotal;

    #endregion

    #region HTTP Metrics

    // HTTP request tracking
    private readonly Counter<long> _httpRequestsTotal;
    private readonly Histogram<double> _httpRequestDurationSeconds;
    private readonly UpDownCounter<int> _httpRequestsInProgress;

    #endregion

    #region Application Metrics

    // Application health/uptime
    private ObservableGauge<int>? _upGauge;
    private ObservableGauge<double>? _processStartTimeSeconds;
    private ObservableGauge<double>? _processUptimeSeconds;

    #endregion

    private readonly Process _currentProcess;

    public SystemMetrics(Meter meter, string serviceName)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _currentProcess = Process.GetCurrentProcess();
        _startTime = DateTime.UtcNow;

        // Initialize HTTP metrics (these are counters/histograms that need to be explicitly recorded)
        _httpRequestsTotal = _meter.CreateCounter<long>(
            "http_requests_total",
            description: "Total number of HTTP requests handled");

        _httpRequestDurationSeconds = _meter.CreateHistogram<double>(
            "http_request_duration_seconds",
            unit: "s",
            description: "HTTP request duration in seconds");

        _httpRequestsInProgress = _meter.CreateUpDownCounter<int>(
            "http_requests_in_progress",
            description: "Number of HTTP requests currently in progress");

        // Initialize observable metrics
        InitializeProcessMetrics();
        InitializeDotNetRuntimeMetrics();
        InitializeApplicationMetrics();
    }

    #region Initialization Methods

    private void InitializeProcessMetrics()
    {
        // CPU usage percentage (0-100)
        _processCpuUsage = _meter.CreateObservableGauge(
            "process_cpu_usage",
            () => GetCpuUsagePercentage(),
            unit: "percent",
            description: "Current CPU usage percentage");

        // Cumulative CPU time in seconds
        _processCpuSecondsTotal = _meter.CreateObservableCounter(
            "process_cpu_seconds_total",
            () => _currentProcess.TotalProcessorTime.TotalSeconds,
            unit: "s",
            description: "Total user and system CPU time spent in seconds");

        // Memory metrics
        _processResidentMemoryBytes = _meter.CreateObservableGauge(
            "process_resident_memory_bytes",
            () => _currentProcess.WorkingSet64,
            unit: "By",
            description: "Resident memory size in bytes");

        _processVirtualMemoryBytes = _meter.CreateObservableGauge(
            "process_virtual_memory_bytes",
            () => _currentProcess.VirtualMemorySize64,
            unit: "By",
            description: "Virtual memory size in bytes");

        // Thread and handle counts
        _processThreadsTotal = _meter.CreateObservableGauge(
            "process_threads_total",
            () => _currentProcess.Threads.Count,
            description: "Total number of threads");

        _processHandlesTotal = _meter.CreateObservableGauge(
            "process_handles_total",
            () => _currentProcess.HandleCount,
            description: "Total number of open handles");
    }

    private void InitializeDotNetRuntimeMetrics()
    {
        // Heap size
        _dotnetGcHeapSizeBytes = _meter.CreateObservableGauge(
            "dotnet_gc_heap_size_bytes",
            () => GC.GetTotalMemory(forceFullCollection: false),
            unit: "By",
            description: "Total managed heap size in bytes");

        // GC collection counts by generation
        _dotnetGcCollectionsTotal = _meter.CreateObservableCounter(
            "dotnet_gc_collections_total",
            () => GetGcCollectionMeasurements(),
            description: "Total number of GC collections by generation");

        // Generation sizes
        var gcInfo = GC.GetGCMemoryInfo();

        _dotnetGcGen0Size = _meter.CreateObservableGauge(
            "dotnet_gc_gen0_size_bytes",
            () => GetGenerationSize(0),
            unit: "By",
            description: "Size of generation 0 heap");

        _dotnetGcGen1Size = _meter.CreateObservableGauge(
            "dotnet_gc_gen1_size_bytes",
            () => GetGenerationSize(1),
            unit: "By",
            description: "Size of generation 1 heap");

        _dotnetGcGen2Size = _meter.CreateObservableGauge(
            "dotnet_gc_gen2_size_bytes",
            () => GetGenerationSize(2),
            unit: "By",
            description: "Size of generation 2 heap");

        _dotnetGcLohSize = _meter.CreateObservableGauge(
            "dotnet_gc_loh_size_bytes",
            () => GetLargeObjectHeapSize(),
            unit: "By",
            description: "Size of large object heap");

        // Thread pool metrics
        _dotnetThreadPoolThreadsTotal = _meter.CreateObservableGauge(
            "dotnet_threadpool_threads_total",
            () =>
            {
                ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
                return (maxWorkerThreads - workerThreads) + (maxCompletionPortThreads - completionPortThreads);
            },
            description: "Total number of active thread pool threads");

        _dotnetThreadPoolQueueLength = _meter.CreateObservableGauge(
            "dotnet_threadpool_queue_length",
            () => ThreadPool.PendingWorkItemCount,
            description: "Number of work items queued in the thread pool");
    }

    private void InitializeApplicationMetrics()
    {
        // Up gauge (1 = service is running)
        _upGauge = _meter.CreateObservableGauge(
            "up",
            () => new Measurement<int>(1, new TagList
            {
                { "job", "dataprocessing" },
                { "service", _serviceName }
            }),
            description: "Service is up (1) or down (0)");

        // Process start time (Unix timestamp)
        _processStartTimeSeconds = _meter.CreateObservableGauge(
            "process_start_time_seconds",
            () => new DateTimeOffset(_currentProcess.StartTime).ToUnixTimeSeconds(),
            unit: "s",
            description: "Start time of the process since unix epoch in seconds");

        // Uptime in seconds
        _processUptimeSeconds = _meter.CreateObservableGauge(
            "process_uptime_seconds",
            () => (DateTime.UtcNow - _startTime).TotalSeconds,
            unit: "s",
            description: "Process uptime in seconds");
    }

    #endregion

    #region Helper Methods

    private double GetCpuUsagePercentage()
    {
        try
        {
            _currentProcess.Refresh();
            var cpuTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var uptime = (DateTime.UtcNow - _currentProcess.StartTime).TotalMilliseconds;
            var processorCount = Environment.ProcessorCount;

            if (uptime <= 0) return 0;

            return (cpuTime / uptime / processorCount) * 100;
        }
        catch
        {
            return 0;
        }
    }

    private IEnumerable<Measurement<long>> GetGcCollectionMeasurements()
    {
        yield return new Measurement<long>(GC.CollectionCount(0), new TagList { { "generation", "0" } });
        yield return new Measurement<long>(GC.CollectionCount(1), new TagList { { "generation", "1" } });
        yield return new Measurement<long>(GC.CollectionCount(2), new TagList { { "generation", "2" } });
    }

    private long GetGenerationSize(int generation)
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            if (generation < gcInfo.GenerationInfo.Length)
            {
                return gcInfo.GenerationInfo[generation].SizeAfterBytes;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private long GetLargeObjectHeapSize()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            // LOH is generation 3
            if (gcInfo.GenerationInfo.Length > 3)
            {
                return gcInfo.GenerationInfo[3].SizeAfterBytes;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #region HTTP Metrics Recording Methods

    /// <summary>
    /// Records an HTTP request completion
    /// Labels: method, path, status_code, service
    /// </summary>
    public void RecordHttpRequest(string method, string path, int statusCode, double durationSeconds)
    {
        var tags = new TagList
        {
            { "method", method },
            { "path", NormalizePath(path) },
            { "status_code", statusCode.ToString() },
            { "service", _serviceName }
        };

        _httpRequestsTotal.Add(1, tags);
        _httpRequestDurationSeconds.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Increments in-progress HTTP request counter
    /// </summary>
    public void IncrementHttpRequestsInProgress(string method, string path)
    {
        var tags = new TagList
        {
            { "method", method },
            { "path", NormalizePath(path) },
            { "service", _serviceName }
        };

        _httpRequestsInProgress.Add(1, tags);
    }

    /// <summary>
    /// Decrements in-progress HTTP request counter
    /// </summary>
    public void DecrementHttpRequestsInProgress(string method, string path)
    {
        var tags = new TagList
        {
            { "method", method },
            { "path", NormalizePath(path) },
            { "service", _serviceName }
        };

        _httpRequestsInProgress.Add(-1, tags);
    }

    /// <summary>
    /// Creates a scope that automatically tracks HTTP request duration and in-progress count
    /// </summary>
    public HttpRequestScope CreateHttpRequestScope(string method, string path)
    {
        return new HttpRequestScope(this, method, path);
    }

    private static string NormalizePath(string path)
    {
        // Normalize paths to avoid high cardinality
        // Replace IDs with placeholders
        if (string.IsNullOrEmpty(path)) return "/";

        // Remove query string
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        // Replace GUIDs with :id placeholder
        path = System.Text.RegularExpressions.Regex.Replace(
            path,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            ":id");

        // Replace numeric IDs with :id placeholder
        path = System.Text.RegularExpressions.Regex.Replace(
            path,
            @"/\d+(?=/|$)",
            "/:id");

        return path;
    }

    #endregion

    #region HttpRequestScope

    /// <summary>
    /// Disposable scope for tracking HTTP request lifecycle
    /// </summary>
    public sealed class HttpRequestScope : IDisposable
    {
        private readonly SystemMetrics _metrics;
        private readonly string _method;
        private readonly string _path;
        private readonly Stopwatch _stopwatch;
        private int _statusCode = 200;
        private bool _disposed;

        internal HttpRequestScope(SystemMetrics metrics, string method, string path)
        {
            _metrics = metrics;
            _method = method;
            _path = path;
            _stopwatch = Stopwatch.StartNew();

            _metrics.IncrementHttpRequestsInProgress(method, path);
        }

        /// <summary>
        /// Sets the response status code for this request
        /// </summary>
        public void SetStatusCode(int statusCode)
        {
            _statusCode = statusCode;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            _metrics.DecrementHttpRequestsInProgress(_method, _path);
            _metrics.RecordHttpRequest(_method, _path, _statusCode, _stopwatch.Elapsed.TotalSeconds);
        }
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentProcess?.Dispose();
            _meter?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Extension methods for IServiceCollection to register SystemMetrics
/// </summary>
public static class SystemMetricsExtensions
{
    /// <summary>
    /// Adds SystemMetrics with proper meter for routing to System Prometheus
    /// </summary>
    public static IServiceCollection AddSystemMetrics(this IServiceCollection services, string serviceName)
    {
        services.AddSingleton(sp =>
        {
            var meter = new Meter("DataProcessing.System.Metrics", "1.0.0");
            return new SystemMetrics(meter, serviceName);
        });
        return services;
    }
}

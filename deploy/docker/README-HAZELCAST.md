# Hazelcast 5.6.0 - Docker Compose Development Guide

## Overview
Single-node Hazelcast deployment for local development using Docker Compose.

**Version:** 5.6.0 (latest stable, November 2025)  
**Client Library:** Hazelcast.Net 5.6.0 (NuGet)  
**Mode:** Single-node (development)  
**Memory:** 2GB heap, 4GB container limit  
**Access:** localhost:5701  

---

## Quick Start

### 1. Start Hazelcast
```bash
# Start only Hazelcast
docker-compose -f docker-compose.development.yml up -d hazelcast

# Or start all infrastructure
docker-compose -f docker-compose.development.yml up -d
```

### 2. Verify Running
```bash
# Check container status
docker ps | grep hazelcast

# View logs
docker logs ezplatform-hazelcast

# Should see: "Hazelcast 5.6.0 starting at [Member]..."
```

### 3. Test Health
```bash
# Health check endpoint
curl http://localhost:5701/hazelcast/health/ready

# Should return: 200 OK

# Node state
curl http://localhost:5701/hazelcast/health/node-state
```

### 4. View Metrics (Prometheus format)
```bash
curl http://localhost:5701/metrics
```

---

## Configuration

All configuration is done via environment variables in `docker-compose.development.yml`.

### Memory Settings
- **Heap Size:** 2GB (-Xms2g -Xmx2g)
- **Container Limit:** 4GB
- **Garbage Collector:** G1GC with 200ms max pause time

**To increase heap:**
```yaml
environment:
  - JAVA_OPTS=-Xms4g -Xmx4g -XX:+UseG1GC
```

Then update container limit:
```yaml
deploy:
  resources:
    limits:
      memory: 8G
```

### Network Mode
- **Single-node:** No clustering in Docker Compose
- **Discovery:** Disabled (not needed for single node)
- **Port:** 5701 (mapped to host)
- **Network:** ezplatform-network (shared with other services)

### Cache TTL
Configured in client applications (default: 1 hour)

---

## Usage in Services

### .NET Client Connection (NuGet: Hazelcast.Net v5.6.0)

**Install Package:**
```bash
dotnet add package Hazelcast.Net --version 5.6.0
```

**Program.cs Configuration:**
```csharp
using Hazelcast;
using Hazelcast.Core;

var builder = WebApplication.CreateBuilder(args);

// Add Hazelcast client
builder.Services.AddSingleton<IHazelcastClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    var options = new HazelcastOptionsBuilder()
        .With(args =>
        {
            // Connection
            args.Networking.Addresses.Add("hazelcast:5701");
            args.ClusterName = "data-processing-cluster";
            
            // Retry policy
            args.Networking.ConnectionRetry.ClusterConnectionTimeoutMilliseconds = 5000;
            args.Networking.ConnectionRetry.InitialBackoffMilliseconds = 1000;
            args.Networking.ConnectionRetry.MaxBackoffMilliseconds = 30000;
            
            // Logging
            args.LoggerFactory.Creator = () => new HazelcastLoggerFactory(logger);
        })
        .Build();
    
    return await HazelcastClientFactory.StartNewClientAsync(options);
});

var app = builder.Build();

// Verify connection on startup
var hazelcastClient = app.Services.GetRequiredService<IHazelcastClient>();
logger.LogInformation("Connected to Hazelcast cluster: {ClusterName}", 
    hazelcastClient.ClusterName);

app.Run();
```

**Service Usage:**
```csharp
public class FileProcessorService
{
    private readonly IHazelcastClient _hazelcast;
    
    public FileProcessorService(IHazelcastClient hazelcast)
    {
        _hazelcast = hazelcast;
    }
    
    // Store file content with TTL
    public async Task StoreFileContentAsync(string key, string content)
    {
        var cache = await _hazelcast.GetMapAsync<string, string>("file-content");
        await cache.SetAsync(key, content, TimeSpan.FromHours(1)); // 1 hour TTL
    }
    
    // Retrieve file content
    public async Task<string> GetFileContentAsync(string key)
    {
        var cache = await _hazelcast.GetMapAsync<string, string>("file-content");
        return await cache.GetAsync(key);
    }
    
    // Delete after processing
    public async Task DeleteFileContentAsync(string key)
    {
        var cache = await _hazelcast.GetMapAsync<string, string>("file-content");
        await cache.RemoveAsync(key);
    }
    
    // Check if key exists
    public async Task<bool> ExistsAsync(string key)
    {
        var cache = await _hazelcast.GetMapAsync<string, string>("file-content");
        return await cache.ContainsKeyAsync(key);
    }
}
```

---

## Monitoring

### Key Metrics (Prometheus)
```bash
curl http://localhost:5701/metrics | grep hz_
```

**Important Metrics:**
- `hz_member_count` - Cluster size (should be 1)
- `hz_map_total_count` - Total cached items
- `hz_map_owned_entry_count` - Entries in cache
- `hz_map_owned_entry_memory_cost` - Memory usage in bytes
- `hz_map_hits` - Cache hits
- `hz_map_misses` - Cache misses

### Logs
```bash
# Follow logs
docker logs -f ezplatform-hazelcast

# Last 100 lines
docker logs --tail 100 ezplatform-hazelcast

# Grep for errors
docker logs ezplatform-hazelcast 2>&1 | grep ERROR

# Grep for startup
docker logs ezplatform-hazelcast 2>&1 | grep "is STARTED"
```

### Container Stats
```bash
# Real-time stats
docker stats ezplatform-hazelcast

# Should show memory usage < 4GB
```

---

## Troubleshooting

### Container Won't Start

**Check logs:**
```bash
docker logs ezplatform-hazelcast
```

**Common issues:**

1. **Port conflict:** Port 5701 already in use
   ```bash
   netstat -an | findstr 5701
   # Solution: Stop conflicting process or change port in docker-compose.yml
   ```

2. **Insufficient memory:** Docker memory limit too low
   ```bash
   docker info | findstr Memory
   # Solution: Increase Docker Desktop memory allocation (Settings > Resources)
   ```

3. **Image pull error:** Cannot download image
   ```bash
   docker pull hazelcast/hazelcast:5.6.0
   # Check internet connection and Docker Hub access
   ```

### Health Check Failing

**Manual test:**
```bash
docker exec ezplatform-hazelcast curl -f http://localhost:5701/hazelcast/health/ready
```

**Check Hazelcast process:**
```bash
docker exec ezplatform-hazelcast ps aux | grep hazelcast
```

**Check if port is listening:**
```bash
docker exec ezplatform-hazelcast netstat -tln | grep 5701
```

### Cannot Connect from Services

**Test network connectivity:**
```bash
# From another container
docker exec <service-container> ping hazelcast

# Check network
docker network inspect ezplatform-network | findstr hazelcast
```

**Common issues:**
1. Services not on same network (must be ezplatform-network)
2. Using `localhost` instead of `hazelcast` hostname
3. Hazelcast not fully started (wait for health check to pass)
4. Firewall blocking port 5701

**Test from host:**
```bash
curl http://localhost:5701/hazelcast/health/ready
```

### Memory Issues

**Check memory usage:**
```bash
docker stats ezplatform-hazelcast --no-stream
```

**If memory > 4GB:**
- Increase container limit in docker-compose.yml
- Reduce heap size if needed
- Check for memory leaks in client applications
- Clear cache manually (see Maintenance section)

### Connection Timeout from Client

**Check Hazelcast is ready:**
```bash
docker logs ezplatform-hazelcast | findstr "is STARTED"
```

**Verify cluster name matches:**
```bash
# In Hazelcast logs
docker logs ezplatform-hazelcast | findstr "data-processing-cluster"

# In client code
args.ClusterName = "data-processing-cluster"; // Must match
```

---

## Maintenance

### View Cache Statistics

**Using curl (REST API):**
```bash
# Get cluster state
curl http://localhost:5701/hazelcast/rest/cluster

# Get map statistics
curl http://localhost:5701/hazelcast/rest/maps/file-content
```

### Clear Cache Manually

**Option 1: Restart container (cache is ephemeral)**
```bash
docker-compose -f docker-compose.development.yml restart hazelcast
```

**Option 2: Using REST API**
```bash
# Clear specific map
curl -X POST http://localhost:5701/hazelcast/rest/maps/file-content/clear
```

### Restart Hazelcast

```bash
# Graceful restart
docker-compose -f docker-compose.development.yml restart hazelcast

# Force recreate
docker-compose -f docker-compose.development.yml up -d --force-recreate hazelcast

# Stop and start
docker-compose -f docker-compose.development.yml stop hazelcast
docker-compose -f docker-compose.development.yml start hazelcast
```

### Stop and Remove

```bash
# Stop
docker-compose -f docker-compose.development.yml stop hazelcast

# Remove (data will be lost - cache is in-memory only)
docker-compose -f docker-compose.development.yml rm -f hazelcast
```

### View Hazelcast Logs with Timestamps

```bash
docker logs --timestamps ezplatform-hazelcast
```

---

## Performance Tips

### 1. Monitor Memory Usage
Keep heap usage below 80% to avoid GC pressure:
```bash
docker stats ezplatform-hazelcast
```

### 2. Use TTL for Auto-Cleanup
Always set TTL when storing data to prevent memory leaks:
```csharp
await cache.SetAsync(key, content, TimeSpan.FromHours(1));
```

### 3. Cleanup After Use
Delete cache entries when done processing:
```csharp
await cache.RemoveAsync(key);
```

### 4. Connection Pooling
Reuse HazelcastClient instance (singleton):
```csharp
builder.Services.AddSingleton<IHazelcastClient>(...);
```

### 5. Batch Operations
Use batch operations for better performance:
```csharp
var cache = await _hazelcast.GetMapAsync<string, string>("file-content");

// Batch put
await cache.PutAllAsync(new Dictionary<string, string>
{
    ["key1"] = "value1",
    ["key2"] = "value2"
});

// Batch get
var keys = new[] { "key1", "key2" };
var results = await cache.GetAllAsync(keys);
```

### 6. Monitor Cache Hit Ratio
```bash
# Check metrics for hit/miss ratio
curl http://localhost:5701/metrics | grep -E "hz_map_(hits|misses)"

# Aim for > 80% hit ratio
```

---

## Version Verification

### Server Version
```bash
docker logs ezplatform-hazelcast | findstr "Hazelcast"
# Should output: "Hazelcast 5.6.0 (20240101 - 1234567)"
```

### Client Version (in services)
```bash
# Check .csproj files
findstr /S "Hazelcast.Net" src\Services\*\*.csproj
# Should show: Version="5.6.0"
```

### Important: Version Consistency
⚠️ **Client and server versions MUST match major.minor version!**

If you upgrade Hazelcast server, update all services:
```bash
dotnet add package Hazelcast.Net --version 5.6.0
```

---

## Production Deployment

For production Kubernetes deployment, see **Task 28** documentation:
- Multi-node cluster (3 replicas)
- StatefulSet for stable network identities
- Persistent volumes (optional)
- Resource requests/limits
- RBAC permissions
- Load balancing

---

## Common Use Cases

### Use Case 1: File Content Caching
```csharp
// FileProcessorService stores large file content
var cacheKey = $"file:{dataSourceId}:{Guid.NewGuid()}";
await cache.SetAsync(cacheKey, fileContent, TimeSpan.FromHours(1));

// Pass only the key in message (not content)
await _publishEndpoint.Publish(new ValidationRequestEvent
{
    HazelcastKey = cacheKey,
    DataSourceId = dataSourceId
});
```

### Use Case 2: Validated Records Storage
```csharp
// ValidationService stores valid records
var validRecordsKey = $"valid:{dataSourceId}:{correlationId}";
await cache.SetAsync(validRecordsKey, validRecords, TimeSpan.FromHours(1));

// OutputService retrieves and processes
var validRecords = await cache.GetAsync(validRecordsKey);
// ... process records ...
await cache.RemoveAsync(validRecordsKey); // Cleanup
```

### Use Case 3: Temporary State Storage
```csharp
// Store processing state
var stateKey = $"state:{jobId}";
await cache.SetAsync(stateKey, jobState, TimeSpan.FromMinutes(30));

// Check state later
if (await cache.ContainsKeyAsync(stateKey))
{
    var state = await cache.GetAsync(stateKey);
    // Resume processing
}
```

---

## References

- [Hazelcast 5.6.0 Documentation](https://docs.hazelcast.com/hazelcast/5.6/)
- [Hazelcast.Net Client](https://github.com/hazelcast/hazelcast-csharp-client)
- [Docker Hub: Hazelcast](https://hub.docker.com/r/hazelcast/hazelcast)
- [Hazelcast REST API](https://docs.hazelcast.com/hazelcast/5.6/clients/rest)

---

## Support

**Issues:** Check logs first (`docker logs ezplatform-hazelcast`)  
**Questions:** See `docs/planning/FILE-PROCESSING-REFACTORING-PLAN-ORIGINAL.md`  
**Performance:** Monitor Prometheus metrics at `http://localhost:5701/metrics`  
**Version Guide:** See `deploy/docker/HAZELCAST-VERSION-GUIDE.md`

---

**Last Updated:** November 2025  
**Hazelcast Version:** 5.6.0  
**Status:** Development Ready ✅

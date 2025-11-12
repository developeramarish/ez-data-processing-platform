# Hazelcast Version Guide

## Version Strategy

**Principle:** Infrastructure and client library versions MUST match major.minor version.

### Current Versions (November 2025)

| Component | Version | Location |
|-----------|---------|----------|
| **Hazelcast Server** | 5.6.0 | docker-compose.development.yml |
| **Hazelcast.Net Client** | 5.6.0 | NuGet package in services |
| **Compatibility** | ✅ 5.6.x client ↔ 5.6.x server | Fully compatible |

---

## Services Using Hazelcast Client

### Task 18: FileProcessorService
**NuGet Package:** `Hazelcast.Net` version `5.6.0`

**Package Reference:**
```xml
<PackageReference Include="Hazelcast.Net" Version="5.6.0" />
```

**Usage:** Store file content in distributed cache
```csharp
var cache = await _hazelcast.GetMapAsync<string, string>("file-content");
await cache.SetAsync(cacheKey, jsonContent, TimeSpan.FromHours(1));
```

**Maps Used:**
- `file-content` - Stores original file content as JSON

---

### Task 19: ValidationService
**NuGet Package:** `Hazelcast.Net` version `5.6.0`

**Package Reference:**
```xml
<PackageReference Include="Hazelcast.Net" Version="5.6.0" />
```

**Usage:** Retrieve file content for validation, store valid records
```csharp
// Retrieve original content
var inputCache = await _hazelcast.GetMapAsync<string, string>("file-content");
var jsonContent = await inputCache.GetAsync(event.HazelcastKey);

// Validate records...

// Store valid records
var outputCache = await _hazelcast.GetMapAsync<string, List<JsonDocument>>("valid-records");
await outputCache.SetAsync(validRecordsKey, validRecords, TimeSpan.FromHours(1));

// Cleanup original
await inputCache.RemoveAsync(event.HazelcastKey);
```

**Maps Used:**
- `file-content` - Reads original file content (then deletes)
- `valid-records` - Stores validated records

---

### Task 20: OutputService
**NuGet Package:** `Hazelcast.Net` version `5.6.0`

**Package Reference:**
```xml
<PackageReference Include="Hazelcast.Net" Version="5.6.0" />
```

**Usage:** Retrieve valid records, cleanup cache
```csharp
// Retrieve valid records
var cache = await _hazelcast.GetMapAsync<string, List<JsonDocument>>("valid-records");
var validRecords = await cache.GetAsync(event.ValidRecordsKey);

// Reconstruct to original format and output...

// Cleanup after output
await cache.RemoveAsync(event.ValidRecordsKey);
```

**Maps Used:**
- `valid-records` - Reads validated records (then deletes)

---

## Version Compatibility Matrix

| Server Version | Client Version | Status | Notes |
|----------------|----------------|--------|-------|
| 5.6.0 | 5.6.0 | ✅ Perfect | Recommended - exact match |
| 5.6.0 | 5.6.x | ✅ Compatible | Patch version differences OK |
| 5.6.0 | 5.5.x | ✅ Compatible | One minor version back |
| 5.6.0 | 5.7.x | ✅ Compatible | One minor version forward (if released) |
| 5.6.0 | 5.4.x | ⚠️ May work | Not recommended - 2+ versions apart |
| 5.6.0 | 4.x.x | ❌ Incompatible | Major version mismatch |

**Recommendation:** Always use exact matching versions (5.6.0 ↔ 5.6.0)

---

## Installation Instructions

### For New Services

**Step 1: Add NuGet Package**
```bash
cd src/Services/YourService
dotnet add package Hazelcast.Net --version 5.6.0
```

**Step 2: Configure in Program.cs**
```csharp
using Hazelcast;
using Hazelcast.Core;

var builder = WebApplication.CreateBuilder(args);

// Add Hazelcast client (Singleton)
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

**Step 3: Inject and Use**
```csharp
public class YourService
{
    private readonly IHazelcastClient _hazelcast;
    
    public YourService(IHazelcastClient hazelcast)
    {
        _hazelcast = hazelcast;
    }
    
    public async Task YourMethodAsync()
    {
        var cache = await _hazelcast.GetMapAsync<string, string>("your-cache");
        await cache.SetAsync("key", "value", TimeSpan.FromHours(1));
    }
}
```

---

## Upgrade Path

When upgrading Hazelcast to a new version:

### Step 1: Check Compatibility
Visit [Hazelcast Release Notes](https://github.com/hazelcast/hazelcast/releases)

**Review:**
- Breaking changes
- New features
- Deprecations
- Migration guides

### Step 2: Update Docker Image

**File:** `docker-compose.development.yml`
```yaml
hazelcast:
  image: hazelcast/hazelcast:5.7.0  # New version
```

### Step 3: Update NuGet Packages

**For each service using Hazelcast:**
```bash
# FileProcessorService
cd src/Services/FileProcessorService
dotnet add package Hazelcast.Net --version 5.7.0

# ValidationService
cd ../ValidationService
dotnet add package Hazelcast.Net --version 5.7.0

# OutputService
cd ../OutputService
dotnet add package Hazelcast.Net --version 5.7.0
```

**Or update .csproj files directly:**
```xml
<PackageReference Include="Hazelcast.Net" Version="5.7.0" />
```

### Step 4: Update This Document

**Update version table:**
```markdown
| Component | Version | Location |
|-----------|---------|----------|
| **Hazelcast Server** | 5.7.0 | docker-compose.development.yml |
| **Hazelcast.Net Client** | 5.7.0 | NuGet package in services |
```

### Step 5: Test

```bash
# Restart infrastructure
docker-compose -f docker-compose.development.yml down
docker-compose -f docker-compose.development.yml up -d

# Verify version
docker logs ezplatform-hazelcast | findstr "Hazelcast 5.7.0"

# Build services
dotnet build

# Run integration tests
dotnet test tests/Integration/
```

### Step 6: Update Kubernetes (Task 28)

**File:** `deploy/kubernetes/hazelcast-deployment.yaml`
```yaml
containers:
- name: hazelcast
  image: hazelcast/hazelcast:5.7.0  # Match Docker version
```

---

## Troubleshooting Version Mismatches

### Symptom: "Protocol version mismatch"

**Error:**
```
Hazelcast.Core.HazelcastException: Client protocol version X does not support server protocol version Y
```

**Cause:** Client and server versions incompatible

**Solution:**
```bash
# Check server version
docker logs ezplatform-hazelcast | findstr "Hazelcast"

# Check client version in .csproj
findstr /S "Hazelcast.Net" src\Services\*\*.csproj

# Update client to match server
cd src/Services/YourService
dotnet add package Hazelcast.Net --version 5.6.0
```

---

### Symptom: "Unknown operation" or "Unsupported operation"

**Error:**
```
Hazelcast.Core.HazelcastException: Unknown operation: XXX
```

**Cause:** Client using features not available in server version

**Solution Option 1:** Update server to newer version
```yaml
# docker-compose.development.yml
hazelcast:
  image: hazelcast/hazelcast:5.7.0
```

**Solution Option 2:** Downgrade client
```bash
dotnet add package Hazelcast.Net --version 5.6.0
```

---

### Symptom: Connection timeouts

**Error:**
```
Hazelcast.Core.HazelcastException: Unable to connect to any cluster member
```

**Possible Causes:**
1. Version mismatch (check logs)
2. Cluster name mismatch
3. Network issues

**Troubleshooting:**
```bash
# Verify Hazelcast is running
docker ps | findstr hazelcast

# Check Hazelcast logs
docker logs ezplatform-hazelcast

# Verify cluster name
docker logs ezplatform-hazelcast | findstr "data-processing-cluster"

# Test network connectivity
docker exec ezplatform-mongodb ping hazelcast

# Test from host
curl http://localhost:5701/hazelcast/health/ready
```

---

## Kubernetes Deployment (Task 28)

### Version Consistency

**Kubernetes deployment MUST use same version as Docker Compose.**

**File:** `deploy/kubernetes/hazelcast-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: hazelcast
spec:
  replicas: 3  # Multi-node for production
  template:
    spec:
      containers:
      - name: hazelcast
        image: hazelcast/hazelcast:5.6.0  # ⬅️ Must match docker-compose.yml
        env:
        - name: JAVA_OPTS
          value: "-Xms4g -Xmx4g -XX:+UseG1GC"
        - name: HZ_CLUSTERNAME
          value: "data-processing-cluster"
        - name: HZ_NETWORK_JOIN_KUBERNETES_ENABLED
          value: "true"
        - name: HZ_NETWORK_JOIN_KUBERNETES_SERVICE_NAME
          value: "hazelcast"
```

**Client services in Kubernetes use same NuGet package version (5.6.0).**

---

## Version History

### 5.6.0 (Current - November 2025)
- ✅ Latest stable release
- ✅ Used in development (Docker Compose)
- ⏳ Production deployment pending (Task 28)

**Key Features:**
- Improved health endpoints
- Enhanced Prometheus metrics
- Better Kubernetes integration
- Performance improvements

**Changes from 5.5.x:**
- Health endpoint path changed to `/hazelcast/health/ready`
- New metrics format
- Improved connection retry logic

---

## Best Practices

### 1. Version Pinning
Always pin specific versions in package references:
```xml
<!-- ✅ Good - pinned version -->
<PackageReference Include="Hazelcast.Net" Version="5.6.0" />

<!-- ❌ Bad - floating version -->
<PackageReference Include="Hazelcast.Net" Version="5.*" />
```

### 2. Consistent Upgrades
Upgrade all components together:
1. Docker image
2. All service NuGet packages
3. Kubernetes deployment
4. Update this document

### 3. Testing After Upgrades
Run comprehensive tests:
```bash
# Unit tests
dotnet test tests/Unit/

# Integration tests
dotnet test tests/Integration/

# E2E tests
dotnet test tests/E2E/
```

### 4. Rollback Plan
Keep previous version documented for quick rollback:
```yaml
# docker-compose.development.yml
hazelcast:
  image: hazelcast/hazelcast:5.6.0  # Current
  # image: hazelcast/hazelcast:5.5.0  # Previous (for rollback)
```

### 5. Monitor After Upgrades
Watch for:
- Connection errors
- Protocol version errors
- Performance degradation
- Memory usage changes

---

## Version Verification Commands

### Check All Versions

**PowerShell script:**
```powershell
# Check Docker image version
Write-Host "Docker Image Version:" -ForegroundColor Cyan
docker logs ezplatform-hazelcast 2>&1 | Select-String "Hazelcast"

# Check NuGet versions in services
Write-Host "`nNuGet Package Versions:" -ForegroundColor Cyan
Get-ChildItem -Path "src\Services" -Filter "*.csproj" -Recurse | 
    ForEach-Object {
        $content = Get-Content $_.FullName
        $hazelcastLine = $content | Select-String "Hazelcast.Net"
        if ($hazelcastLine) {
            Write-Host "$($_.Directory.Name): $hazelcastLine"
        }
    }

# Check if versions match
Write-Host "`nVersion Check:" -ForegroundColor Cyan
$dockerVersion = (docker logs ezplatform-hazelcast 2>&1 | Select-String "Hazelcast \d+\.\d+\.\d+" | Select-Object -First 1).ToString() -replace '.*Hazelcast (\d+\.\d+\.\d+).*', '$1'
Write-Host "Docker: $dockerVersion"

$nugetVersions = @()
Get-ChildItem -Path "src\Services" -Filter "*.csproj" -Recurse | 
    ForEach-Object {
        $content = Get-Content $_.FullName
        $hazelcastLine = $content | Select-String 'Hazelcast\.Net.*Version="([^"]+)"'
        if ($hazelcastLine) {
            $version = $hazelcastLine.Matches.Groups[1].Value
            $nugetVersions += $version
        }
    }

$allMatch = ($nugetVersions | Select-Object -Unique).Count -eq 1 -and $nugetVersions[0] -eq $dockerVersion
if ($allMatch) {
    Write-Host "✅ All versions match: $dockerVersion" -ForegroundColor Green
} else {
    Write-Host "❌ Version mismatch detected!" -ForegroundColor Red
    Write-Host "NuGet versions: $($nugetVersions -join ', ')"
}
```

---

## References

### Official Documentation
- [Hazelcast 5.6.0 Release Notes](https://github.com/hazelcast/hazelcast/releases/tag/v5.6.0)
- [Hazelcast Documentation](https://docs.hazelcast.com/hazelcast/5.6/)
- [Hazelcast.Net Client](https://github.com/hazelcast/hazelcast-csharp-client)
- [NuGet Package](https://www.nuget.org/packages/Hazelcast.Net/5.6.0)

### Version Compatibility
- [Compatibility Guide](https://docs.hazelcast.com/hazelcast/5.6/clients/dotnet#compatibility)
- [Migration Guide](https://docs.hazelcast.com/hazelcast/5.6/migrate/migration-guide)

---

## Support

**Version Issues:** Check this guide first  
**Upgrade Help:** See "Upgrade Path" section above  
**Compatibility:** See "Version Compatibility Matrix"  
**Usage Guide:** See `deploy/docker/README-HAZELCAST.md`

---

**Last Updated:** November 2025  
**Current Version:** 5.6.0  
**Status:** Stable ✅

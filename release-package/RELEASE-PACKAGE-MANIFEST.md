# EZ Platform v0.1.1-rc1 - Release Package Manifest

**Release Date:** January 1, 2026
**Version:** 0.1.1-rc1
**Git Commit:** e99b71a

---

## Changes from v0.1.0-beta

### Backend Services - Swagger/OpenAPI
- Added interactive API documentation to all 8 backend services
- Swagger UI available at `/swagger` endpoint on each service
- OpenAPI 3.0 specification with full endpoint documentation

### Frontend Enhancements
- Added EZ Platform splash screen and logo (ez-platform-logo.svg)
- Fixed documentation loading (USER-GUIDE-HE.md) in frontend help page
- Updated nginx.conf routing for correct `/api/v1/` endpoint paths
- Enhanced application header with branding

### Configuration Fixes
- Added `database-name: "ezplatform"` to services-config ConfigMap
- Fixed MetricsConfigurationService health checks (increased probe timeouts)
- Changed imagePullPolicy to `Never` for offline deployment compatibility
- Updated probe timings for production stability

### Technical Improvements
- Enhanced frontend Docker build with explicit documentation copy
- Standardized error messages to Corvus.Json.Validator format
- Improved error message parsing in frontend error parser

### Updated Images (9 total)
- datasource-management:v0.1.1-rc1
- filediscovery:v0.1.1-rc1
- fileprocessor:v0.1.1-rc1
- validation:v0.1.1-rc1
- output:v0.1.1-rc1
- invalidrecords:v0.1.1-rc1
- scheduling:v0.1.1-rc1
- metrics-configuration:v0.1.1-rc1
- frontend:v0.1.1-rc1
- ezplatform-docs:v0.1.1-rc1

**Commits:** 17 commits since v0.1.0-beta
**Backward Compatible:** Yes (no breaking changes)

---

## Package Structure

```
ez-platform-v0.1.1-rc1/
├── helm/                           # Helm Chart (COMPLETE)
│   └── ez-platform/
│       ├── Chart.yaml              # Chart metadata
│       ├── values.yaml             # Configuration (300+ options)
│       ├── README.md               # Helm chart documentation
│       └── templates/              # Kubernetes templates
│           ├── _helpers.tpl        # Template functions
│           ├── namespace.yaml
│           ├── configmaps/         # 5 ConfigMaps
│           ├── deployments/        # 16 Deployments
│           ├── statefulsets/       # 3 StatefulSets
│           ├── services/           # All services
│           └── pvcs.yaml           # Persistent volumes
│
├── k8s/                            # Raw Kubernetes Manifests
│   ├── namespace.yaml
│   ├── configmaps/                 # Service configurations
│   ├── deployments/                # Microservice deployments
│   ├── infrastructure/             # MongoDB, Kafka, observability
│   ├── services/                   # Service definitions
│   └── ingress/                    # Ingress routing
│
├── docs/                           # Documentation Site (MkDocs)
│   ├── mkdocs.yml                  # Site configuration
│   ├── Dockerfile                  # Documentation server
│   └── docs/
│       ├── index.md                # Home page
│       ├── installation.md         # Standard installation
│       ├── installation/
│       │   └── helm-installation.md # Helm deployment guide
│       ├── admin.md                # Admin guide
│       ├── user-guide-he.md        # Hebrew user guide
│       ├── architecture/           # Architecture docs
│       ├── release-notes.md        # Release notes
│       └── changelog.md            # Version history
│
├── scripts/                        # Deployment Scripts
│   ├── start-port-forwards.ps1    # Port forwarding (18 ports)
│   └── (other utility scripts)
│
├── install.sh                      # Automated Helm installation
├── uninstall.sh                    # Clean uninstallation
└── README.md                       # Package overview

```

---

## Deployment Options

### Option 1: Helm Installation (Recommended)

**Quick Start:**
```bash
./install.sh
```

**Manual:**
```bash
helm install ez-platform ./helm/ez-platform \
  --namespace ez-platform \
  --create-namespace
```

**With Custom Configuration:**
```bash
helm install ez-platform ./helm/ez-platform \
  --namespace ez-platform \
  --create-namespace \
  --values custom-values.yaml
```

### Option 2: Direct Kubernetes Manifests

```bash
kubectl create namespace ez-platform
kubectl apply -f k8s/
```

### Option 3: Documentation Server First

```bash
cd docs/
docker build -t ez-docs .
docker run -p 8000:8000 ez-docs

# Open: http://localhost:8000
# Follow installation guide
```

---

## Package Contents Checklist

### ✅ Helm Chart
- [x] Chart.yaml - Chart metadata
- [x] values.yaml - Complete configuration (300+ options)
- [x] README.md - Helm chart documentation
- [x] templates/_helpers.tpl - Template functions (18 helpers)
- [x] templates/namespace.yaml
- [x] templates/configmaps/ - 5 ConfigMaps
- [x] templates/deployments/ - 16 Deployments
- [x] templates/statefulsets/ - 3 StatefulSets (MongoDB, Kafka, Hazelcast)
- [x] templates/services/ - All Kubernetes services
- [x] templates/pvcs.yaml - Persistent volume claims

**Total Helm Resources:** 50+ templated Kubernetes resources

### ✅ Kubernetes Manifests (Alternative to Helm)
- [x] namespace.yaml
- [x] configmaps/ - All service configurations
- [x] deployments/ - All microservice deployments
- [x] infrastructure/ - MongoDB, Kafka, Hazelcast, observability
- [x] services/ - Service definitions
- [x] ingress/ - Ingress routing rules

### ✅ Documentation
- [x] MkDocs site with Material theme
- [x] Installation Guide (English)
- [x] Helm Installation Guide (English)
- [x] Admin Guide (English)
- [x] User Guide (Hebrew - מדריך משתמש)
- [x] System Architecture (English + Hebrew)
- [x] Release Notes
- [x] Changelog
- [x] Dockerfile for docs server

### ✅ Installation Scripts
- [x] install.sh - Automated Helm installation with prerequisites check
- [x] uninstall.sh - Clean uninstallation with options
- [x] scripts/start-port-forwards.ps1 - Development port forwarding

### ✅ Deployment Components

**Microservices (9):**
- [x] FileDiscoveryService - File polling and discovery
- [x] FileProcessorService - Format conversion
- [x] ValidationService - Schema validation
- [x] OutputService - Multi-destination output
- [x] DataSourceManagementService - Primary API
- [x] MetricsConfigurationService - Metrics management
- [x] InvalidRecordsService - Invalid record handling
- [x] SchedulingService - Job scheduling
- [x] Frontend - React 19 UI

**Infrastructure (3):**
- [x] MongoDB - 3-node replica set
- [x] Kafka + Zookeeper - 3-node cluster
- [x] Hazelcast - 3-node distributed cache

**Observability (7):**
- [x] Prometheus System - System metrics
- [x] Prometheus Business - Business metrics
- [x] Grafana - Visualization dashboards
- [x] Jaeger - Distributed tracing
- [x] OTEL Collector - Telemetry aggregation
- [x] Elasticsearch - Log storage
- [x] Fluent Bit - Log collection

---

## Resource Summary

### Kubernetes Resources
- **Deployments:** 16
- **StatefulSets:** 3 (MongoDB, Kafka, Hazelcast)
- **Services:** 18+
- **ConfigMaps:** 6
- **PVCs:** Auto-created by StatefulSets
- **Total Pods:** ~35 (at default replica counts)

### Storage Requirements
- MongoDB: 60 GB (3 × 20 GB)
- Kafka: 30 GB (3 × 10 GB)
- Hazelcast: 15 GB (3 × 5 GB)
- Data Input: 50 GB
- Data Output: 100 GB
- Zookeeper: 5 GB
- **Total:** ~260 GB

### Compute Requirements
**Minimum:**
- CPU: 15 cores (requests)
- Memory: 40 GB (requests)

**Recommended:**
- CPU: 48 cores (limits)
- Memory: 96 GB (limits)

---

## Installation Verification

### Quick Health Check

```bash
# Check all pods running
kubectl get pods -n ez-platform

# Check services
kubectl get svc -n ez-platform

# Test frontend
curl http://localhost:3000  # After port-forward

# Test API
curl http://localhost:5001/health
```

### Full Verification

```bash
# All services health
for port in 5001 5002 5003 5004 5006 5007 5008 5009; do
  echo "Testing port $port..."
  curl -s http://localhost:$port/health
done

# Check Kafka
kubectl exec -it kafka-0 -n ez-platform -- \
  kafka-topics.sh --bootstrap-server localhost:9092 --list

# Check MongoDB
kubectl exec -it mongodb-0 -n ez-platform -- \
  mongosh --eval "rs.status()"

# Check Hazelcast
kubectl exec -it hazelcast-0 -n ez-platform -- \
  curl http://localhost:5701/hazelcast/health
```

---

## Documentation Access

### View Documentation Locally

```bash
# Build and run docs container
cd docs/
docker build -t ez-docs .
docker run -p 8000:8000 ez-docs

# Open: http://localhost:8000
```

### Documentation Files
- `docs/installation/INSTALLATION-GUIDE.md` - Standard installation
- `docs/installation/HELM-INSTALLATION.md` - Helm deployment
- `docs/admin/ADMIN-GUIDE.md` - System administration
- `docs/user-guide/USER-GUIDE-HE.md` - Hebrew user guide
- `docs/architecture/SYSTEM-ARCHITECTURE.md` - Architecture
- `docs/releases/RELEASE-NOTES-v0.1.0-beta.md` - Release notes

---

## Known Limitations (Beta)

⚠️ **This is a BETA release for testing and demonstration:**

1. **E2E Test Gaps:** Some scenarios not fully tested
   - Multiple file formats (XML, Excel)
   - High load testing (10,000+ records)
   - Multi-destination scaling (4+ destinations)

2. **Production Hardening Needed:**
   - Security: Enable Elasticsearch x-pack security
   - Secrets: Use K8s Secrets for all credentials
   - TLS: Enable SSL/TLS for all services

3. **Documentation:** Some sections marked "Coming soon"

See: `docs/testing/E2E-GAP-ANALYSIS-REPORT.md` for complete gap analysis

---

## Support & Feedback

- **Issues:** GitHub Issues (link in README.md)
- **Documentation:** Full docs included in `docs/` folder
- **Helm Chart:** See `helm/ez-platform/README.md`

---

## Version Information

| Component | Version |
|-----------|---------|
| Helm Chart | 1.0.0 |
| Application | 0.1.0-beta |
| .NET Runtime | 10.0 |
| React | 19 |
| MongoDB | 8.0 |
| Kafka | 7.5.0 |
| Hazelcast | 5.6 |

---

## Checksums

Run verification:
```bash
# Count Helm templates
find helm/ez-platform/templates -type f | wc -l
# Expected: 29+

# Validate Helm chart
helm lint helm/ez-platform/

# Test template rendering
helm template ez-platform helm/ez-platform/ > test-manifests.yaml
kubectl apply --dry-run=client -f test-manifests.yaml
```

---

**Package Status:** ✅ Complete and Ready for Distribution
**Last Updated:** December 30, 2025
**Git Tag:** v0.1.0-beta
**Commit:** d7e18f8

🤖 Generated with Claude Code
Co-Authored-By: Claude Sonnet 4.5 (1M context) <noreply@anthropic.com>

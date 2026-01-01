# EZ Platform v0.1.1-rc1

**Data Processing Platform for Enterprise File Management**

[![Release](https://img.shields.io/badge/release-v0.1.1--rc1-blue)](https://github.com/usercourses63/ez-data-processing-platform/releases/tag/v0.1.1-rc1)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-blue)](https://react.dev/)

---

## Overview

EZ Platform: Microservices-based data processing with automated file discovery, format conversion, schema validation, and multi-destination output.

**Key Features:**
- 📁 Multi-source file discovery (Local, SFTP, FTP, HTTP, Kafka)
- 🔄 Format conversion (CSV, JSON, XML, Excel)
- ✅ JSON Schema validation
- 📤 Multi-destination output
- 🌐 Hebrew/RTL UI
- 📊 Business metrics & monitoring
- 📝 Swagger/OpenAPI documentation (NEW in v0.1.1)

---

## Quick Start

### Option 1: Helm Installation (Recommended)
```bash
# Install using Helm
helm install ez-platform ./helm/ez-platform \
  --namespace ez-platform \
  --create-namespace

# Wait for deployment
kubectl wait --for=condition=ready pod --all -n ez-platform --timeout=10m

# Access via port-forward
kubectl port-forward svc/frontend 3000:80 -n ez-platform
# Open: http://localhost:3000
```

### Option 2: Direct Kubernetes Manifests
```bash
# Deploy with kubectl
kubectl create namespace ez-platform
kubectl apply -f k8s/

# Access (get node IP first)
kubectl get nodes -o wide
# Open: http://<NODE-IP>:30080
```

**📖 Full Guide:** [Installation Guide](docs/installation/INSTALLATION-GUIDE.md) | [Helm Chart README](helm/ez-platform/README.md)

---

## Network Access

### Production (Internal LAN)
```
Frontend: http://<K8S-NODE-IP>:30080
Example: http://192.168.1.50:30080
```

### Development (localhost)
```bash
scripts/start-port-forwards.ps1
# Access: http://localhost:3000
```

---

## Documentation

- **[Installation Guide](docs/installation/INSTALLATION-GUIDE.md)** - Complete deployment
- **[Admin Guide](docs/admin/ADMIN-GUIDE.md)** - System administration
- **[User Guide (Hebrew)](docs/user-guide/USER-GUIDE-HE.md)** - מדריך משתמש
- **[Release Notes](release-package/docs/docs/release-notes.md)** - What's new
- **[Deployment Plan v0.1.1](release-package/Deployment Plan v0.1.1-rc1.md)** - Offline deployment guide
- **[CHANGELOG.md](CHANGELOG.md)** - Version history

---

## Architecture

**9 Microservices** (.NET 10) + React Frontend
- DataSourceManagement, FileDiscovery, FileProcessor
- Validation, Output, InvalidRecords
- Scheduling, MetricsConfiguration
- Frontend (React 19 + TypeScript)

**Infrastructure:**
MongoDB, RabbitMQ, Kafka, Hazelcast, Elasticsearch, Prometheus, Grafana, Jaeger

---

## Default Credentials

| Service | User | Password |
|---------|------|----------|
| Grafana | admin | EZPlatform2025!Beta |
| RabbitMQ | guest | guest |

---

**Version:** v0.1.1-rc1
**Release:** January 1, 2026
**Status:** Release Candidate - Production Readiness Update
**Download:** [GitHub Releases](https://github.com/usercourses63/ez-data-processing-platform/releases/tag/v0.1.1-rc1)

**What's New in v0.1.1-rc1:**
- ✅ Swagger/OpenAPI documentation on all backend services
- ✅ Frontend branding with EZ Platform logo and splash screen
- ✅ Fixed MetricsConfigurationService health checks
- ✅ Updated nginx routing for correct API v1 endpoints
- ✅ Added database-name to ConfigMap for consistency

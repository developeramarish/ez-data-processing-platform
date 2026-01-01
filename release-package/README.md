# EZ Platform v0.1.1-rc1 - Installation Package

**Complete Offline Installation Package**

---

## Package Contents

This package contains everything needed for offline deployment:

- ✅ **21 Docker Images** (4.1GB)
  - 10 application services
  - 11 infrastructure services
  - See `IMAGE-MANIFEST.txt` for complete list

- ✅ **Kubernetes Manifests** (33 files)
  - All deployments, services, configmaps
  - Infrastructure configurations
  - Ready to deploy

- ✅ **Helm Chart** (optional)
  - Alternative deployment method
  - Located in `helm/ez-platform/`

- ✅ **Documentation**
  - MkDocs site (as Docker image)
  - All guides included in `docs/`

- ✅ **Installation Scripts**
  - Automated installation
  - Image loading script

---

## Quick Installation

### Prerequisites

- Kubernetes cluster (v1.25+)
- kubectl configured
- Docker installed (for loading images)
- 16GB RAM, 50GB storage

### Install Steps

```bash
# 1. Extract package
tar -xzf ezplatform-v0.1.1-rc1.tar.gz
cd ezplatform-v0.1.1-rc1

# 2. Load Docker images (5-10 minutes)
chmod +x scripts/*.sh
./scripts/load-images.sh

# 3. Deploy to Kubernetes (5 minutes)
./scripts/install.sh

# 4. Get node IP and access
kubectl get nodes -o wide
# Access: http://<NODE-IP>:30080
```

---

## Manual Installation

### 1. Load Images

```bash
cd images/
for img in *.tar; do
    docker load -i "$img"
done
```

### 2. Deploy to Kubernetes

```bash
# Create namespace
kubectl create namespace ez-platform

# Deploy infrastructure
kubectl apply -f k8s/infrastructure/

# Wait for infrastructure
kubectl wait --for=condition=ready pod -l app=mongodb -n ez-platform --timeout=300s

# Deploy services
kubectl apply -f k8s/configmaps/
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/services/
```

### 3. Verify Deployment

```bash
kubectl get pods -n ez-platform
# All pods should be Running
```

---

## Access

**Frontend (NodePort):**
```bash
# Get node IP
kubectl get nodes -o wide

# Access
http://<NODE-IP>:30080
```

**Documentation Site (Optional):**
```bash
# Deploy docs
kubectl run ezplatform-docs \
  --image=ezplatform-docs:v0.1.1-rc1 \
  --port=80 \
  -n ez-platform

kubectl expose pod ezplatform-docs \
  --type=NodePort \
  --port=80 \
  --target-port=80 \
  -n ez-platform

# Access
http://<NODE-IP>:<DOCS-NODEPORT>
```

---

## Default Credentials

| Service | Username | Password |
|---------|----------|----------|
| Grafana | admin | EZPlatform2025!Beta |
| RabbitMQ Management | guest | guest |
| MongoDB | - | No auth (dev mode) |

---

## Documentation

Included documentation:
- **Installation Guide:** `docs/docs/installation.md`
- **Admin Guide:** `docs/docs/admin.md`
- **User Guide (Hebrew):** `docs/docs/user-guide-he.md`
- **Release Notes:** `docs/docs/release-notes.md`

View documentation:
```bash
cd docs
mkdocs serve
# Or deploy ezplatform-docs container
```

---

## Package Structure

```
ezplatform-v0.1.1-rc1/
├── images/              # 21 Docker images (.tar files, 4.1GB)
├── k8s/                 # Kubernetes manifests
│   ├── deployments/     # Service deployments
│   ├── services/        # Service definitions
│   ├── infrastructure/  # MongoDB, Kafka, etc.
│   ├── configmaps/      # Configuration
│   └── namespace.yaml   # Namespace definition
├── helm/                # Helm chart (optional)
│   └── ez-platform/
├── docs/                # MkDocs documentation site
│   ├── Dockerfile       # Docs site Docker image
│   ├── mkdocs.yml       # MkDocs configuration
│   └── docs/            # Documentation files
├── scripts/             # Installation scripts
│   ├── load-images.sh   # Load all Docker images
│   └── install.sh       # Complete installation
├── IMAGE-MANIFEST.txt   # List of all images
└── README.md            # This file
```

---

## Troubleshooting

**Issue: Pods not starting**
```bash
kubectl get events -n ez-platform --sort-by='.lastTimestamp'
kubectl describe pod <pod-name> -n ez-platform
```

**Issue: Images not loading**
```bash
# Verify Docker can see images
docker images | grep v0.1.1-rc1

# Verify minikube can see images (if using minikube)
minikube image ls | grep v0.1.1-rc1
```

---

## Support

- **Installation Guide:** docs/docs/installation.md
- **Admin Guide:** docs/docs/admin.md
- **GitHub:** https://github.com/usercourses63/ez-data-processing-platform

---

**Version:** v0.1.1-rc1
**Release Date:** January 1, 2026
**Package Size:** 4.1GB (images) + manifests + docs

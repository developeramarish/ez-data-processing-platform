# EZ Platform - Kubernetes Deployment

**Version:** 1.0
**Date:** December 2, 2025
**Status:** 🔄 Week 2 In Progress
**Completion:** Days 1-4 Complete (Infrastructure Ready)

---

## Overview

Complete Kubernetes deployment configuration for the EZ Data Processing Platform with 9 microservices and full infrastructure stack.

---

## Architecture

### Microservices (9 total, 23 replicas)

| Service | Replicas | Port | Purpose |
|---------|----------|------|---------|
| FileDiscoveryService | 2 | 5007 | File discovery & polling |
| FileProcessorService | 5 | 5008 | Format conversion & caching |
| ValidationService | 3 | 5003 | Schema validation |
| OutputService | 3 | 5009 | Multi-destination output |
| DataSourceManagement | 2 | 5001 | API & CRUD operations |
| MetricsConfiguration | 2 | 5002 | Metrics management |
| InvalidRecordsService | 2 | 5006 | Invalid record handling |
| SchedulingService | 1 | 5004 | Job scheduling |
| Frontend | 2 | 3000 | React UI |

### Infrastructure

| Component | Type | Replicas | Storage | Purpose |
|-----------|------|----------|---------|---------|
| MongoDB | StatefulSet | 3 | 20GB each | Primary datastore |
| Kafka | StatefulSet | 3 | 10GB each | Message queue |
| ZooKeeper | StatefulSet | 1 | 5GB | Kafka coordination |
| Hazelcast | StatefulSet | 3 | 8GB RAM, 5GB disk | Distributed cache |

---

## Quick Deployment

### Prerequisites

```bash
# Kubernetes cluster (Minikube, K3s, or cloud)
minikube start --cpus=4 --memory=16384 --disk-size=100g

# Or K3s
curl -sfL https://get.k3s.io | sh -
```

### Deploy Infrastructure First

```bash
# Create namespace
kubectl apply -f namespace.yaml

# Deploy infrastructure
kubectl apply -f infrastructure/mongodb-statefulset.yaml
kubectl apply -f infrastructure/kafka-statefulset.yaml
kubectl apply -f infrastructure/hazelcast-statefulset.yaml

# Wait for infrastructure to be ready
kubectl wait --for=condition=ready pod -l app=mongodb -n ez-platform --timeout=300s
kubectl wait --for=condition=ready pod -l app=kafka -n ez-platform --timeout=300s
kubectl wait --for=condition=ready pod -l app=hazelcast -n ez-platform --timeout=300s
```

### Deploy ConfigMaps and PVCs

```bash
kubectl apply -f configmaps/services-config.yaml
kubectl apply -f deployments/data-pvcs.yaml
```

### Deploy Services

```bash
# Deploy all 9 microservices
kubectl apply -f deployments/filediscovery-deployment.yaml
kubectl apply -f deployments/fileprocessor-deployment.yaml
kubectl apply -f deployments/validation-deployment.yaml
kubectl apply -f deployments/output-deployment.yaml
kubectl apply -f deployments/datasource-management-deployment.yaml
kubectl apply -f deployments/metrics-configuration-deployment.yaml
kubectl apply -f deployments/invalidrecords-deployment.yaml
kubectl apply -f deployments/scheduling-deployment.yaml
kubectl apply -f deployments/frontend-deployment.yaml

# Deploy service definitions
kubectl apply -f services/all-services.yaml
```

### Verify Deployment

```bash
# Check all pods
kubectl get pods -n ez-platform

# Check services
kubectl get svc -n ez-platform

# Check StatefulSets
kubectl get statefulsets -n ez-platform

# Check health
kubectl get pods -n ez-platform -o wide

# View logs
kubectl logs -f deployment/filediscovery -n ez-platform
```

---

## Folder Structure

```
k8s/
├── README.md                           # This file
├── namespace.yaml                      # Namespace definition
├── deployments/
│   ├── filediscovery-deployment.yaml   # FileDiscovery (2 replicas)
│   ├── fileprocessor-deployment.yaml   # FileProcessor (5 replicas)
│   ├── validation-deployment.yaml      # Validation (3 replicas)
│   ├── output-deployment.yaml          # Output (3 replicas)
│   ├── datasource-management-deployment.yaml  # API (2 replicas)
│   ├── metrics-configuration-deployment.yaml  # Metrics (2 replicas)
│   ├── invalidrecords-deployment.yaml  # InvalidRecords (2 replicas)
│   ├── scheduling-deployment.yaml      # Scheduling (1 replica)
│   ├── frontend-deployment.yaml        # Frontend (2 replicas)
│   └── data-pvcs.yaml                  # Persistent volume claims
├── services/
│   └── all-services.yaml               # All 9 service definitions
├── infrastructure/
│   ├── mongodb-statefulset.yaml        # MongoDB (3-node replica set)
│   ├── kafka-statefulset.yaml          # Kafka + ZooKeeper
│   └── hazelcast-statefulset.yaml      # Hazelcast (3-node cluster)
└── configmaps/
    └── services-config.yaml            # Central configuration
```

---

## Resource Requirements

### Total Cluster Requirements

**CPU:**
- Requests: ~15 cores
- Limits: ~40 cores

**Memory:**
- Requests: ~30GB
- Limits: ~80GB

**Storage:**
- MongoDB: 60GB (3 x 20GB)
- Kafka: 30GB (3 x 10GB)
- Hazelcast: 15GB (3 x 5GB)
- Data volumes: 150GB
- **Total: ~255GB**

### Recommended Cluster

- **Nodes:** 3 worker nodes
- **CPU per node:** 16 cores
- **RAM per node:** 32GB
- **Storage per node:** 100GB

---

## Health Checks

All services include:
- **Liveness Probe:** HTTP GET /health (restart if fails)
- **Readiness Probe:** HTTP GET /health (remove from service if fails)

---

## Environment Variables

Configured via ConfigMap (`services-config`):
- MongoDB connection string
- Kafka broker addresses
- Hazelcast cluster addresses
- Prometheus endpoints
- OpenTelemetry collector

---

## Deployment Order

**Critical:** Follow this order:

1. Namespace
2. ConfigMaps
3. PVCs
4. Infrastructure (MongoDB, Kafka, Hazelcast) - Wait for ready
5. Microservices
6. Services

---

## Accessing Services

### Frontend UI
```bash
# Get LoadBalancer IP
kubectl get svc frontend -n ez-platform

# Access
http://<EXTERNAL-IP>
```

### API Services (Internal)
```bash
# Port forward for testing
kubectl port-forward svc/datasource-management 5001:5001 -n ez-platform
```

---

## Troubleshooting

### Pods not starting
```bash
kubectl describe pod <pod-name> -n ez-platform
kubectl logs <pod-name> -n ez-platform
```

### Services not communicating
```bash
kubectl exec -it <pod-name> -n ez-platform -- curl http://service-name:port/health
```

### Storage issues
```bash
kubectl get pvc -n ez-platform
kubectl describe pvc <pvc-name> -n ez-platform
```

---

## Week 2 Status

**Days 1-4: Infrastructure Complete** ✅
- All 9 service deployments created
- All service definitions created
- All 3 infrastructure StatefulSets created
- ConfigMaps and PVCs created

**Days 5-6: Helm Chart** 📋 Next
**Day 7: Deploy & Test** 📋 Next

---

**Status:** ✅ Ready for Deployment Testing
**Last Updated:** December 2, 2025

# EZ Platform Helm Chart

**Version:** 1.0.0
**App Version:** 1.0.0
**Date:** December 2, 2025

---

## Overview

Helm chart for deploying the complete EZ Data Processing Platform with 9 microservices and full infrastructure stack (MongoDB, Kafka, Hazelcast).

---

## Quick Start

### Prerequisites

- Kubernetes cluster (1.28+)
- Helm 3.12+
- kubectl configured
- 3 nodes with 16 cores, 32GB RAM each (recommended)
- 255GB total storage available

### Install

```bash
# Add namespace
kubectl create namespace ez-platform

# Install chart
helm install ez-platform ./helm/ez-platform -n ez-platform

# Wait for deployment
kubectl wait --for=condition=ready pod --all -n ez-platform --timeout=10m
```

### Verify

```bash
# Check all pods
kubectl get pods -n ez-platform

# Check services
kubectl get svc -n ez-platform

# Get Frontend URL
kubectl get svc frontend -n ez-platform
```

### Uninstall

```bash
helm uninstall ez-platform -n ez-platform
kubectl delete namespace ez-platform
```

---

## Configuration

### values.yaml Structure

```yaml
services:          # All 9 microservices (replicas, resources, images)
mongodb:           # MongoDB 3-node replica set
kafka:             # Kafka 3-node cluster + ZooKeeper
hazelcast:         # Hazelcast 3-node distributed cache
storage:           # PVC configurations
config:            # Environment and logging
```

### Customize Deployment

```bash
# Use custom values
helm install ez-platform ./helm/ez-platform \
  -n ez-platform \
  -f custom-values.yaml

# Override specific values
helm install ez-platform ./helm/ez-platform \
  -n ez-platform \
  --set services.fileprocessor.replicas=10 \
  --set mongodb.storage=50Gi
```

---

## Architecture

**Deployed Components:**
- 9 Microservices (23 pod replicas)
- MongoDB (3-node replica set)
- Kafka (3-node cluster)
- Hazelcast (3-node cache)
- ZooKeeper (1 node)

**Total:** ~33 pods, 255GB storage, 15 CPU cores (requests)

---

## Resource Requirements

**Minimum:**
- 3 nodes x 8 cores x 16GB RAM = 24 cores, 48GB RAM

**Recommended:**
- 3 nodes x 16 cores x 32GB RAM = 48 cores, 96GB RAM

**Storage:**
- 255GB total (MongoDB: 60GB, Kafka: 30GB, Hazelcast: 15GB, Data: 150GB)

---

## Week 2 Status

**Days 1-4:** ✅ Infrastructure Complete
**Days 5-6:** ✅ Helm Chart Created
**Day 7:** 📋 Deploy & Test (next)

---

**Chart Status:** ✅ Ready for Deployment
**Last Updated:** December 2, 2025

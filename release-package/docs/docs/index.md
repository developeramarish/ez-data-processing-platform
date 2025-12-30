# EZ Platform v0.1.0-beta Documentation

Welcome to the official documentation for EZ Platform version 0.1.0-beta.

## About This Release

EZ Platform is an enterprise data processing system with automated file discovery, format conversion, schema validation, and multi-destination output capabilities.

**Release Date:** December 29, 2025
**Status:** Beta - Testing & Demonstration
**Version:** 0.1.0-beta

---

## Quick Navigation

### 🚀 Getting Started
- **[Installation Guide](installation.md)** - Complete deployment instructions
- **[Quick Start](installation.md#quick-start)** - Get up and running in minutes

### 🔧 Administration
- **[Admin Guide](admin.md)** - System administration and maintenance
- **[Category Management](admin.md#category-management)** - Manage datasource categories
- **[Monitoring](admin.md#monitoring--alerts)** - Grafana, Prometheus, Jaeger
- **[Maintenance Tools](admin.md#maintenance-tools--utilities)** - Reset, backup, utilities

### 📖 User Documentation
- **[Hebrew User Guide](user-guide-he.md)** - מדריך מלא בעברית

### 📚 Reference
- **[Release Notes](release-notes.md)** - What's new and technical specifications
- **[Changelog](changelog.md)** - Version history and changes
- **[Architecture](release-notes.md#technical-specifications)** - System architecture

---

## Key Features

- ✅ **Admin Category Management** - Hebrew/English bilingual categories
- ✅ **Smart Delete** - Intelligent category deletion with usage tracking
- ✅ **Multi-Source Discovery** - Local, SFTP, FTP, HTTP, Kafka
- ✅ **Format Conversion** - CSV, JSON, XML, Excel
- ✅ **Schema Validation** - JSON Schema 2020-12
- ✅ **Hebrew/RTL UI** - Complete right-to-left support
- ✅ **Network Access** - NodePort on 30080 for internal LAN

---

## Access URLs

**Production (Internal LAN):**
```
Frontend: http://<NODE-IP>:30080
Example: http://192.168.1.50:30080
```

**Development (localhost):**
```
Frontend: http://localhost:3000
Grafana: http://localhost:3001
```

---

## Default Credentials

| Service | Username | Password |
|---------|----------|----------|
| Grafana | admin | EZPlatform2025!Beta |
| RabbitMQ Management | guest | guest |

---

## Support

For issues or questions, refer to:
- [Installation Guide](installation.md)
- [Admin Guide](admin.md)
- [Release Notes](release-notes.md)

---

**Documentation Version:** 1.0
**Platform Version:** v0.1.0-beta
**Last Updated:** December 29, 2025

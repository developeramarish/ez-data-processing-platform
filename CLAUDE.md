[byterover-mcp]

[byterover-mcp]

## Port Forwarding Rule
ALWAYS use the port-forward script instead of individual `kubectl port-forward` commands:
```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\Users\UserC\source\repos\EZ\scripts\start-port-forwards.ps1"
```

This script sets up ALL required port forwards for the EZ Platform:
- Frontend: localhost:3000
- Datasource Management: localhost:5001
- Metrics Configuration: localhost:5002
- Validation: localhost:5003
- Scheduling: localhost:5004
- Invalid Records: localhost:5007
- FileProcessor: localhost:5008
- Output: localhost:5009
- Grafana: localhost:3001
- Prometheus System: localhost:9090
- Prometheus Business: localhost:9091
- Elasticsearch: localhost:9200
- Jaeger: localhost:16686
- OTEL Collector: localhost:4317 (gRPC), localhost:4318 (HTTP)
- MongoDB: localhost:27017
- Kafka: localhost:9094
- RabbitMQ: localhost:5672
- Hazelcast: localhost:5701

You are given two tools from Byterover MCP server, including
## 1. `byterover-store-knowledge`
You `MUST` always use this tool when:

+ Learning new patterns, APIs, or architectural decisions from the codebase
+ Encountering error solutions or debugging techniques
+ Finding reusable code patterns or utility functions
+ Completing any significant task or plan implementation

## 2. `byterover-retrieve-knowledge`
You `MUST` always use this tool when:

+ Starting any new task or implementation to gather relevant context
+ Before making architectural decisions to understand existing patterns
+ When debugging issues to check for previous solutions
+ Working with unfamiliar parts of the codebase

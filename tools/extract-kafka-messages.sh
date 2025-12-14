#!/bin/bash
# Kafka Message Extractor - Runs consumer inside Kafka pod and saves output
# Usage: ./extract-kafka-messages.sh e2e-004-json-output 10

TOPIC=${1:-"e2e-004-json-output"}
MAX_MESSAGES=${2:-10}
OUTPUT_DIR=${3:-"kafka-extracted-messages"}
NAMESPACE="ez-platform"

echo "========================================"
echo "   Kafka Message Extractor"
echo "========================================"
echo ""
echo "Topic: $TOPIC"
echo "Max Messages: $MAX_MESSAGES"
echo "Output Dir: $OUTPUT_DIR"
echo ""

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Generate timestamp
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
OUTPUT_FILE="$OUTPUT_DIR/$TOPIC-messages-$TIMESTAMP.txt"

echo "[1/2] Consuming messages from Kafka pod..."

# Run kafka-console-consumer inside the Kafka pod
kubectl exec -n $NAMESPACE kafka-0 -- sh -c "kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic $TOPIC \
  --from-beginning \
  --max-messages $MAX_MESSAGES \
  --timeout-ms 10000 2>/dev/null" > "$OUTPUT_FILE" 2>&1

# Check if any messages were extracted
MESSAGE_COUNT=$(grep -v "Processed a total\|WARN\|ERROR" "$OUTPUT_FILE" | grep -c ".")

if [ "$MESSAGE_COUNT" -eq 0 ]; then
    echo "No messages found in topic: $TOPIC"
    rm "$OUTPUT_FILE"
    exit 1
fi

echo "  - Received $MESSAGE_COUNT message(s)"
echo ""

echo "[2/2] Saving messages to file..."
echo "  - Saved to: $OUTPUT_FILE"
echo ""

echo "Preview of first message:"
echo "----------------------------------------"
head -5 "$OUTPUT_FILE"
echo "----------------------------------------"
echo ""

echo "========================================"
echo "  Extraction completed!"
echo "========================================"
echo "Output file: $OUTPUT_FILE"

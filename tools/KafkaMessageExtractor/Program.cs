using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("   📨 Kafka Message Extractor for E2E Testing   ");
Console.WriteLine("═══════════════════════════════════════════════\n");

// Parse arguments
var topic = GetArgument(args, "--topic") ?? "e2e-004-json-output";
var outputPath = GetArgument(args, "--output") ?? "kafka-extracted-messages";
var maxMessages = int.Parse(GetArgument(args, "--max") ?? "100");
var kafkaPod = GetArgument(args, "--kafka-pod") ?? "kafka-0";
var namespace_ = GetArgument(args, "--namespace") ?? "ez-platform";

Console.WriteLine($"Topic: {topic}");
Console.WriteLine($"Kafka Pod: {kafkaPod}");
Console.WriteLine($"Namespace: {namespace_}");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Max Messages: {maxMessages}\n");

try
{
    Console.WriteLine($"[1/3] 📥 Consuming messages from Kafka pod (via kubectl exec)...");

    // Use kubectl exec to run kafka-console-consumer inside the Kafka pod
    // This avoids DNS resolution issues with internal k8s service names
    var kubectlArgs = $"exec -n {namespace_} {kafkaPod} -- sh -c \"kafka-console-consumer --bootstrap-server localhost:9092 --topic {topic} --partition 0 --offset 0 --max-messages {maxMessages} --timeout-ms 10000 2>/dev/null\"";

    var processInfo = new ProcessStartInfo
    {
        FileName = "kubectl",
        Arguments = kubectlArgs,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    var process = Process.Start(processInfo);
    if (process == null)
    {
        Console.WriteLine("❌ Failed to start kubectl process");
        return;
    }

    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
    {
        Console.WriteLine($"⚠️  kubectl error: {error}");
    }

    // Parse messages from output (each line or block is a message)
    var messageLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Where(line => !line.Contains("Processed a total") && !line.Trim().StartsWith("%"))
        .ToList();

    if (messageLines.Count == 0)
    {
        Console.WriteLine("⚠️  No messages found in topic.");
        Console.WriteLine("\nPossible reasons:");
        Console.WriteLine("  - Topic is empty");
        Console.WriteLine("  - Messages were already consumed");
        Console.WriteLine("  - Topic doesn't exist\n");
        return;
    }

    Console.WriteLine($"  ✓ Received {messageLines.Count} message(s)\n");

    Console.WriteLine($"[2/3] 📊 Parsing messages...");
    var messages = new List<object>();

    // Try to parse as JSON array (common for batch messages)
    foreach (var line in messageLines)
    {
        var parsed = TryParseJson(line);
        if (parsed != null)
        {
            messages.Add(parsed);
            Console.WriteLine($"  ✓ Parsed message {messages.Count}");
        }
        else
        {
            messages.Add(line); // Keep as raw string if not JSON
            Console.WriteLine($"  ✓ Message {messages.Count} (raw text)");
        }
    }

    Console.WriteLine($"\n[3/3] 💾 Saving {messages.Count} message(s) to {outputPath}/...");

    // Create output directory
    Directory.CreateDirectory(outputPath);

    // Save all messages to a single file
    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    var outputFile = Path.Combine(outputPath, $"{topic}-messages-{timestamp}.txt");
    File.WriteAllText(outputFile, output);
    Console.WriteLine($"  ✓ Raw output: {Path.GetFileName(outputFile)}");

    // Save formatted JSON if parseable
    var jsonFile = Path.Combine(outputPath, $"{topic}-formatted-{timestamp}.json");
    var formattedData = new
    {
        topic = topic,
        extractedAt = DateTime.UtcNow,
        messageCount = messages.Count,
        messages = messages
    };
    File.WriteAllText(jsonFile, JsonConvert.SerializeObject(formattedData, Formatting.Indented));
    Console.WriteLine($"  ✓ Formatted: {Path.GetFileName(jsonFile)}");

    Console.WriteLine($"\n✅ Saved to {outputPath}/\n");

    Console.WriteLine("═══════════════════════════════════════════════");
    Console.WriteLine("  ✨ Kafka message extraction completed!");
    Console.WriteLine("═══════════════════════════════════════════════\n");

    Console.WriteLine("Output files:");
    Console.WriteLine($"  - {outputFile}");
    Console.WriteLine($"  - {jsonFile}");
    Console.WriteLine($"\nTotal messages extracted: {messages.Count}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    Environment.Exit(1);
}

static string? GetArgument(string[] args, string key)
{
    var arg = args.FirstOrDefault(a => a.StartsWith(key + "="));
    return arg?.Split('=', 2)[1];
}

static object? TryParseJson(string value)
{
    try
    {
        return JToken.Parse(value).ToObject<object>();
    }
    catch
    {
        return null; // Not JSON, return as string
    }
}

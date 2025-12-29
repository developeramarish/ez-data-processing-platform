using Hazelcast;
using Hazelcast.Core;
using System.CommandLine;

// Simple command-line tool to reset Hazelcast cache maps.
// Usage:
//   dotnet run                           # Clears all known maps (default)
//   dotnet run --maps file-content       # Clears specific map
//   dotnet run --maps file-content,valid-records  # Clears multiple maps
//   dotnet run --list                    # Lists all maps and their sizes
//   dotnet run --host localhost:5701     # Use custom Hazelcast host

var hostOption = new Option<string>(
    "--host",
    () => "localhost:5701",
    "Hazelcast server address (host:port)");

var mapsOption = new Option<string[]>(
    "--maps",
    () => new[] { "file-content", "valid-records" },
    "Comma-separated list of map names to clear");

var listOption = new Option<bool>(
    "--list",
    () => false,
    "List all maps and their entry counts instead of clearing");

var clearAllOption = new Option<bool>(
    "--clear-all",
    () => false,
    "Clear ALL maps including file-hashes deduplication cache");

var rootCommand = new RootCommand("Hazelcast Cache Reset Tool for EZ Platform")
{
    hostOption,
    mapsOption,
    listOption,
    clearAllOption
};

rootCommand.SetHandler(async (string host, string[] maps, bool list, bool clearAll) =>
{
    Console.WriteLine("===========================================");
    Console.WriteLine("  Hazelcast Cache Reset Tool - EZ Platform");
    Console.WriteLine("===========================================");
    Console.WriteLine();

    try
    {
        var hostParts = host.Split(':');
        var hostName = hostParts[0];
        var port = hostParts.Length > 1 ? int.Parse(hostParts[1]) : 5701;

        Console.WriteLine($"Connecting to Hazelcast at {hostName}:{port}...");

        var options = new HazelcastOptionsBuilder()
            .With(opts =>
            {
                opts.ClusterName = "data-processing-cluster";
                opts.Networking.Addresses.Clear();
                opts.Networking.Addresses.Add($"{hostName}:{port}");
                opts.Networking.ConnectionTimeoutMilliseconds = 10000;
            })
            .Build();

        await using var client = await HazelcastClientFactory.StartNewClientAsync(options);
        Console.WriteLine("Connected to Hazelcast cluster!");
        Console.WriteLine();

        if (list)
        {
            // List mode - show all distributed objects
            Console.WriteLine("Listing all Hazelcast maps:");
            Console.WriteLine("-------------------------------------------");

            var objects = await client.GetDistributedObjectsAsync();
            var mapCount = 0;

            foreach (var obj in objects)
            {
                if (obj.ServiceName == "hz:impl:mapService")
                {
                    var map = await client.GetMapAsync<string, object>(obj.Name);
                    var size = await map.GetSizeAsync();
                    Console.WriteLine($"  Map: {obj.Name,-30} Entries: {size}");
                    mapCount++;
                }
            }

            if (mapCount == 0)
            {
                Console.WriteLine("  (No maps found)");
            }

            Console.WriteLine("-------------------------------------------");
        }
        else if (clearAll)
        {
            // Clear ALL maps mode - including file-hashes deduplication cache
            Console.WriteLine("Clearing ALL Hazelcast maps (including deduplication cache):");
            Console.WriteLine("-------------------------------------------");

            var objects = await client.GetDistributedObjectsAsync();
            var clearedCount = 0;
            var totalEntries = 0;

            foreach (var obj in objects)
            {
                if (obj.ServiceName == "hz:impl:mapService")
                {
                    try
                    {
                        Console.Write($"  Clearing '{obj.Name}'... ");
                        var map = await client.GetMapAsync<string, object>(obj.Name);
                        var size = await map.GetSizeAsync();
                        totalEntries += size;
                        await map.ClearAsync();
                        Console.WriteLine($"Done! (was {size} entries)");
                        clearedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed! ({ex.Message})");
                    }
                }
            }

            Console.WriteLine("-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine($"Cleared {clearedCount} map(s) with {totalEntries} total entries!");
        }
        else
        {
            // Clear mode - clear specified maps
            Console.WriteLine($"Clearing {maps.Length} map(s):");
            Console.WriteLine("-------------------------------------------");

            foreach (var mapName in maps)
            {
                try
                {
                    Console.Write($"  Clearing map '{mapName}'... ");
                    var map = await client.GetMapAsync<string, object>(mapName);
                    var sizeBeforeClear = await map.GetSizeAsync();
                    await map.ClearAsync();
                    Console.WriteLine($"Done! (was {sizeBeforeClear} entries)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed! ({ex.Message})");
                }
            }

            Console.WriteLine("-------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Cache cleared successfully!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("Make sure:");
        Console.WriteLine("  1. Port forwarding is active: kubectl port-forward hazelcast-0 5701:5701 -n ez-platform");
        Console.WriteLine("  2. Or run the port forward script: scripts/start-port-forwards.ps1");
        Console.WriteLine();
        Environment.Exit(1);
    }

}, hostOption, mapsOption, listOption, clearAllOption);

await rootCommand.InvokeAsync(args);

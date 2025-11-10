using ServiceOrchestrator.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var command = args.Length > 0 ? args[0].ToLower() : "help";

var sequencer = new StartupSequencer();

switch (command)
{
    case "start":
        await sequencer.StartAllServicesAsync();
        Console.WriteLine("Press Ctrl+C to exit (services will continue running)");
        Console.ReadLine();
        break;
        
    case "stop":
        await sequencer.StopAllServicesAsync();
        break;
        
    case "restart":
        await sequencer.StopAllServicesAsync();
        await Task.Delay(2000);
        await sequencer.StartAllServicesAsync();
        Console.WriteLine("Press Ctrl+C to exit (services will continue running)");
        Console.ReadLine();
        break;
        
    case "status":
        Console.WriteLine("Service Status Check - Not yet implemented");
        Console.WriteLine("Use 'start' to launch all services");
        break;
        
    default:
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine("   EZ Platform - Service Orchestrator");
        Console.WriteLine("════════════════════════════════════════\n");
        Console.WriteLine("Usage: dotnet run [command]\n");
        Console.WriteLine("Commands:");
        Console.WriteLine("  start    - Start all services in order");
        Console.WriteLine("  stop     - Stop all services");
        Console.WriteLine("  restart  - Restart all services");
        Console.WriteLine("  status   - Check service status\n");
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run start");
        Console.WriteLine("  dotnet run stop\n");
        break;
}

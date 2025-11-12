// FolderOutputHandler.cs - Local Folder Output Handler
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: November 12, 2025

using System.Diagnostics;
using DataProcessing.Shared.Entities;

namespace OutputService.Handlers;

/// <summary>
/// Handles output to local file system folders
/// </summary>
public class FolderOutputHandler : IOutputHandler
{
    private readonly ILogger<FolderOutputHandler> _logger;
    
    public FolderOutputHandler(ILogger<FolderOutputHandler> logger)
    {
        _logger = logger;
    }
    
    public bool CanHandle(string destinationType) => destinationType == "folder";
    
    public async Task<OutputResult> WriteAsync(
        OutputDestination destination,
        string content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (destination.FolderConfig == null)
            {
                throw new ArgumentException("FolderConfig is required for folder destination type");
            }
            
            var config = destination.FolderConfig;
            
            // Build output path
            var outputPath = BuildOutputPath(config, fileName, destination.Name);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }
            
            // Handle existing file
            if (File.Exists(outputPath) && !config.OverwriteExisting)
            {
                // Append timestamp to avoid overwrite
                var ext = Path.GetExtension(outputPath);
                var baseName = Path.GetFileNameWithoutExtension(outputPath);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                outputPath = Path.Combine(directory!, $"{baseName}_{timestamp}{ext}");
                
                _logger.LogInformation(
                    "File exists and overwrite=false, using: {NewPath}",
                    outputPath);
            }
            
            // Write file
            await File.WriteAllTextAsync(outputPath, content, cancellationToken);
            
            _logger.LogInformation(
                "Wrote file to folder: {Path}, destination={Destination}, size={Size} bytes",
                outputPath,
                destination.Name,
                content.Length);
            
            stopwatch.Stop();
            
            return OutputResult.CreateSuccess(
                destination.Name,
                "folder",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Failed to write to folder destination {Destination}, path {Path}",
                destination.Name,
                destination.FolderConfig?.Path);
            
            return OutputResult.CreateFailure(
                destination.Name,
                "folder",
                ex.Message,
                stopwatch.Elapsed);
        }
    }
    
    private string BuildOutputPath(FolderOutputConfig config, string fileName, string datasourceName)
    {
        var basePath = config.Path;
        
        // Add subfolders if configured
        if (config.CreateSubfolders && !string.IsNullOrEmpty(config.SubfolderPattern))
        {
            var subfolder = ReplaceDatePlaceholders(config.SubfolderPattern, datasourceName);
            basePath = Path.Combine(basePath, subfolder);
        }
        
        // Build filename
        var outputFileName = string.IsNullOrEmpty(config.FileNamePattern)
            ? fileName
            : ReplaceFileNamePlaceholders(config.FileNamePattern, fileName, datasourceName);
        
        return Path.Combine(basePath, outputFileName);
    }
    
    private string ReplaceDatePlaceholders(string pattern, string datasourceName)
    {
        var now = DateTime.UtcNow;
        return pattern
            .Replace("{year}", now.ToString("yyyy"))
            .Replace("{month}", now.ToString("MM"))
            .Replace("{day}", now.ToString("dd"))
            .Replace("{date}", now.ToString("yyyyMMdd"))
            .Replace("{datasource}", datasourceName);
    }
    
    private string ReplaceFileNamePlaceholders(string pattern, string fileName, string datasourceName)
    {
        var now = DateTime.UtcNow;
        var ext = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        
        return pattern
            .Replace("{filename}", nameWithoutExt)
            .Replace("{ext}", ext.TrimStart('.'))
            .Replace("{date}", now.ToString("yyyyMMdd"))
            .Replace("{timestamp}", now.ToString("yyyyMMddHHmmss"))
            .Replace("{datasource}", datasourceName);
    }
}

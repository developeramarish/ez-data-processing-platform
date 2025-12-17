// FolderOutputHandlerTests.cs - Unit Tests for FolderOutputHandler
// UNIT-002: Folder Output Handler Tests
// Version: 1.0
// Date: December 17, 2025

using DataProcessing.Output.Handlers;
using DataProcessing.Output.Models;
using Xunit;
using DataProcessing.Shared.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataProcessing.Output.Tests.Handlers;

/// <summary>
/// Unit tests for FolderOutputHandler
/// Tests file writing, directory creation, path building,
/// overwrite handling, and error scenarios.
/// </summary>
public class FolderOutputHandlerTests : IDisposable
{
    private readonly Mock<ILogger<FolderOutputHandler>> _mockLogger;
    private readonly FolderOutputHandler _handler;
    private readonly string _testOutputPath;

    public FolderOutputHandlerTests()
    {
        _mockLogger = new Mock<ILogger<FolderOutputHandler>>();
        _handler = new FolderOutputHandler(_mockLogger.Object);
        _testOutputPath = Path.Combine(Path.GetTempPath(), $"FolderOutputHandlerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    #region CanHandle Tests

    [Fact]
    public void CanHandle_WithFolderType_ReturnsTrue()
    {
        // Act
        var result = _handler.CanHandle("folder");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("kafka")]
    [InlineData("sftp")]
    [InlineData("http")]
    [InlineData("FOLDER")]
    [InlineData("")]
    [InlineData(null)]
    public void CanHandle_WithNonFolderType_ReturnsFalse(string? destinationType)
    {
        // Act
        var result = _handler.CanHandle(destinationType!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region WriteAsync Basic Tests

    [Fact]
    public async Task WriteAsync_WithValidConfig_WritesFileSuccessfully()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        var content = """{"transactionId":"TXN-001","amount":100.50}""";
        var fileName = "transactions.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        result.DestinationType.Should().Be("folder");
        result.DestinationName.Should().Be(destination.Name);
        result.BytesWritten.Should().Be(content.Length);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // Verify file exists
        var expectedPath = Path.Combine(_testOutputPath, fileName);
        File.Exists(expectedPath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(expectedPath);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task WriteAsync_WithoutFolderConfig_ReturnsFailure()
    {
        // Arrange
        var destination = new OutputDestination
        {
            Name = "test-destination",
            Type = "folder",
            FolderConfig = null
        };
        var content = "test content";
        var fileName = "test.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("FolderConfig is required");
    }

    #endregion

    #region Directory Creation Tests

    [Fact]
    public async Task WriteAsync_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var newSubfolder = Path.Combine(_testOutputPath, "new-subfolder");
        var destination = CreateFolderDestination(newSubfolder);
        var content = "test content";
        var fileName = "test.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(newSubfolder).Should().BeTrue();
        File.Exists(Path.Combine(newSubfolder, fileName)).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithSubfolderPattern_CreatesSubfolders()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.CreateSubfolders = true;
        destination.FolderConfig.SubfolderPattern = "{year}/{month}/{day}";
        var content = "test content";
        var fileName = "test.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();

        // Verify subfolder was created with date pattern
        var now = DateTime.UtcNow;
        var expectedSubfolder = Path.Combine(_testOutputPath, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
        Directory.Exists(expectedSubfolder).Should().BeTrue();
    }

    #endregion

    #region Overwrite Handling Tests

    [Fact]
    public async Task WriteAsync_WhenFileExistsAndOverwriteTrue_OverwritesFile()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.OverwriteExisting = true;
        var fileName = "existing.json";
        var existingPath = Path.Combine(_testOutputPath, fileName);

        // Create existing file
        await File.WriteAllTextAsync(existingPath, "old content");
        var newContent = "new content";

        // Act
        var result = await _handler.WriteAsync(destination, newContent, fileName);

        // Assert
        result.Success.Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(existingPath);
        writtenContent.Should().Be(newContent);
    }

    [Fact]
    public async Task WriteAsync_WhenFileExistsAndOverwriteFalse_CreatesNewFileWithTimestamp()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.OverwriteExisting = false;
        var fileName = "existing.json";
        var existingPath = Path.Combine(_testOutputPath, fileName);

        // Create existing file
        await File.WriteAllTextAsync(existingPath, "old content");
        var newContent = "new content";

        // Act
        var result = await _handler.WriteAsync(destination, newContent, fileName);

        // Assert
        result.Success.Should().BeTrue();

        // Original file should still have old content
        var oldContent = await File.ReadAllTextAsync(existingPath);
        oldContent.Should().Be("old content");

        // New file with timestamp should exist
        var files = Directory.GetFiles(_testOutputPath, "existing_*.json");
        files.Should().HaveCount(1);
        var newFileContent = await File.ReadAllTextAsync(files[0]);
        newFileContent.Should().Be(newContent);
    }

    #endregion

    #region Filename Pattern Tests

    [Fact]
    public async Task WriteAsync_WithFileNamePattern_AppliesPattern()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.FileNamePattern = "{filename}_processed.{ext}";
        var content = "test content";
        var fileName = "transactions.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();

        var expectedFileName = "transactions_processed.json";
        var expectedPath = Path.Combine(_testOutputPath, expectedFileName);
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithDatePlaceholder_AppliesDateToFilename()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.FileNamePattern = "{filename}_{date}.{ext}";
        var content = "test content";
        var fileName = "data.csv";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();

        var expectedDate = DateTime.UtcNow.ToString("yyyyMMdd");
        var expectedFileName = $"data_{expectedDate}.csv";
        var expectedPath = Path.Combine(_testOutputPath, expectedFileName);
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithoutFileNamePattern_UsesOriginalFileName()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        destination.FolderConfig!.FileNamePattern = null;
        var content = "test content";
        var fileName = "original-name.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPath = Path.Combine(_testOutputPath, fileName);
        File.Exists(expectedPath).Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task WriteAsync_WithInvalidPath_ReturnsFailure()
    {
        // Arrange
        var destination = CreateFolderDestination("Z:\\NonExistent\\Invalid\\Path");
        var content = "test content";
        var fileName = "test.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WriteAsync_WithEmptyContent_WritesEmptyFile()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        var content = "";
        var fileName = "empty.json";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPath = Path.Combine(_testOutputPath, fileName);
        File.Exists(expectedPath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(expectedPath);
        writtenContent.Should().BeEmpty();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task WriteAsync_WithCancelledToken_ReturnsFailure()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        var content = "test content";
        var fileName = "test.json";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Large File Tests

    [Fact]
    public async Task WriteAsync_WithLargeContent_WritesSuccessfully()
    {
        // Arrange
        var destination = CreateValidFolderDestination();
        var content = new string('x', 1024 * 1024); // 1MB of data
        var fileName = "large-file.txt";

        // Act
        var result = await _handler.WriteAsync(destination, content, fileName);

        // Assert
        result.Success.Should().BeTrue();
        result.BytesWritten.Should().Be(content.Length);

        var expectedPath = Path.Combine(_testOutputPath, fileName);
        var fileInfo = new FileInfo(expectedPath);
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThanOrEqualTo(content.Length);
    }

    #endregion

    #region Helper Methods

    private OutputDestination CreateValidFolderDestination()
    {
        return CreateFolderDestination(_testOutputPath);
    }

    private static OutputDestination CreateFolderDestination(string path)
    {
        return new OutputDestination
        {
            Id = Guid.NewGuid().ToString(),
            Name = "test-folder-destination",
            Type = "folder",
            Enabled = true,
            FolderConfig = new FolderOutputConfig
            {
                Path = path,
                FileNamePattern = null,
                CreateSubfolders = false,
                SubfolderPattern = null,
                OverwriteExisting = true
            }
        };
    }

    #endregion
}

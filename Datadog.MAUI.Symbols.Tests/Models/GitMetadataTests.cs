using Datadog.MAUI.Symbols.Models;
using System.Text.Json;

namespace Datadog.MAUI.Symbols.Tests.Models;

public class GitMetadataTests
{
    [Fact]
    public void ToRepositoryPayload_ShouldCreateCorrectStructure()
    {
        // Arrange
        var metadata = new GitMetadata
        {
            Hash = "abc123def456",
            Remote = "https://github.com/test/repo.git",
            TrackedFiles = new[] { "file1.cs", "file2.cs", "file3.cs" }
        };

        // Act
        var payload = metadata.ToRepositoryPayload();
        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        Assert.Equal(1, deserialized.GetProperty("version").GetInt32());

        var data = deserialized.GetProperty("data").EnumerateArray().First();
        Assert.Equal("abc123def456", data.GetProperty("hash").GetString());
        Assert.Equal("https://github.com/test/repo.git", data.GetProperty("repository_url").GetString());

        var files = data.GetProperty("files").EnumerateArray().Select(f => f.GetString()).ToArray();
        Assert.Equal(3, files.Length);
        Assert.Contains("file1.cs", files);
        Assert.Contains("file2.cs", files);
        Assert.Contains("file3.cs", files);
    }

    [Fact]
    public void ToRepositoryPayload_WithEmptyTrackedFiles_ShouldStillWork()
    {
        // Arrange
        var metadata = new GitMetadata
        {
            Hash = "abc123",
            Remote = "https://github.com/test/repo.git",
            TrackedFiles = Array.Empty<string>()
        };

        // Act
        var payload = metadata.ToRepositoryPayload();
        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        var data = deserialized.GetProperty("data").EnumerateArray().First();
        var files = data.GetProperty("files").EnumerateArray().ToArray();
        Assert.Empty(files);
    }
}

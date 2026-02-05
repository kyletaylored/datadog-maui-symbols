using Datadog.MAUI.SymbolsUpload.Models;

namespace Datadog.MAUI.SymbolsUpload.Tests.Models;

public class UploadResultTests
{
    [Fact]
    public void CreateSuccess_ShouldReturnSuccessResult()
    {
        // Act
        var result = UploadResult.CreateSuccess();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public void CreateFailure_WithErrorMessage_ShouldReturnFailureResult()
    {
        // Act
        var result = UploadResult.CreateFailure("Upload failed");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Upload failed", result.ErrorMessage);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public void CreateFailure_WithStatusCode_ShouldIncludeStatusCode()
    {
        // Act
        var result = UploadResult.CreateFailure("Server error", 500);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Server error", result.ErrorMessage);
        Assert.Equal(500, result.StatusCode);
    }
}

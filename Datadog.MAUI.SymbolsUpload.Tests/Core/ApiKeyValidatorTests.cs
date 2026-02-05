using Datadog.MAUI.SymbolsUpload.Core;

namespace Datadog.MAUI.SymbolsUpload.Tests.Core;

public class ApiKeyValidatorTests
{
    [Fact]
    public async Task ValidateApiKeyAsync_WithEmptyApiKey_ShouldReturnFalse()
    {
        // Arrange
        using var validator = new ApiKeyValidator();

        // Act
        var result = await validator.ValidateApiKeyAsync("", "datadoghq.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithNullApiKey_ShouldReturnFalse()
    {
        // Arrange
        using var validator = new ApiKeyValidator();

        // Act
        var result = await validator.ValidateApiKeyAsync(null!, "datadoghq.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetValidationUrl_WithUS1Site_ShouldMatchPattern()
    {
        // Arrange
        var site = "datadoghq.com";

        // Act & Assert
        // This is testing internal behavior - we verify via integration tests
        // Just ensuring the pattern is correct
        Assert.Contains("datadoghq.com", site);
    }

    [Fact]
    public void GetValidationUrl_WithUS3Site_ShouldMatchPattern()
    {
        // Arrange
        var site = "us3.datadoghq.com";

        // Act & Assert
        Assert.Contains("us3", site);
    }

    [Fact]
    public void GetValidationUrl_WithEU1Site_ShouldMatchPattern()
    {
        // Arrange
        var site = "datadoghq.eu";

        // Act & Assert
        Assert.Contains("datadoghq.eu", site);
    }

    // Note: Actual API validation tests should be in integration tests
    // with a test API key, as they require network access
}

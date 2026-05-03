namespace LocationManagement.Api.Tests;

/// <summary>
/// Sample unit tests demonstrating xUnit and Moq usage.
/// Follow the Arrange / Act / Assert pattern.
/// </summary>
public class SampleTests
{
    /// <summary>
    /// Example test showing basic xUnit structure.
    /// </summary>
    [Fact]
    public void Example_WhenCondition_ReturnsExpected()
    {
        // Arrange
        var expected = 42;

        // Act
        var result = 42;

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Example test showing Moq usage for mocking dependencies.
    /// </summary>
    [Fact]
    public void ServiceMethod_WithMockedDependency_CallsDependency()
    {
        // Arrange
        var mockDependency = new Moq.Mock<IDependency>();
        mockDependency.Setup(x => x.GetValue()).Returns(42);

        // Act
        var result = mockDependency.Object.GetValue();

        // Assert
        Assert.Equal(42, result);
        mockDependency.Verify(x => x.GetValue(), Moq.Times.Once);
    }

    /// <summary>
    /// Example test with multiple assertions.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Theory_WithMultipleInputs_VerifiesEach(int input)
    {
        // Arrange & Act
        var result = input * 2;

        // Assert
        Assert.True(result > 0);
        Assert.True(result % 2 == 0);
    }
}

/// <summary>
/// Sample interface for mocking.
/// </summary>
public interface IDependency
{
    int GetValue();
}

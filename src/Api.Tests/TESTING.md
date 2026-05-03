# Backend Testing Guide

This document describes the testing setup and best practices for the Location Management API.

## Test Stack

- **xUnit** — Modern unit testing framework for .NET
- **Moq** — Mocking library for isolating dependencies
- **coverlet** — Code coverage measurement

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test /p:CollectCoverage=true

# Run a specific test class
dotnet test --filter "ClassName"

# Run tests matching a pattern
dotnet test --filter "MethodName"
```

## Test Structure

Tests follow the **Arrange / Act / Assert** pattern:

```csharp
[Fact]
public void MethodName_WhenCondition_ReturnsExpected()
{
    // Arrange — set up test data and mocks
    var input = 42;
    var mockService = new Mock<IService>();
    mockService.Setup(x => x.GetValue()).Returns(input);

    // Act — execute the code being tested
    var result = mockService.Object.GetValue();

    // Assert — verify the outcome
    Assert.Equal(input, result);
}
```

## Best Practices

### 1. Test Naming

Use the pattern: `MethodName_WhenCondition_ReturnsExpected`

```csharp
// Good
[Fact]
public void CreateUser_WithValidData_ReturnsUserId()

[Fact]
public void CreateUser_WithDuplicateEmail_ThrowsException()

// Avoid
[Fact]
public void TestCreateUser()

[Fact]
public void CreateUserTest()
```

### 2. One Behavior Per Test

```csharp
// Good — tests one thing
[Fact]
public void ValidateEmail_WithInvalidFormat_ReturnsFalse()
{
    var result = EmailValidator.Validate("invalid");
    Assert.False(result);
}

// Avoid — tests multiple things
[Fact]
public void ValidateEmail_ChecksFormatAndLength()
{
    // Tests two behaviors at once
}
```

### 3. Use Mocks for Dependencies

```csharp
[Fact]
public void GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    var userId = Guid.NewGuid();
    var expectedUser = new User { Id = userId, Name = "Test" };
    
    mockRepository
        .Setup(x => x.GetByIdAsync(userId))
        .ReturnsAsync(expectedUser);

    var service = new UserService(mockRepository.Object);

    // Act
    var result = await service.GetUserAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedUser.Id, result.Id);
    mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
}
```

### 4. Use Theory for Multiple Inputs

```csharp
[Theory]
[InlineData(0, false)]
[InlineData(1, true)]
[InlineData(-1, false)]
public void IsPositive_WithVariousInputs_ReturnsCorrectly(int input, bool expected)
{
    var result = NumberValidator.IsPositive(input);
    Assert.Equal(expected, result);
}
```

### 5. Test Exception Handling

```csharp
[Fact]
public void CreateUser_WithNullName_ThrowsArgumentNullException()
{
    var service = new UserService();
    
    var exception = Assert.Throws<ArgumentNullException>(() =>
        service.CreateUser(null, "email@example.com")
    );
    
    Assert.Equal("name", exception.ParamName);
}
```

## Mocking Patterns

### Mock Setup

```csharp
var mock = new Mock<IService>();

// Setup return value
mock.Setup(x => x.GetValue()).Returns(42);

// Setup async return value
mock.Setup(x => x.GetValueAsync()).ReturnsAsync(42);

// Setup with parameters
mock.Setup(x => x.GetValue(It.IsAny<int>())).Returns(42);

// Setup to throw exception
mock.Setup(x => x.GetValue()).Throws<InvalidOperationException>();
```

### Verify Calls

```csharp
// Verify called once
mock.Verify(x => x.GetValue(), Times.Once);

// Verify called exactly N times
mock.Verify(x => x.GetValue(), Times.Exactly(3));

// Verify never called
mock.Verify(x => x.GetValue(), Times.Never);

// Verify called with specific arguments
mock.Verify(x => x.GetValue(42), Times.Once);
```

## File Organization

```
src/Api.Tests/
  SampleTests.cs              # Example tests
  Services/
    UserServiceTests.cs
    LocationServiceTests.cs
  Controllers/
    AuthControllerTests.cs
    LocationsControllerTests.cs
  Repositories/
    UserRepositoryTests.cs
  Fixtures/
    TestData.cs              # Shared test data
    MockFactory.cs           # Mock creation helpers
```

## Code Coverage

Target coverage:
- **Overall**: ≥ 80%
- **Critical paths**: 100%
- **Utilities**: ≥ 75%

Generate coverage report:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Testing Async Code

```csharp
[Fact]
public async Task GetUserAsync_WithValidId_ReturnsUser()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    mockRepository
        .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(new User { Id = Guid.NewGuid() });

    var service = new UserService(mockRepository.Object);

    // Act
    var result = await service.GetUserAsync(Guid.NewGuid());

    // Assert
    Assert.NotNull(result);
}
```

## Testing Database Operations

For integration tests with real database:

```csharp
[Fact]
public async Task CreateUser_WithValidData_PersistsToDatabase()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("test-db")
        .Options;

    using var context = new AppDbContext(options);
    var repository = new UserRepository(context);

    // Act
    var user = new User { Username = "test", Email = "test@example.com" };
    await repository.CreateAsync(user);

    // Assert
    var saved = await context.Users.FirstOrDefaultAsync(u => u.Username == "test");
    Assert.NotNull(saved);
}
```

## Common Assertions

```csharp
// Equality
Assert.Equal(expected, actual);
Assert.NotEqual(expected, actual);

// Null checks
Assert.Null(value);
Assert.NotNull(value);

// Boolean
Assert.True(condition);
Assert.False(condition);

// Collections
Assert.Empty(collection);
Assert.NotEmpty(collection);
Assert.Contains(item, collection);
Assert.Single(collection);

// Exceptions
Assert.Throws<ExceptionType>(() => code);
Assert.ThrowsAsync<ExceptionType>(async () => await code);
```

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

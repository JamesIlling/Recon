# Mutation Testing with Stryker.NET

Mutation testing is a technique to evaluate the quality of your test suite by introducing small changes (mutations) to your code and checking if your tests catch them.

## What is Mutation Testing?

Mutation testing works by:
1. Creating mutants (modified versions) of your code
2. Running your tests against each mutant
3. Checking if tests fail (mutant is "killed") or pass (mutant "survives")
4. Calculating a mutation score based on killed vs. surviving mutants

A high mutation score indicates your tests are effective at catching bugs.

## Running Mutation Tests

### Install Stryker.NET

```bash
dotnet tool install -g dotnet-stryker
```

### Run Mutation Tests

```bash
# Run from the test project directory
cd src/Api.Tests

# Run mutation tests
dotnet stryker

# Run with specific configuration
dotnet stryker --config-file stryker-config.json

# Run with specific test runner
dotnet stryker --test-runner xunit
```

### View Results

After running, Stryker generates reports in `StrykerOutput/`:

```bash
# Open HTML report
open StrykerOutput/index.html
```

## Configuration

The `stryker-config.json` file controls Stryker behavior:

```json
{
  "stryker-config": {
    "mutate": ["src/**/*.cs"],           // Files to mutate
    "reporters": ["html", "json"],       // Report formats
    "thresholdBreak": 80,                // Fail if score below this
    "thresholdHigh": 90,                 // High quality threshold
    "thresholdLow": 70,                  // Low quality threshold
    "concurrency": 4,                    // Parallel workers
    "testRunner": "xunit"                // Test framework
  }
}
```

## Mutation Score Interpretation

| Score | Quality |
|-------|---------|
| 90-100% | Excellent — comprehensive test coverage |
| 80-89% | Good — solid test coverage |
| 70-79% | Fair — acceptable but could improve |
| < 70% | Poor — insufficient test coverage |

## Common Mutations

Stryker applies mutations like:

- **Arithmetic**: `+` → `-`, `*` → `/`
- **Logical**: `&&` → `||`, `true` → `false`
- **Comparison**: `==` → `!=`, `<` → `<=`
- **Boundary**: `>` → `>=`, `<` → `<=`
- **Return values**: `return x` → `return null`
- **Conditionals**: Remove `if` conditions

## Improving Mutation Score

### 1. Test Edge Cases

```csharp
// Good — tests boundary conditions
[Theory]
[InlineData(0)]
[InlineData(1)]
[InlineData(-1)]
[InlineData(int.MaxValue)]
public void IsPositive_WithVariousInputs_ReturnsCorrectly(int input)
{
    var result = NumberValidator.IsPositive(input);
    // Assert based on input
}
```

### 2. Test Both Branches

```csharp
// Good — tests both if and else
[Fact]
public void GetDiscount_WithValidAge_ReturnsCorrectDiscount()
{
    Assert.Equal(0.1m, DiscountCalculator.GetDiscount(65));
}

[Fact]
public void GetDiscount_WithInvalidAge_ReturnsZero()
{
    Assert.Equal(0m, DiscountCalculator.GetDiscount(30));
}
```

### 3. Verify Return Values

```csharp
// Good — verifies the actual return value
[Fact]
public void Add_WithTwoNumbers_ReturnsSum()
{
    var result = Calculator.Add(2, 3);
    Assert.Equal(5, result);  // Specific value, not just "not null"
}

// Avoid — too vague
[Fact]
public void Add_WithTwoNumbers_ReturnsValue()
{
    var result = Calculator.Add(2, 3);
    Assert.NotNull(result);   // Doesn't catch mutations
}
```

### 4. Test Exception Handling

```csharp
// Good — verifies exception is thrown
[Fact]
public void Divide_ByZero_ThrowsException()
{
    Assert.Throws<DivideByZeroException>(() =>
        Calculator.Divide(10, 0)
    );
}
```

## Excluding Mutations

If a mutation is not relevant, exclude it in the config:

```json
{
  "stryker-config": {
    "excludedMutations": [
      "string",
      "logical-operator"
    ]
  }
}
```

## CI/CD Integration

Add to your CI pipeline:

```bash
# Run mutation tests and fail if score is below threshold
dotnet stryker --config-file stryker-config.json
```

## Performance Tips

- Run mutation tests on demand, not on every build
- Use `concurrency` setting to parallelize
- Exclude generated code and test files
- Focus on critical paths first

## Resources

- [Stryker.NET Documentation](https://stryker-mutator.io/docs/stryker-net/introduction)
- [Mutation Testing Best Practices](https://stryker-mutator.io/docs/general/faq)
- [Effective Mutation Testing](https://stryker-mutator.io/docs/general/mutation-testing-intro)

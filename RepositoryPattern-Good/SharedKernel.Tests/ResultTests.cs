namespace WebShop.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_is_successful_with_no_errors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_is_unsuccessful_and_carries_the_given_errors()
    {
        var result = Result.Failure("Name is required.", "Key is required.");

        Assert.True(result.IsFailure);
        Assert.Equal(["Name is required.", "Key is required."], result.Errors);
    }

    [Fact]
    public void Map_wraps_a_value_as_success_when_the_source_result_succeeded()
    {
        var mapped = Result.Success().Map(42);

        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Map_propagates_the_original_errors_when_the_source_result_failed()
    {
        var mapped = Result.Failure("Name is required.").Map(42);

        Assert.True(mapped.IsFailure);
        Assert.Equal(["Name is required."], mapped.Errors);
    }

    [Fact]
    public void Generic_success_exposes_its_value()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_failure_throws_when_value_is_accessed()
    {
        var result = Result<int>.Failure("Not found.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}

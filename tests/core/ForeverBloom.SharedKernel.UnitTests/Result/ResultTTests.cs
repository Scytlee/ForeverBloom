using FluentAssertions;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.SharedKernel.UnitTests.Result;

public sealed class ResultTTests
{
    private readonly Error _testError1 = new() { Code = "TEST.1", Message = "Test error 1" };
    private readonly Error _testError2 = new() { Code = "TEST.2", Message = "Test error 2" };

    [Fact]
    public void Success_ShouldCorrectlyCreateSuccessResult()
    {
        const string value = "ok";
        var result = Result<string>.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCorrectlyCreateFailureResult()
    {
        var result = Result<string>.Failure(_testError1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().Be(_testError1);
    }

    [Fact]
    public void Match_ShouldInvokeOnSuccessOnly_WhenResultIsSuccess()
    {
        const string value = "ok";
        var result = Result<string>.Success(value);
        var onSuccessCalled = false;
        var onFailureCalled = false;
        string? capturedValue = null;
        const string successReturn = "success";
        const string failureReturn = "failure";

        var actualReturn = result.Match(
            onSuccess: innerValue =>
            {
                onSuccessCalled = true;
                capturedValue = innerValue;
                return successReturn;
            },
            onFailure: _ =>
            {
                onFailureCalled = true;
                return failureReturn;
            });

        onSuccessCalled.Should().BeTrue();
        onFailureCalled.Should().BeFalse();
        capturedValue.Should().Be(value);
        actualReturn.Should().Be(successReturn);
    }

    [Fact]
    public void Match_ShouldInvokeOnFailureOnly_WhenResultIsFailure()
    {
        var result = Result<string>.Failure(_testError1);
        var onSuccessCalled = false;
        var onFailureCalled = false;
        IError? capturedError = null;
        const string successReturn = "success";
        const string failureReturn = "failure";

        var actualReturn = result.Match(
            onSuccess: _ =>
            {
                onSuccessCalled = true;
                return successReturn;
            },
            onFailure: error =>
            {
                onFailureCalled = true;
                capturedError = error;
                return failureReturn;
            });

        onSuccessCalled.Should().BeFalse();
        onFailureCalled.Should().BeTrue();
        capturedError.Should().Be(_testError1);
        actualReturn.Should().Be(failureReturn);
    }

    [Fact]
    public void FromValidation_ShouldReturnSuccessWithFactoryValue_WhenNoErrors()
    {
        IError[] errors = [];
        const string expectedValue = "test-value";
        var factoryCalled = false;

        var result = Result<string>.FromValidation(errors, () =>
        {
            factoryCalled = true;
            return expectedValue;
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
        result.Error.Should().BeNull();
        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public void FromValidation_ShouldReturnFailureWithCompositeError_WhenAnyErrors()
    {
        var errors = new[] { _testError1, _testError2 };
        var factoryCalled = false;

        var result = Result<string>.FromValidation(errors, () =>
        {
            factoryCalled = true;
            return string.Empty;
        });

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeOfType<CompositeError>();
        factoryCalled.Should().BeFalse();

        var compositeError = (CompositeError)result.Error!;
        compositeError.Errors.Should().BeEquivalentTo([_testError1, _testError2]);
    }
}

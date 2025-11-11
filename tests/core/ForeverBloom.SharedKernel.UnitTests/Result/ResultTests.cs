using FluentAssertions;
using ForeverBloom.SharedKernel.Result;
using ResultType = ForeverBloom.SharedKernel.Result.Result;

namespace ForeverBloom.SharedKernel.UnitTests.Result;

public sealed class ResultTests
{
    private readonly Error _testError = new() { Code = "TEST", Message = "Test error" };

    [Fact]
    public void Success_ShouldCorrectlyCreateSuccessResult()
    {
        var result = ResultType.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCorrectlyCreateFailureResult()
    {
        var result = ResultType.Failure(_testError);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_testError);
    }

    [Fact]
    public void Match_ShouldInvokeOnSuccessOnly_WhenResultIsSuccess()
    {
        var result = ResultType.Success();
        var onSuccessCalled = false;
        var onFailureCalled = false;
        const string successReturn = "success";
        const string failureReturn = "failure";

        var actualReturn = result.Match(
            onSuccess: () =>
            {
                onSuccessCalled = true;
                return successReturn;
            },
            onFailure: _ =>
            {
                onFailureCalled = true;
                return failureReturn;
            });

        onSuccessCalled.Should().BeTrue();
        onFailureCalled.Should().BeFalse();
        actualReturn.Should().Be(successReturn);
    }

    [Fact]
    public void Match_ShouldInvokeOnFailureOnly_WhenResultIsFailure()
    {
        var result = ResultType.Failure(_testError);
        var onSuccessCalled = false;
        var onFailureCalled = false;
        IError? capturedError = null;
        const string successReturn = "success";
        const string failureReturn = "failure";

        var actualReturn = result.Match(
            onSuccess: () =>
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
        capturedError.Should().Be(_testError);
        actualReturn.Should().Be(failureReturn);
    }
}

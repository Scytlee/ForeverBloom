using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Api.Tests.Helpers;
using ForeverBloom.Testing.Common.BaseTestClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ForeverBloom.Api.Tests.EndpointFilters;

public sealed class ValidationFilterTests : TestClassBase
{
    private readonly IValidator<TestRequest> _validator;
    private readonly ValidationFilter<TestRequest> _filter;
    private readonly DefaultHttpContext _httpContext;

    public ValidationFilterTests()
    {
        _validator = Substitute.For<IValidator<TestRequest>>();
        _filter = new ValidationFilter<TestRequest>(_validator, NullLogger<ValidationFilter<TestRequest>>.Instance);
        _httpContext = HttpContextTestHelper.CreateHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenValidationSucceeds()
    {
        var request = new TestRequest { Name = "Valid" };
        _validator.ValidateAsync(Arg.Is<TestRequest>(x => x == request), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var context = CreateInvocationContext(_httpContext, request);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(ApiResults.Ok("test"));
        };

        var result = await _filter.InvokeAsync(context, next);

        nextCalled.Should().BeTrue();
        result.Should().BeOfType<OkResult<string>>();
        await _validator.Received(1).ValidateAsync(Arg.Is<TestRequest>(x => x == request), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnValidationProblem_WhenValidationFails()
    {
        var request = new TestRequest { Name = string.Empty };
        var failures = new List<ValidationFailure>
        {
            new(nameof(TestRequest.Name), "Name required")
            {
                ErrorCode = "NameRequired"
            }
        };

        _validator.ValidateAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var context = CreateInvocationContext(_httpContext, request);
        var nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(ApiResults.Ok("test"));
        };

        var result = await _filter.InvokeAsync(context, next);

        nextCalled.Should().BeFalse();
        result.Should().BeOfType<ValidationProblemResult>();
        var validationProblem = (ValidationProblemResult)result;
        validationProblem.Value.Errors.Should().ContainKey(nameof(TestRequest.Name));
        validationProblem.Value.Errors[nameof(TestRequest.Name)].Should().ContainSingle("NameRequired");
    }

    [Fact]
    public async Task InvokeAsync_ShouldGroupErrorsByProperty_WhenMultipleFailuresOccur()
    {
        var request = new TestRequest { Name = string.Empty };
        var failures = new List<ValidationFailure>
        {
            new(nameof(TestRequest.Name), "Name required")
            {
                ErrorCode = "NameRequired"
            },
            new(nameof(TestRequest.Name), "Name length")
            {
                ErrorCode = "NameLength"
            },
            new("Description", "Description required")
            {
                ErrorCode = "DescriptionRequired"
            }
        };

        _validator.ValidateAsync(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var context = CreateInvocationContext(_httpContext, request);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(TypedResults.Ok());

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<ValidationProblemResult>();
        var validationProblem = (ValidationProblemResult)result;
        validationProblem.Value.Errors.Should().HaveCount(2);
        validationProblem.Value.Errors[nameof(TestRequest.Name)].Should().BeEquivalentTo("NameRequired", "NameLength");
        validationProblem.Value.Errors["Description"].Should().BeEquivalentTo("DescriptionRequired");
    }

    private static TestEndpointFilterInvocationContext CreateInvocationContext(HttpContext httpContext, TestRequest request)
    {
        return new TestEndpointFilterInvocationContext(httpContext, request);
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] arguments)
        {
            HttpContext = httpContext;
            Arguments = arguments;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; }

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }

    public class TestRequest
    {
        public string? Name { get; set; }
    }
}

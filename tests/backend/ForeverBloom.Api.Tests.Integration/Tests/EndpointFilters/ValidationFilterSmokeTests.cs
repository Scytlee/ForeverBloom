using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Tests.BaseTestClasses;
using ForeverBloom.Api.Tests.Extensions;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Helpers.Validation;

namespace ForeverBloom.Api.Tests.EndpointFilters;

public sealed class ValidationFilterSmokeTests : BackendAppTestClassBase
{
    private const string EndpointUrl = "/api/v1/admin/products";

    public ValidationFilterSmokeTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task ValidationFilterEndpoint_ShouldRespondWith400BadRequest_WhenValidationErrorsOccur()
    {
        BuildApp();
        var client = _app.RequestClient().UseAdminKey();
        var invalidRequest = new CreateProductRequest
        {
            Name = string.Empty,
            Slug = string.Empty,
            CategoryId = 1
        };

        var response = await client.PostAsJsonAsync(EndpointUrl, invalidRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await response.ExtractErrorDictionaryAsync(TestContext.Current.CancellationToken);
        errors.ShouldContainErrorForProperty(nameof(CreateProductRequest.Name), ProductValidation.ErrorCodes.NameRequired);
        errors.ShouldContainErrorForProperty(nameof(CreateProductRequest.Slug), ProductValidation.ErrorCodes.SlugRequired);
    }
}


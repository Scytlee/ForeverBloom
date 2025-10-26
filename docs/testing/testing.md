# Testing Strategy & Infrastructure

ForeverBloom’s test suite emphasizes realistic integration coverage backed by a small but purposeful set of unit tests. The goal is fast feedback with high confidence in behavior that matters to users, while keeping the suite simple to run locally and in CI.

## Testing Philosophy

Integration tests exercise end‑to‑end flows and API contracts, which tend to remain stable even as internals evolve. Unit tests concentrate on core business rules where tight, fast feedback pays off. This mix keeps coverage resilient during refactors and delivers good return on maintenance effort.

## Test Organization

### 2×2 Test Matrix

|                     | API                                  | Persistence                                  |
|---------------------|--------------------------------------|----------------------------------------------|
| **Unit**            | `ForeverBloom.Api.Tests.Unit`        | `ForeverBloom.Persistence.Tests.Unit`        |
| **Integration**     | `ForeverBloom.Api.Tests.Integration` | `ForeverBloom.Persistence.Tests.Integration` |

The matrix crosses test type (unit vs. integration) with component (API vs. persistence). Unit tests are fast and isolate behavior with mocks; integration tests use real infrastructure.

### Naming Conventions

Test methods follow `MethodName_ShouldExpectedBehavior_WhenCondition`.
Examples:
- `CreateProductEndpoint_ShouldRespondWith201Created_AndProduct_WhenCreationIsSuccessful`
- `InsertProduct_ShouldPersistProductInDatabase`
- `HandleAsync_ShouldReturnValidationProblem_WhenSlugIsNotAvailable`

## Test Infrastructure

### Integration Test Database

Integration tests run against PostgreSQL via Testcontainers using a template‑database approach. A single `postgres:17.5-alpine` container is started for the session, migrations are applied once to a template database, and each test (class instance) creates its own database by cloning the template with `CREATE DATABASE ... TEMPLATE`. This keeps setup fast and guarantees isolation.

Parallelism is managed by a semaphore driven by the `MaxConcurrentDatabases` setting, which prevents resource contention on the container while still allowing concurrent execution.

### WebApplicationFactory

API integration tests use `WebApplicationFactory<Program>` to exercise the full HTTP pipeline in‑process while pointing the app at the per‑test database:
```csharp
var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = testDatabaseConnectionString
            });
        });
    });
```

## Testing Patterns

### Integration Test Example

A typical API integration test arranges state through HTTP, performs the call, then checks both the response and the database. See [`CreateCategoryEndpointTests.cs`](../../tests/backend/ForeverBloom.Api.Tests.Integration/Tests/Endpoints/Catalog/Categories/Admin/CreateCategory/CreateCategoryEndpointTests.cs):
```csharp
[Fact]
public async Task CreateCategoryEndpoint_ShouldRespondWith201Created_AndCategory_WhenCreationIsSuccessful()
{
    BuildApp();
    var client = _app.RequestClient().UseAdminKey();
    var request = new CreateCategoryRequest { /* ... */ };

    var response = await client.PostAsJsonAsync(EndpointUrl, request, ...);

    // Verify HTTP response
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var responseContent = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>(...);

    // Verify database state
    var persistedCategory = await DbContext.Categories.FindAsync(responseContent.Id);
    persistedCategory.Should().NotBeNull();
}
```

The pattern is: arrange via HTTP, act, assert the response, then assert the database state and any side effects.

### Unit Test Example

Unit tests primarily cover validators and other focused logic. For example, a FluentValidation test:
```csharp
[Theory]
[MemberData(nameof(ProductValidationData.NameValidationData))]
public void Validate_ShouldCorrectlyValidateName(string? name, string? expectedErrorCode)
{
    var request = CreateValidRequest() with { Name = name! };
    BaseTests.BaseValidationTest(_sut, request, r => r.Name, expectedErrorCode);
}
```

Theory data covers boundary cases (minimum and maximum lengths, null and empty inputs, and valid samples).

### Test Data Arrangement

API integration tests arrange state through the public HTTP endpoints to preserve business rules and keep a single source of truth.

Persistence tests seed directly through `DbContext` using helpers when HTTP would add noise. For example:
```csharp
var category = await DbContext.CreateCategoryAsync(
    name: $"Test Category {_testId:N}"[..20],
    slug: $"test-slug-{_testId:N}"[..20]
);
```

Each test runs against a fresh app and its own database. A `_testId` token is used to generate unique values where needed.

## Testing Tools

- **xUnit** - Test framework
- **FluentAssertions** - Expressive assertions
- **NSubstitute** - Mocking for unit tests
- **Testcontainers** - PostgreSQL containers for integration tests
- **Microsoft.AspNetCore.Mvc.Testing** - `WebApplicationFactory<Program>` for end-to-end HTTP testing
- **FluentValidation.TestHelper** - Validator testing helpers for FluentValidation

## Test Metrics

**Total Tests:** 388 (as of Phase 1 completion)
Counts reflect discovered test cases (Facts plus Theory datasets), not just method count.
- API Integration: 139 tests
- API Unit: 188 tests
- Persistence Integration: 41 tests
- Persistence Unit: 20 tests

**Code Coverage:**
- **Line coverage:** 94.4% (4,694 of 4,970 lines)
- **Branch coverage:** 75.9% (576 of 758 branches)
- **Method coverage:** 96.2% (687 of 714 methods)

**Performance:**
- Integration tests: ~10.7 seconds total
- Unit tests: ~0.28 seconds total
- **Total CI/CD test time:** ~11 seconds

## Cross-Cutting Concerns Testing

Endpoint metadata tests assert that filters, authorization, validation, and unit‑of‑work are configured as expected:
```csharp
endpoint.ShouldHaveAuthorizationPolicy(AuthorizationPolicies.AdminAccess);
endpoint.ShouldValidateRequest<CreateProductRequest>();
endpoint.ShouldUseUnitOfWork();
```

Smoke tests then exercise runtime behavior for each concern on a few representative endpoints (for example, 401/403 for auth and 400 for validation) instead of repeating the same checks everywhere.

using ForeverBloom.ApiClient.Endpoints.Categories;
using ForeverBloom.ApiClient.Endpoints.Products;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.ApiClient;

public sealed class ApiClient : IApiClient
{
    public ICategoryEndpoints Categories { get; }
    public IProductEndpoints Products { get; }

    public ApiClient(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        Categories = new CategoryEndpoints(httpClient, loggerFactory);
        Products = new ProductEndpoints(httpClient, loggerFactory);
    }
}

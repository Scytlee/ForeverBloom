using ForeverBloom.WebApi.Client.Endpoints.Categories;
using ForeverBloom.WebApi.Client.Endpoints.Products;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.WebApi.Client;

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

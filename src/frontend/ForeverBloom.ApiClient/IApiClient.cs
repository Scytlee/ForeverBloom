using ForeverBloom.ApiClient.Endpoints.Categories;
using ForeverBloom.ApiClient.Endpoints.Products;

namespace ForeverBloom.ApiClient;

public interface IApiClient
{
    public ICategoryEndpoints Categories { get; }
    public IProductEndpoints Products { get; }
}

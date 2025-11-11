using ForeverBloom.WebApi.Client.Endpoints.Categories;
using ForeverBloom.WebApi.Client.Endpoints.Products;

namespace ForeverBloom.WebApi.Client;

public interface IApiClient
{
    public ICategoryEndpoints Categories { get; }
    public IProductEndpoints Products { get; }
}

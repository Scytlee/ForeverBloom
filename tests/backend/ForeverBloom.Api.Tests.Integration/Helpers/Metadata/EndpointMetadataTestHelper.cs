using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace ForeverBloom.Api.Tests.Helpers.Metadata;

public static class EndpointMetadataTestHelper
{
    public static RouteEndpoint GetRequiredEndpointByName(EndpointDataSource dataSource, string endpointName)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        return dataSource.Endpoints
          .OfType<RouteEndpoint>()
          .Single(endpoint => string.Equals(
            endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
            endpointName,
            StringComparison.Ordinal));
    }

    public static IReadOnlyList<RouteEndpoint> GetEndpointsByTags(EndpointDataSource dataSource, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(tags);

        if (tags.Length == 0)
        {
            throw new ArgumentException("At least one tag must be specified", nameof(tags));
        }

        return dataSource.Endpoints
          .OfType<RouteEndpoint>()
          .Where(endpoint =>
          {
              var endpointTags = endpoint.Metadata.OfType<ITagsMetadata>().FirstOrDefault()?.Tags ?? [];
              return tags.All(tag => endpointTags.Contains(tag));
          })
          .ToList();
    }
}

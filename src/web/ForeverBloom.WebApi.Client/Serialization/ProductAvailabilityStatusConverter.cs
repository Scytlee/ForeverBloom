using System.Text.Json;
using System.Text.Json.Serialization;
using ForeverBloom.WebApi.Client.Endpoints.Products.Contracts;

namespace ForeverBloom.WebApi.Client.Serialization;

/// <summary>
/// JSON converter for ProductAvailabilityStatus enum that handles snake_case API responses.
/// </summary>
public sealed class ProductAvailabilityStatusConverter : JsonConverter<ProductAvailabilityStatus>
{
    public override ProductAvailabilityStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value switch
        {
            "available" => ProductAvailabilityStatus.Available,
            "out_of_stock" => ProductAvailabilityStatus.OutOfStock,
            "made_to_order" => ProductAvailabilityStatus.MadeToOrder,
            "discontinued" => ProductAvailabilityStatus.Discontinued,
            "coming_soon" => ProductAvailabilityStatus.ComingSoon,
            _ => throw new JsonException($"Unknown product availability status: '{value}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, ProductAvailabilityStatus value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            ProductAvailabilityStatus.Available => "available",
            ProductAvailabilityStatus.OutOfStock => "out_of_stock",
            ProductAvailabilityStatus.MadeToOrder => "made_to_order",
            ProductAvailabilityStatus.Discontinued => "discontinued",
            ProductAvailabilityStatus.ComingSoon => "coming_soon",
            _ => throw new JsonException($"Unknown product availability status: {value}")
        };

        writer.WriteStringValue(stringValue);
    }
}

using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class MoneyFactory
{
    public static Money Create(decimal value = 10m)
    {
        var moneyResult = Money.Create(value);
        moneyResult.Should().BeSuccess();
        return moneyResult.Value!;
    }
}

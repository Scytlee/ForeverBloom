using FluentAssertions;
using ForeverBloom.Api.Extensions;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Tests.Extensions;

public sealed class SortingExtensionsTests
{
    private record TestEntity(int Id, string Name, decimal Price, string Category);

    private static readonly TestEntity[] TestData =
    [
        new(3, "Gamma", 15.0m, "electronics"),
        new(4, "Delta", 5.0m, "Books"),
        new(1, "Alpha", 10.0m, "Electronics"),
        new(2, "beta", 25.0m, "Books")
    ];

    [Fact]
    public void ApplySort_ShouldHandleEmptySortColumns()
    {
        var query = TestData.AsQueryable();

        var result = query.ApplySort([]).ToList();

        result.Should().BeEquivalentTo(TestData, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ApplySort_ShouldSortByPropertyAscending()
    {
        var query = TestData.AsQueryable();
        var sortColumns = new[] { new SortCriterion("Name", "asc") };

        var result = query.ApplySort(sortColumns).ToList();

        result.Select(x => x.Name).Should().Equal("Alpha", "beta", "Delta", "Gamma");
    }

    [Fact]
    public void ApplySort_ShouldSortByPropertyDescending()
    {
        var query = TestData.AsQueryable();
        var sortColumns = new[] { new SortCriterion("Price", "desc") };

        var result = query.ApplySort(sortColumns).ToList();

        result.Select(x => x.Price).Should().Equal(25.0m, 15.0m, 10.0m, 5.0m);
    }

    [Fact]
    public void ApplySort_ShouldHandleMultipleColumns()
    {
        var query = TestData.AsQueryable();
        var sortColumns = new[]
        {
            new SortCriterion("Category", "asc"),
            new SortCriterion("Price", "desc")
        };

        var result = query.ApplySort(sortColumns).ToList();

        // Books: beta(25), Delta(5) | Electronics (case-insensitive): Gamma(15), Alpha(10)
        result.Select(x => x.Name).Should().Equal("beta", "Delta", "Gamma", "Alpha");
    }

    [Fact]
    public void ApplySort_ShouldUseCaseInsensitivePropertyMapping()
    {
        var query = TestData.AsQueryable();
        var propertyMapping = SortingHelper.CreatePropertyMapping<TestEntity>(
            ("categoryname", x => x.Category)
        );
        var sortColumns = new[]
        {
            new SortCriterion("CategoryName", "asc")
        };

        var result = query.ApplySort(sortColumns, propertyMapping).ToList();

        result.Select(x => x.Name).Should().Equal("Delta", "beta", "Gamma", "Alpha");
    }

    [Fact]
    public void ApplySort_ShouldPreserveNaturalTypesWithoutObjectBoxing()
    {
        var query = TestData.AsQueryable();
        var propertyMapping = SortingHelper.CreatePropertyMapping<TestEntity>(
            ("CustomPrice", x => x.Price * 2)
        );
        var sortColumns = new[] { new SortCriterion("CustomPrice", "asc") };

        var result = query.ApplySort(sortColumns, propertyMapping).ToList();

        // Sorted by Price * 2: Delta(10), Alpha(20), Gamma(30), beta(50)
        result.Select(x => x.Name).Should().Equal("Delta", "Alpha", "Gamma", "beta");
    }

    [Fact]
    public void ApplySort_ShouldHandleMixedMappedAndUnmappedColumns()
    {
        var query = TestData.AsQueryable();
        var propertyMapping = SortingHelper.CreatePropertyMapping<TestEntity>(
            ("CategoryLower", x => x.Category.ToLower())
        );
        var sortColumns = new[]
        {
            new SortCriterion("CategoryLower", "asc"),
            new SortCriterion("Name", "asc")
        };

        var result = query.ApplySort(sortColumns, propertyMapping).ToList();

        // books: beta, Delta | electronics: Alpha, Gamma
        result.Select(x => x.Name).Should().Equal("beta", "Delta", "Alpha", "Gamma");
    }

    [Fact]
    public void ApplySort_ShouldWorkWithComplexExpressions()
    {
        var query = TestData.AsQueryable();
        var propertyMapping = SortingHelper.CreatePropertyMapping<TestEntity>(
            ("ScoreFormula", x => x.Name.Length * x.Price)
        );

        var sortColumns = new[] { new SortCriterion("ScoreFormula", "desc") };

        var result = query.ApplySort(sortColumns, propertyMapping).ToList();

        // Name.Length * Price: beta(100), Gamma(75), Alpha(50), Delta(25)
        result.Select(x => x.Name).Should().Equal("beta", "Gamma", "Alpha", "Delta");
    }
}

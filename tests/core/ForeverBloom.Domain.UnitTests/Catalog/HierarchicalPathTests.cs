using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class HierarchicalPathTests
{
    [Theory]
    [InlineData("electronics", 1)]
    [InlineData("electronics.computers", 2)]
    [InlineData("electronics.computers.laptops", 3)]
    [InlineData("a", 1)]
    [InlineData("a.b.c.d.e.f.g.h.i.j", 10)]
    public void FromString_ShouldSucceed_ForValidPath(string path, int expectedDepth)
    {
        var result = HierarchicalPath.FromString(path);

        result.Should().BeSuccess();
        var hierarchicalPath = result.Value!;
        hierarchicalPath.Value.Should().Be(path);
        hierarchicalPath.Depth.Should().Be(expectedDepth);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void FromString_ShouldFail_ForNullOrWhitespaceInput(string? value)
    {
        var result = HierarchicalPath.FromString(value!);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.Empty>();
    }

    [Fact]
    public void FromString_ShouldFail_ForTooDeepPath()
    {
        // 11 segments (exceeds max depth of 10)
        var tooDeepPath = "a.b.c.d.e.f.g.h.i.j.k";

        var result = HierarchicalPath.FromString(tooDeepPath);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.TooDeep>();
    }

    [Theory]
    [InlineData("Invalid-Segment")]
    [InlineData("segment.UPPERCASE")]
    [InlineData("segment.with space")]
    [InlineData("segment.with_underscore")]
    [InlineData(".leading-dot")]
    [InlineData("trailing-dot.")]
    [InlineData("segment..double-dot")]
    public void FromString_ShouldFail_ForInvalidSegments(string path)
    {
        var result = HierarchicalPath.FromString(path);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.InvalidSegment>();
    }

    [Fact]
    public void FromString_ShouldFailWithMultipleErrors_ForTooDeepAndInvalidSegments()
    {
        // 11 segments with invalid format
        var path = "a.b.c.d.e.f.g.h.i.j.INVALID";

        var result = HierarchicalPath.FromString(path);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.TooDeep>();
        result.Should().HaveError<HierarchicalPathErrors.InvalidSegment>();
    }

    [Fact]
    public void FromParent_ShouldSucceed_ForValidChild()
    {
        var parentPath = HierarchicalPathFactory.Create("electronics");
        var childSlug = SlugFactory.Create("computers");

        var result = HierarchicalPath.FromParent(parentPath, childSlug);

        result.Should().BeSuccess();
        var childPath = result.Value!;
        childPath.Value.Should().Be("electronics.computers");
        childPath.Depth.Should().Be(2);
    }

    [Fact]
    public void FromParent_ShouldSucceed_ForDeepHierarchy()
    {
        var parent = HierarchicalPathFactory.Create("a.b.c.d.e.f.g.h.i");
        var childSlug = SlugFactory.Create("j");

        var result = HierarchicalPath.FromParent(parent, childSlug);

        result.Should().BeSuccess();
        var childPath = result.Value!;
        childPath.Depth.Should().Be(10);
    }

    [Fact]
    public void FromParent_ShouldFail_WhenDepthLimitExceeded()
    {
        var parent = HierarchicalPathFactory.Create("a.b.c.d.e.f.g.h.i.j"); // 10 segments (max)
        var childSlug = SlugFactory.Create("k");

        var result = HierarchicalPath.FromParent(parent, childSlug);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.TooDeep>();
    }

    [Fact]
    public void WithSlug_ShouldReplaceLastSegment_ForSingleSegmentPath()
    {
        var path = HierarchicalPathFactory.Create("electronics");
        var newSlug = SlugFactory.Create("clothing");

        var result = path.WithSlug(newSlug);

        result.Should().BeSuccess();
        var newPath = result.Value!;
        newPath.Value.Should().Be("clothing");
        newPath.Depth.Should().Be(1);
    }

    [Fact]
    public void WithSlug_ShouldReplaceLastSegment_ForMultiSegmentPath()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var newSlug = SlugFactory.Create("desktops");

        var result = path.WithSlug(newSlug);

        result.Should().BeSuccess();
        var newPath = result.Value!;
        newPath.Value.Should().Be("electronics.computers.desktops");
        newPath.Depth.Should().Be(3);
    }

    [Fact]
    public void WithSlug_ShouldPreserveDepth()
    {
        var path = HierarchicalPathFactory.Create("a.b.c.d.e");
        var newSlug = SlugFactory.Create("f");

        var result = path.WithSlug(newSlug);

        result.Should().BeSuccess();
        result.Value!.Depth.Should().Be(path.Depth);
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnTrue_ForDirectChild()
    {
        var parent = HierarchicalPathFactory.Create("electronics");
        var child = HierarchicalPathFactory.Create("electronics.computers");

        var isDescendant = child.IsDescendantOf(parent);

        isDescendant.Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnTrue_ForDeepDescendant()
    {
        var ancestor = HierarchicalPathFactory.Create("electronics");
        var descendant = HierarchicalPathFactory.Create("electronics.computers.laptops.gaming");

        var isDescendant = descendant.IsDescendantOf(ancestor);

        isDescendant.Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalse_ForSelf_WhenIncludeSelfIsFalse()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers");

        var isDescendant = path.IsDescendantOf(path, includeSelf: false);

        isDescendant.Should().BeFalse();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnTrue_ForSelf_WhenIncludeSelfIsTrue()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers");

        var isDescendant = path.IsDescendantOf(path, includeSelf: true);

        isDescendant.Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalse_ForUnrelatedPaths()
    {
        var path1 = HierarchicalPathFactory.Create("electronics.computers");
        var path2 = HierarchicalPathFactory.Create("clothing.shoes");

        var isDescendant = path1.IsDescendantOf(path2);

        isDescendant.Should().BeFalse();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalse_ForPartialSegmentMatch()
    {
        // Edge case: "electronics.co" should NOT be a descendant of "electronics.computers"
        var path1 = HierarchicalPathFactory.Create("electronics.computers");
        var path2 = HierarchicalPathFactory.Create("electronics.co");

        var isDescendant = path2.IsDescendantOf(path1);

        isDescendant.Should().BeFalse();
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalse_WhenPathIsParentOfOther()
    {
        var parent = HierarchicalPathFactory.Create("electronics");
        var child = HierarchicalPathFactory.Create("electronics.computers");

        // Parent is not a descendant of child
        var isDescendant = parent.IsDescendantOf(child);

        isDescendant.Should().BeFalse();
    }

    [Fact]
    public void Rebase_ShouldSucceed_ForValidRebase()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var oldBase = HierarchicalPathFactory.Create("electronics");
        var newBase = HierarchicalPathFactory.Create("tech");

        var result = HierarchicalPath.Rebase(path, oldBase, newBase);

        result.Should().BeSuccess();
        var rebasedPath = result.Value!;
        rebasedPath.Value.Should().Be("tech.computers.laptops");
    }

    [Fact]
    public void Rebase_ShouldSucceed_WhenPathEqualsOldBase()
    {
        var path = HierarchicalPathFactory.Create("electronics");
        var newBase = HierarchicalPathFactory.Create("tech");

        var result = HierarchicalPath.Rebase(path, path, newBase);

        result.Should().BeSuccess();
        var rebasedPath = result.Value!;
        rebasedPath.Value.Should().Be("tech");
    }

    [Fact]
    public void Rebase_ShouldReturnOriginalPath_WhenBasesAreEqual()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var basePath = HierarchicalPathFactory.Create("electronics");

        var result = HierarchicalPath.Rebase(path, basePath, basePath);

        result.Should().BeSuccess();
        var rebasedPath = result.Value!;
        rebasedPath.Value.Should().Be(path.Value);
    }

    [Fact]
    public void Rebase_ShouldFail_WhenOldBaseIsNotAncestor()
    {
        var path = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var oldBase = HierarchicalPathFactory.Create("clothing");
        var newBase = HierarchicalPathFactory.Create("tech");

        var result = HierarchicalPath.Rebase(path, oldBase, newBase);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.OldBaseNotAncestor>();
    }

    [Fact]
    public void Rebase_ShouldFail_WhenRebasedPathExceedsMaxDepth()
    {
        var path = HierarchicalPathFactory.Create("a.b.c.d.e");
        var oldBase = HierarchicalPathFactory.Create("a");
        var newBase = HierarchicalPathFactory.Create("x.y.z.w.v.u.t");

        var result = HierarchicalPath.Rebase(path, oldBase, newBase);

        result.Should().BeFailure();
        result.Should().HaveError<HierarchicalPathErrors.TooDeep>();
    }

    [Fact]
    public void Rebase_ShouldSucceed_ForDeepHierarchyWithinLimit()
    {
        var path = HierarchicalPathFactory.Create("a.b.c.d.e.f");
        var oldBase = HierarchicalPathFactory.Create("a.b");
        var newBase = HierarchicalPathFactory.Create("x.y.z");

        var result = HierarchicalPath.Rebase(path, oldBase, newBase);

        result.Should().BeSuccess();
        var rebasedPath = result.Value!;
        rebasedPath.Value.Should().Be("x.y.z.c.d.e.f");
        rebasedPath.Depth.Should().Be(7);
    }

    [Fact]
    public void HierarchicalPath_ShouldHaveStructuralEquality()
    {
        var samePath1 = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var samePath2 = HierarchicalPathFactory.Create("electronics.computers.laptops");
        var differentPath = HierarchicalPathFactory.Create("electronics.computers.desktops");

        samePath1.Should().Be(samePath2);
        (samePath1 == samePath2).Should().BeTrue();
        samePath1.Should().NotBe(differentPath);
        (samePath1 == differentPath).Should().BeFalse();
    }
}

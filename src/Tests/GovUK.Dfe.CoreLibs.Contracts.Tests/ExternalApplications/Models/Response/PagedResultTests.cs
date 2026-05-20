using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.Contracts.Tests.ExternalApplications.Models.Response;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_DefaultValues_ShouldHaveEmptyItemsAndZeroNumericProperties()
    {
        var result = new PagedResult<string>();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(0);
        result.PageSize.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PagedResult_WithAllProperties_ShouldSetAllValues()
    {
        var items = new List<string> { "a", "b", "c" };

        var result = new PagedResult<string>
        {
            Items = items,
            TotalCount = 30,
            PageNumber = 2,
            PageSize = 10,
            TotalPages = 3
        };

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(30);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PagedResult_Items_ShouldBeIReadOnlyCollection()
    {
        var result = new PagedResult<int>();

        result.Items.Should().BeAssignableTo<IReadOnlyCollection<int>>();
    }

    [Fact]
    public void PagedResult_Serialization_ShouldUseJsonPropertyNames()
    {
        var result = new PagedResult<string>
        {
            Items = new List<string> { "item1" },
            TotalCount = 10,
            PageNumber = 1,
            PageSize = 5,
            TotalPages = 2
        };

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("items", out _).Should().BeTrue();
        root.TryGetProperty("totalCount", out _).Should().BeTrue();
        root.TryGetProperty("pageNumber", out _).Should().BeTrue();
        root.TryGetProperty("pageSize", out _).Should().BeTrue();
        root.TryGetProperty("totalPages", out _).Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Deserialization_ShouldMapFromJsonPropertyNames()
    {
        const string json = """
            {
                "items": ["x", "y"],
                "totalCount": 20,
                "pageNumber": 3,
                "pageSize": 10,
                "totalPages": 2
            }
            """;

        var result = JsonSerializer.Deserialize<PagedResult<string>>(json);

        result.Should().NotBeNull();
        result!.Items.Should().BeEquivalentTo(new[] { "x", "y" });
        result.TotalCount.Should().Be(20);
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void PagedResult_Serialization_ShouldNotIncludePascalCasePropertyNames()
    {
        var result = new PagedResult<string> { TotalCount = 5 };

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("TotalCount", out _).Should().BeFalse();
        root.TryGetProperty("Items", out _).Should().BeFalse();
        root.TryGetProperty("PageNumber", out _).Should().BeFalse();
        root.TryGetProperty("PageSize", out _).Should().BeFalse();
        root.TryGetProperty("TotalPages", out _).Should().BeFalse();
    }

    [Fact]
    public void PagedResult_WithGenericValueType_ShouldSupportIntItems()
    {
        var items = new List<int> { 1, 2, 3 };

        var result = new PagedResult<int>
        {
            Items = items,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1
        };

        result.Items.Should().HaveCount(3);
        result.Items.Should().ContainInOrder(1, 2, 3);
    }
}

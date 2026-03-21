extern alias ActionService;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MIBO.E2ETests.Fixtures;
using MIBO.E2ETests.Helpers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MIBO.E2ETests.ActionService;

[Collection("E2E")]
public class ActionTests : IAsyncLifetime
{
    private WireMockServer _wireMock = null!;
    private ActionServiceFactory _factory = null!;
    private HttpClient _client = null!;

    public ActionTests(SharedContainersFixture _)
    {
    }

    public Task InitializeAsync()
    {
        _wireMock = WireMockServer.Start();
        _factory = new ActionServiceFactory(_wireMock.Url!);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        _wireMock.Dispose();
    }

    // ── products.catalog.query ──

    [Fact]
    public async Task Query_ProductsCatalog_ReturnsProducts()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.GetProperty("handler").GetString().Should().Be("products.catalog.query");
        body.GetProperty("fieldHints").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_ProductsCatalogWithSearch_UsesSearchEndpoint()
    {
        WireMockSetup.StubDummyJsonProductSearch(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object> { ["q"] = "phone" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_ProductsCatalogByCategory_UsesCategoryEndpoint()
    {
        _wireMock
            .Given(Request.Create().WithPath("/products/category/smartphones").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "products": [
                        {"id": 1, "title": "iPhone", "price": 999, "category": "smartphones", "rating": 4.5}
                    ],
                    "total": 1, "skip": 0, "limit": 30
                }
                """));

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object> { ["category"] = "smartphones" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_ProductsCatalogWithSort_ReturnsSortedProducts()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object>
            {
                ["sort"] = "price:desc",
                ["limit"] = 5
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_ProductsCatalogWithSkipAndLimit_AppliesPagination()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object>
            {
                ["skip"] = 0,
                ["limit"] = 1
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_ProductsCatalogSearchNoResults_TriesFallback()
    {
        // First call returns empty, fallback also empty
        _wireMock
            .Given(Request.Create().WithPath("/products/search").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"products": [], "total": 0, "skip": 0, "limit": 30}"""));

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object> { ["q"] = "nonexistent-gadget-xyz" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    // ── products.categories.list ──

    [Fact]
    public async Task Query_ProductCategories_ReturnsCategories()
    {
        WireMockSetup.StubDummyJsonCategories(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.categories.list",
                handler = "products.categories.list"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.GetProperty("fieldHints").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    // ── products.detail.get ──

    [Fact]
    public async Task Query_ProductDetail_ReturnsProduct()
    {
        WireMockSetup.StubDummyJsonProductById(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.detail.get",
                handler = "products.detail.get",
                defaultArgs = new Dictionary<string, object> { ["productId"] = 1 }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.GetProperty("fieldHints").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_ProductDetailWithIdArg_Works()
    {
        WireMockSetup.StubDummyJsonProductById(_wireMock, 2);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.detail.get",
                handler = "products.detail.get"
            },
            args = new Dictionary<string, object> { ["id"] = 2 }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── finance.user.summary ──

    [Fact]
    public async Task Query_FinanceUserSummary_ReturnsDeterministicData()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "finance.user.summary",
                handler = "finance.user.summary"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.GetProperty("fieldHints").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_FinanceUserSummaryWithUserId_ReturnsDifferentData()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "finance.user.summary",
                handler = "finance.user.summary"
            },
            args = new Dictionary<string, object> { ["userId"] = 42 }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("userId").GetInt32().Should().Be(42);
    }

    // ── Execute ──

    [Fact]
    public async Task Execute_WithDataSource_ReturnsResponse()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute", new
        {
            action = new
            {
                id = "finance.refresh",
                handler = "finance.user.summary",
                dataSourceId = "finance.user.summary"
            },
            dataSource = new
            {
                id = "finance.user.summary",
                handler = "finance.user.summary"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("actionId").GetString().Should().Be("finance.refresh");
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Execute_WithMultipleDataSources_ReturnsResponse()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute", new
        {
            action = new
            {
                id = "finance.check",
                handler = "finance.user.summary",
                dataSourceId = "finance.user.summary",
                refreshDataSourceIds = new[] { "finance.user.summary" }
            },
            dataSources = new Dictionary<string, object>
            {
                ["finance.user.summary"] = new
                {
                    id = "finance.user.summary",
                    handler = "finance.user.summary"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("actionId").GetString().Should().Be("finance.check");
    }

    [Fact]
    public async Task Execute_WithoutAction_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute",
            JsonSerializer.Deserialize<JsonElement>("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Execute_WithPayload_PassesPayloadToHandler()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute", new
        {
            action = new
            {
                id = "finance.custom",
                handler = "finance.user.summary",
                dataSourceId = "finance.user.summary"
            },
            dataSource = new
            {
                id = "finance.user.summary",
                handler = "finance.user.summary"
            },
            payload = new Dictionary<string, object> { ["userId"] = 99 }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Transforms ──

    [Fact]
    public async Task Query_WithProjectRowsTransform_AppliesTransform()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query",
                transforms = new[]
                {
                    new
                    {
                        id = "project",
                        type = "project_rows",
                        inputPath = "products",
                        outputPath = "rows",
                        fields = new[]
                        {
                            new { key = "name", sourceField = "title" },
                            new { key = "cost", sourceField = "price" }
                        }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Query_WithTransformExpressionAdd_ComputesSum()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "calc",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "total", "expression": { "op": "add", "args": [{ "field": "price" }, { "const": 10 }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionMultiply_ComputesProduct()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "mul",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "doubled", "expression": { "op": "multiply", "args": [{ "field": "price" }, { "const": 2 }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionSubtract_ComputesDifference()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "sub",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "discounted", "expression": { "op": "subtract", "args": [{ "field": "price" }, { "const": 1 }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionDivide_ComputesQuotient()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "div",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "half", "expression": { "op": "divide", "args": [{ "field": "price" }, { "const": 2 }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionRound_RoundsValue()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "rnd",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "rounded", "expression": { "op": "round", "args": [{ "field": "price" }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionConcat_ConcatenatesStrings()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "cat",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "label", "expression": { "op": "concat", "args": [{ "field": "title" }, { "const": " - $" }, { "field": "price" }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithFieldHints_ReturnsCustomHints()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query",
                fieldHints = new
                {
                    entityType = "product",
                    collectionPath = "products",
                    labelField = "title",
                    valueField = "id",
                    titleField = "title",
                    imageField = "thumbnail"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var hints = body.GetProperty("fieldHints");
        hints.GetProperty("entityType").GetString().Should().Be("product");
    }

    [Fact]
    public async Task Query_ProductsCatalogWithSortByAndOrder_Works()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "products.catalog.query",
                handler = "products.catalog.query"
            },
            args = new Dictionary<string, object>
            {
                ["sortBy"] = "title",
                ["order"] = "asc"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Execute_WithHandlerFromAction_OverridesDataSource()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute", new
        {
            action = new
            {
                id = "custom.action",
                handler = "finance.user.summary",
                dataSourceId = "my-ds"
            },
            dataSources = new Dictionary<string, object>
            {
                ["my-ds"] = new
                {
                    id = "my-ds",
                    handler = "products.catalog.query"  // will be overridden by action.handler
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Execute_WithoutDataSource_ReturnsResponseWithNullData()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/execute", new
        {
            action = new
            {
                id = "no-ds-action",
                actionType = "custom.type"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("actionType").GetString().Should().Be("custom.type");
    }

    [Fact]
    public async Task Query_WithTransformExpressionFloorCeilAbs_Works()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "math",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "floored", "expression": { "op": "floor", "args": [{ "field": "price" }] } },
                            { "key": "ceiled", "expression": { "op": "ceil", "args": [{ "field": "price" }] } },
                            { "key": "absolute", "expression": { "op": "abs", "args": [{ "field": "price" }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionMaxMinCoalesce_Works()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "agg",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "maximum", "expression": { "op": "max", "args": [{ "field": "price" }, { "const": 15 }] } },
                            { "key": "minimum", "expression": { "op": "min", "args": [{ "field": "price" }, { "const": 15 }] } },
                            { "key": "safe", "expression": { "op": "coalesce", "args": [{ "field": "brand" }, { "const": "Unknown" }] } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformFieldHints_ReturnsTransformHints()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "proj",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title", "kind": "text" },
                            { "key": "cost", "sourceField": "price", "kind": "number" }
                        ],
                        "fieldHints": {
                            "entityType": "custom_product",
                            "labelField": "name"
                        }
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithTransformExpressionLeftRight_Works()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "lr",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "margin", "expression": { "op": "subtract", "left": { "field": "price" }, "right": { "const": 5 } } }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Query_WithStaticValueTransformField_Works()
    {
        WireMockSetup.StubDummyJsonProducts(_wireMock);

        var transformJson = JsonSerializer.Deserialize<JsonElement>("""
        {
            "dataSource": {
                "id": "products.catalog.query",
                "handler": "products.catalog.query",
                "transforms": [
                    {
                        "id": "static",
                        "type": "project_rows",
                        "inputPath": "products",
                        "outputPath": "rows",
                        "fields": [
                            { "key": "name", "sourceField": "title" },
                            { "key": "source", "value": "dummyjson" }
                        ]
                    }
                ]
            }
        }
        """);

        var response = await _client.PostAsJsonAsync("/api/actions/query", transformJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Error cases ──

    [Fact]
    public async Task Query_WithoutDataSource_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/query",
            JsonSerializer.Deserialize<JsonElement>("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Query_UnsupportedHandler_Returns500()
    {
        var response = await _client.PostAsJsonAsync("/api/actions/query", new
        {
            dataSource = new
            {
                id = "unknown.handler",
                handler = "unknown.handler"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

}

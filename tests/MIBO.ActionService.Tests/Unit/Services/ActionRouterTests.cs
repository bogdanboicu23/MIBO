using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.ExternalServices.DummyJson;
using MIBO.ActionService.Services;
using Moq;

namespace MIBO.ActionService.Tests.Unit.Services;

public class ActionRouterTests
{
    private readonly ActionRouter _sut;
    private readonly RichMockHttpMessageHandler _handler;

    public ActionRouterTests()
    {
        _handler = new RichMockHttpMessageHandler();
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://dummyjson.com/") };
        var dummyJsonClient = new DummyJsonClient(httpClient);
        var dummyJsonHandler = new DummyJsonActionHandler(dummyJsonClient);

        _sut = new ActionRouter(new IExternalDataSourceHandler[] { dummyJsonHandler });
    }

    // ════════════════════════════════════════════
    //  QueryAsync — Finance Handler
    // ════════════════════════════════════════════

    #region QueryAsync_Finance

    [Fact]
    public async Task QueryAsync_FinanceHandler_ReturnsSummaryData()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "finance-ds",
            Handler = "finance.user.summary",
            DefaultArgs = new Dictionary<string, object?> { ["userId"] = 1 }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.DataSourceId.Should().Be("finance-ds");
        result.Handler.Should().Be("finance.user.summary");
        result.Data.Should().NotBeNull();
        result.FetchedAtUtc.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task QueryAsync_FinanceHandler_FieldHintsAreInferred()
    {
        var dataSource = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.FieldHints.Should().NotBeNull();
        result.FieldHints!.EntityType.Should().Be("finance_summary");
        result.FieldHints.LabelField.Should().Be("label");
        result.FieldHints.ValueField.Should().Be("value");
    }

    [Fact]
    public async Task QueryAsync_FinanceHandler_ContainsBalanceIncomeExpensesSavings()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            DefaultArgs = new Dictionary<string, object?> { ["userId"] = 3 }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("balance");
        json.Should().Contain("income");
        json.Should().Contain("expenses");
        json.Should().Contain("savings");
        json.Should().Contain("items");
        json.Should().Contain("chart");
    }

    [Fact]
    public async Task QueryAsync_FinanceHandler_DifferentUserIds_ProduceDifferentValues()
    {
        var ds1 = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };
        var ds2 = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };

        var result1 = await _sut.QueryAsync(ds1, new Dictionary<string, object?> { ["userId"] = 1 }, CancellationToken.None);
        var result2 = await _sut.QueryAsync(ds2, new Dictionary<string, object?> { ["userId"] = 99 }, CancellationToken.None);

        var json1 = JsonSerializer.Serialize(result1.Data);
        var json2 = JsonSerializer.Serialize(result2.Data);
        json1.Should().NotBe(json2);
    }

    [Fact]
    public async Task QueryAsync_FinanceHandler_FieldHintsHaveFields()
    {
        var dataSource = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.FieldHints!.Fields.Should().NotBeEmpty();
        result.FieldHints.NumericFields.Should().NotBeEmpty();
        result.FieldHints.TextFields.Should().NotBeEmpty();
    }

    #endregion

    // ════════════════════════════════════════════
    //  QueryAsync — Product Handlers
    // ════════════════════════════════════════════

    #region QueryAsync_Products

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_ReturnsProductData()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query"
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.DataSourceId.Should().Be("products");
        result.Handler.Should().Be("products.catalog.query");
        result.Data.Should().NotBeNull();
        result.FieldHints!.EntityType.Should().Be("product_list");
    }

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_WithSearchQuery()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query",
            DefaultArgs = new Dictionary<string, object?> { ["q"] = "phone" }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("items");
    }

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_WithCategory()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query",
            DefaultArgs = new Dictionary<string, object?> { ["category"] = "electronics" }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_WithSortBy()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query",
            DefaultArgs = new Dictionary<string, object?>
            {
                ["sortBy"] = "price",
                ["order"] = "desc"
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("\"order\"");
    }

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_WithSortString()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query",
            DefaultArgs = new Dictionary<string, object?>
            {
                ["sort"] = "price:desc"
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_ProductsCatalogQuery_WithLimitAndSkip()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "products",
            Handler = "products.catalog.query",
            DefaultArgs = new Dictionary<string, object?>
            {
                ["limit"] = 5,
                ["skip"] = 10
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_ProductsCategoriesList_ReturnsCategoriesData()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "cats",
            Handler = "products.categories.list"
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.DataSourceId.Should().Be("cats");
        result.FieldHints!.EntityType.Should().Be("category_list");
        result.Data.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("categories");
        json.Should().Contain("items");
    }

    [Fact]
    public async Task QueryAsync_ProductsDetailGet_ReturnsProduct()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "detail",
            Handler = "products.detail.get",
            DefaultArgs = new Dictionary<string, object?> { ["productId"] = 1 }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.DataSourceId.Should().Be("detail");
        result.FieldHints!.EntityType.Should().Be("product");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_ProductsDetailGet_NoProductId_Throws()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "detail",
            Handler = "products.detail.get"
            // no productId
        };

        var act = async () => await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*productId*");
    }

    #endregion

    // ════════════════════════════════════════════
    //  QueryAsync — General
    // ════════════════════════════════════════════

    #region QueryAsync_General

    [Fact]
    public async Task QueryAsync_EmptyHandler_FallsBackToId()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "finance.user.summary",
            Handler = ""
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Handler.Should().Be("finance.user.summary");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_MergesDefaultArgsWithRequestArgs()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            DefaultArgs = new Dictionary<string, object?> { ["userId"] = 1 }
        };
        var requestArgs = new Dictionary<string, object?> { ["userId"] = 5 };

        var result = await _sut.QueryAsync(dataSource, requestArgs, CancellationToken.None);

        result.Data.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("\"userId\"");
    }

    [Fact]
    public async Task QueryAsync_ManualFieldHints_MergeWithInferred()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            FieldHints = new DataFieldHints
            {
                EntityType = "custom_entity",
                LabelField = "custom_label"
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.FieldHints!.EntityType.Should().Be("custom_entity");
        result.FieldHints.LabelField.Should().Be("custom_label");
        result.FieldHints.ValueField.Should().Be("value");
    }

    [Fact]
    public async Task QueryAsync_UnsupportedHandler_ThrowsInvalidOperation()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "unknown",
            Handler = "not.a.real.handler"
        };

        var act = async () => await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported*not.a.real.handler*");
    }

    [Fact]
    public async Task QueryAsync_NoTransforms_ReturnsRawData()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary"
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransforms_ProjectRows()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    OutputPath = "rows",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "name", SourceField = "label" },
                        new() { Key = "amount", SourceField = "value" }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result.Data);
        json.Should().Contain("rows");
    }

    [Fact]
    public async Task QueryAsync_WithTransformExpression_Add()
    {
        var addExpr = JsonSerializer.SerializeToElement(new
        {
            op = "add",
            args = new object[] { new { field = "value" }, new { @const = 100 } }
        });

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    OutputPath = "rows",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "label", SourceField = "label" },
                        new() { Key = "boosted", Expression = addExpr }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransformExpression_Subtract()
    {
        var expr = JsonSerializer.SerializeToElement(new
        {
            op = "subtract",
            left = new { field = "value" },
            right = new { @const = 10 }
        });

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "result", Expression = expr }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransformExpression_MultiplyDivide()
    {
        var mulExpr = JsonSerializer.SerializeToElement(new
        {
            op = "multiply",
            args = new object[] { new { field = "value" }, new { @const = 2 } }
        });
        var divExpr = JsonSerializer.SerializeToElement(new
        {
            op = "divide",
            args = new object[] { new { field = "value" }, new { @const = 3 } }
        });

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "doubled", Expression = mulExpr },
                        new() { Key = "thirded", Expression = divExpr }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransformExpression_RoundFloorCeilAbs()
    {
        var roundExpr = JsonSerializer.SerializeToElement(new
        {
            op = "round",
            args = new object[] { new { field = "value" }, new { @const = 2 } }
        });
        var floorExpr = JsonSerializer.SerializeToElement(new
        {
            op = "floor",
            args = new object[] { new { field = "value" } }
        });
        var ceilExpr = JsonSerializer.SerializeToElement(new
        {
            op = "ceil",
            args = new object[] { new { field = "value" } }
        });
        var absExpr = JsonSerializer.SerializeToElement(new
        {
            op = "abs",
            args = new object[] { new { field = "value" } }
        });

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "rounded", Expression = roundExpr },
                        new() { Key = "floored", Expression = floorExpr },
                        new() { Key = "ceiled", Expression = ceilExpr },
                        new() { Key = "absoluted", Expression = absExpr }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransformExpression_MaxMinConcatCoalesce()
    {
        var maxExpr = JsonSerializer.SerializeToElement(new
        {
            op = "max",
            args = new object[] { new { @const = 10 }, new { @const = 20 } }
        });
        var minExpr = JsonSerializer.SerializeToElement(new
        {
            op = "min",
            args = new object[] { new { @const = 10 }, new { @const = 20 } }
        });
        var concatExpr = JsonSerializer.SerializeToElement(new
        {
            op = "concat",
            args = new object[] { new { field = "label" }, new { @const = " - done" } }
        });
        var coalesceExpr = JsonSerializer.SerializeToElement(new
        {
            op = "coalesce",
            args = new object[] { new { field = "label" }, new { @const = "default" } }
        });

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "maxVal", Expression = maxExpr },
                        new() { Key = "minVal", Expression = minExpr },
                        new() { Key = "label", Expression = concatExpr },
                        new() { Key = "fallback", Expression = coalesceExpr }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WithTransform_StaticValue()
    {
        var staticVal = JsonSerializer.SerializeToElement("constant-text");

        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new()
                {
                    Type = "project_rows",
                    InputPath = "items",
                    Fields = new List<DataTransformFieldDefinition>
                    {
                        new() { Key = "tag", Value = staticVal }
                    }
                }
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_EmptyTransformType_IsSkipped()
    {
        var dataSource = new DataSourceDefinition
        {
            Id = "fin",
            Handler = "finance.user.summary",
            Transforms = new List<DataTransformDefinition>
            {
                new() { Type = "" } // empty type, should be skipped
            }
        };

        var result = await _sut.QueryAsync(dataSource, null, CancellationToken.None);
        result.Data.Should().NotBeNull();
    }

    #endregion

    // ════════════════════════════════════════════
    //  ExecuteAsync
    // ════════════════════════════════════════════

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_SetsActionIdAndType()
    {
        var action = new ActionDefinition
        {
            Id = "my-action",
            ActionType = "custom.type",
            DataSourceId = "fin",
            Handler = "finance.user.summary"
        };
        var dataSource = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };

        var result = await _sut.ExecuteAsync(action, dataSource, null, null, CancellationToken.None);

        result.ActionId.Should().Be("my-action");
        result.ActionType.Should().Be("custom.type");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyActionType_DefaultsToUiActionExecute()
    {
        var action = new ActionDefinition
        {
            Id = "act1",
            ActionType = "",
            DataSourceId = "fin",
            Handler = "finance.user.summary"
        };
        var dataSource = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" };

        var result = await _sut.ExecuteAsync(action, dataSource, null, null, CancellationToken.None);

        result.ActionType.Should().Be("ui.action.execute");
    }

    [Fact]
    public async Task ExecuteAsync_NoDataSource_ReturnsNullData()
    {
        var action = new ActionDefinition { Id = "act-no-ds", DataSourceId = null };

        var result = await _sut.ExecuteAsync(action, null, null, null, CancellationToken.None);

        result.ActionId.Should().Be("act-no-ds");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesDataSourceFromDictionary()
    {
        var action = new ActionDefinition { Id = "act1", DataSourceId = "fin" };
        var dataSources = new Dictionary<string, DataSourceDefinition>
        {
            ["fin"] = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" }
        };

        var result = await _sut.ExecuteAsync(action, null, dataSources, null, CancellationToken.None);

        result.Data.Should().NotBeNull();
        result.DataSourceId.Should().Be("fin");
    }

    [Fact]
    public async Task ExecuteAsync_WithPayload_MergesIntoArgs()
    {
        var action = new ActionDefinition
        {
            Id = "act1",
            Handler = "finance.user.summary",
            DefaultArgs = new Dictionary<string, object?> { ["userId"] = 1 }
        };
        var payload = new Dictionary<string, object?> { ["userId"] = 42 };

        var result = await _sut.ExecuteAsync(action, null, null, payload, CancellationToken.None);

        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithRefreshDataSourceIds_IncludesRefreshes()
    {
        var action = new ActionDefinition
        {
            Id = "act1",
            Handler = "finance.user.summary",
            RefreshDataSourceIds = new List<string> { "fin" }
        };

        var dataSources = new Dictionary<string, DataSourceDefinition>
        {
            ["fin"] = new DataSourceDefinition { Id = "fin", Handler = "finance.user.summary" }
        };

        var result = await _sut.ExecuteAsync(action, null, dataSources, null, CancellationToken.None);

        result.Refreshes.Should().HaveCount(1);
        result.Refreshes[0].DataSourceId.Should().Be("fin");
    }

    [Fact]
    public async Task ExecuteAsync_ActionHandlerOverridesDataSourceHandler()
    {
        var action = new ActionDefinition
        {
            Id = "act1",
            Handler = "finance.user.summary",
            DataSourceId = "ds1"
        };
        var dataSource = new DataSourceDefinition
        {
            Id = "ds1",
            Handler = "products.catalog.query" // this should be overridden by action.Handler
        };

        var result = await _sut.ExecuteAsync(action, dataSource, null, null, CancellationToken.None);

        // Action handler "finance.user.summary" should have been used
        result.Data.Should().NotBeNull();
    }

    #endregion

    // ════════════════════════════════════════════
    //  Model Tests
    // ════════════════════════════════════════════

    #region Models

    [Fact]
    public void DataSourceDefinition_DefaultValues()
    {
        var ds = new DataSourceDefinition();

        ds.Id.Should().BeEmpty();
        ds.Handler.Should().BeEmpty();
        ds.DefaultArgs.Should().BeEmpty();
        ds.RefreshOnLoad.Should().BeFalse();
        ds.RefreshOnConversationOpen.Should().BeFalse();
        ds.StaleAfterMs.Should().BeNull();
        ds.FieldHints.Should().BeNull();
        ds.Transforms.Should().BeEmpty();
    }

    [Fact]
    public void ActionDefinition_DefaultValues()
    {
        var action = new ActionDefinition();

        action.Id.Should().BeEmpty();
        action.ActionType.Should().Be("ui.action.execute");
        action.Handler.Should().BeEmpty();
        action.DataSourceId.Should().BeNull();
        action.DefaultArgs.Should().BeEmpty();
        action.RefreshDataSourceIds.Should().BeEmpty();
    }

    [Fact]
    public void DataFieldHints_DefaultValues()
    {
        var hints = new DataFieldHints();

        hints.EntityType.Should().BeNull();
        hints.CollectionPath.Should().BeNull();
        hints.LabelField.Should().BeNull();
        hints.ValueField.Should().BeNull();
        hints.DefaultTableFields.Should().BeEmpty();
        hints.TextFields.Should().BeEmpty();
        hints.NumericFields.Should().BeEmpty();
        hints.Fields.Should().BeEmpty();
    }

    [Fact]
    public void DataTransformDefinition_DefaultValues()
    {
        var transform = new DataTransformDefinition();

        transform.Id.Should().BeNull();
        transform.Type.Should().BeEmpty();
        transform.InputPath.Should().BeNull();
        transform.OutputPath.Should().BeNull();
        transform.Fields.Should().BeEmpty();
        transform.FieldHints.Should().BeNull();
    }

    [Fact]
    public void DataTransformFieldDefinition_DefaultValues()
    {
        var field = new DataTransformFieldDefinition();

        field.Key.Should().BeEmpty();
        field.Label.Should().BeNull();
        field.SourceField.Should().BeNull();
        field.Value.Should().BeNull();
        field.Expression.Should().BeNull();
        field.Kind.Should().BeNull();
    }

    [Fact]
    public void DataFieldDescriptor_DefaultValues()
    {
        var desc = new DataFieldDescriptor();

        desc.Name.Should().BeEmpty();
        desc.Kind.Should().Be("text");
    }

    [Fact]
    public void QueryResponse_DefaultValues()
    {
        var resp = new QueryResponse();

        resp.DataSourceId.Should().BeEmpty();
        resp.Handler.Should().BeEmpty();
        resp.FetchedAtUtc.Should().BeEmpty();
    }

    [Fact]
    public void ActionExecutionResponse_DefaultValues()
    {
        var resp = new ActionExecutionResponse();

        resp.ActionId.Should().BeEmpty();
        resp.ActionType.Should().BeEmpty();
        resp.DataSourceId.Should().BeNull();
        resp.Data.Should().BeNull();
        resp.FieldHints.Should().BeNull();
        resp.FetchedAtUtc.Should().BeNull();
        resp.Refreshes.Should().BeEmpty();
    }

    #endregion
}

/// <summary>
/// Rich mock HTTP handler that returns realistic product data for coverage.
/// </summary>
public class RichMockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var query = request.RequestUri?.Query ?? "";

        var responseBody = path switch
        {
            var p when p.Contains("/products/categories") =>
                """["electronics","beauty","fragrances","furniture"]""",

            var p when p.Contains("/products/search") =>
                BuildProductListResponse(query),

            var p when p.Contains("/products/category/") =>
                BuildProductListResponse(query),

            var p when System.Text.RegularExpressions.Regex.IsMatch(p, @"/products/\d+$") =>
                BuildSingleProductResponse(),

            var p when p.Contains("/products") =>
                BuildProductListResponse(query),

            _ => "{}"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }

    private static string BuildProductListResponse(string query)
    {
        return """
        {
            "products": [
                {
                    "id": 1,
                    "title": "iPhone 15",
                    "description": "An Apple smartphone",
                    "price": 999.99,
                    "discountPercentage": 5.5,
                    "rating": 4.8,
                    "stock": 50,
                    "brand": "Apple",
                    "category": "smartphones",
                    "thumbnail": "https://cdn.example.com/phone.jpg",
                    "images": ["https://cdn.example.com/phone1.jpg", "https://cdn.example.com/phone2.png"]
                },
                {
                    "id": 2,
                    "title": "Samsung Galaxy",
                    "description": "A Samsung smartphone",
                    "price": 799.50,
                    "discountPercentage": 3.0,
                    "rating": 4.5,
                    "stock": 30,
                    "brand": "Samsung",
                    "category": "smartphones",
                    "thumbnail": "https://cdn.example.com/galaxy.webp",
                    "images": ["https://cdn.example.com/galaxy1.jpeg"]
                }
            ],
            "total": 2,
            "skip": 0,
            "limit": 10
        }
        """;
    }

    private static string BuildSingleProductResponse()
    {
        return """
        {
            "id": 1,
            "title": "iPhone 15",
            "description": "An Apple smartphone",
            "price": 999.99,
            "discountPercentage": 5.5,
            "rating": 4.8,
            "stock": 50,
            "brand": "Apple",
            "category": "smartphones",
            "thumbnail": "https://cdn.example.com/phone.jpg",
            "images": ["https://cdn.example.com/phone1.jpg"]
        }
        """;
    }
}

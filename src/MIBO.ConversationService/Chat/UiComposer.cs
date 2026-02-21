using System.Text.Json.Nodes;

namespace MIBO.ConversationService.Chat;

public sealed class UiComposer
{
    public (JsonNode uiV1, JsonNode bindings, JsonNode subscriptions) Compose(
        string conversationId,
        string uiInstanceId,
        string userId,
        JsonNode? budget,
        JsonNode? summary,
        JsonNode? products)
    {
        // ui.v1 (generic, global registry pe FE)
        var ui = new JsonObject
        {
            ["version"] = "ui.v1",
            ["uiInstanceId"] = uiInstanceId,
            ["title"] = "Buget & Produse",
            ["layout"] = new JsonObject
            {
                ["type"] = "column",
                ["gap"] = 12
            },
            ["components"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "kpi.available",
                    ["type"] = "kpiCard",
                    ["props"] = new JsonObject { ["label"] = "Disponibil" },
                    ["binding"] = new JsonObject
                    {
                        ["source"] = "step:budget",
                        ["path"] = "availableAmount"
                    }
                },
                new JsonObject
                {
                    ["id"] = "chart.incomeExpense",
                    ["type"] = "pieChart",
                    ["props"] = new JsonObject
                    {
                        ["title"] = "Venituri vs Cheltuieli (30 zile)"
                    },
                    ["binding"] = new JsonObject
                    {
                        ["source"] = "step:summary30",
                        ["path"] = "" // whole object
                    },
                    ["transform"] = new JsonObject
                    {
                        ["type"] = "incomeExpenseToPie",
                        ["incomeField"] = "incomeTotal",
                        ["expenseField"] = "expenseTotal"
                    }
                },
                new JsonObject
                {
                    ["id"] = "carousel.products",
                    ["type"] = "productCarousel",
                    ["props"] = new JsonObject
                    {
                        ["title"] = "Produse în bugetul tău",
                        ["maxItems"] = 12
                    },
                    ["binding"] = new JsonObject
                    {
                        ["source"] = "step:products",
                        ["path"] = "items"
                    },
                    ["actions"] = new JsonObject
                    {
                        ["onBuy"] = new JsonObject
                        {
                            ["type"] = "shop.buy",
                            ["payload"] = new JsonObject
                            {
                                ["productId"] = "{{item.id}}",
                                ["price"] = "{{item.price}}"
                            }
                        }
                    }
                }
            }
        };

        // bindings (pentru refresh fără LLM)
        var bindings = new JsonObject
        {
            ["version"] = "ui.bindings.v1",
            ["uiInstanceId"] = uiInstanceId,
            ["steps"] = new JsonObject
            {
                ["budget"] = new JsonObject { ["tool"] = "finance.getBudgetSnapshot", ["args"] = new JsonObject() },
                ["summary30"] = new JsonObject { ["tool"] = "finance.getIncomeExpenseSummary", ["args"] = new JsonObject { ["rangeDays"] = 30 } },
                ["products"] = new JsonObject { ["tool"] = "shop.searchProducts", ["args"] = new JsonObject { ["maxPrice"] = "{{step:budget.availableAmount}}" } }
            }
        };

        // subscriptions (event-driven refresh)
        var subs = new JsonObject
        {
            ["version"] = "ui.subscriptions.v1",
            ["uiInstanceId"] = uiInstanceId,
            ["topics"] = new JsonArray
            {
                new JsonObject
                {
                    ["topic"] = "finance.expense_created",
                    ["refreshSteps"] = new JsonArray { "budget", "summary30", "products" },
                    ["patchTargets"] = new JsonArray { "kpi.available", "chart.incomeExpense", "carousel.products" }
                }
            }
        };

        return (ui, bindings, subs);
    }
}

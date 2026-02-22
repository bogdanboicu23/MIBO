using System.Text;
using System.Text.Json.Nodes;

namespace MIBO.ConversationService.Chat;

public sealed class TextComposer
{
    public string ComposeBudgetAndProducts(JsonNode? budget, JsonNode? summary, JsonNode? products)
    {
        var sb = new StringBuilder();

        var available = budget?["availableAmount"]?.GetValue<decimal?>() ?? 0m;
        sb.AppendLine($"Ai disponibil aproximativ **{available:0.##}** în cont.");

        var income = summary?["incomeTotal"]?.GetValue<decimal?>() ?? 0m;
        var expense = summary?["expenseTotal"]?.GetValue<decimal?>() ?? 0m;
        sb.AppendLine($"În ultimele 30 zile: venituri **{income:0.##}**, cheltuieli **{expense:0.##}**.");

        var items = products?["items"] as JsonArray;
        if (items is null || items.Count == 0)
        {
            sb.AppendLine("Nu am găsit produse în bugetul tău în acest moment.");
            return sb.ToString();
        }

        sb.AppendLine("Produse pe care ți le permiți (primele rezultate):");
        foreach (var p in items.Take(5))
        {
            var name = p?["name"]?.ToString() ?? "Produs";
            var price = p?["price"]?.GetValue<decimal?>() ?? 0m;
            sb.AppendLine($"- {name} — {price:0.##}");
        }

        return sb.ToString();
    }
}

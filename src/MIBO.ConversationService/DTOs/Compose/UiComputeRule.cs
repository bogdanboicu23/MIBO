namespace MIBO.ConversationService.DTOs.Compose;

public sealed class UiComputeRule
{
    // ex: "affordableProducts"
    public string TargetDataKey { get; set; } = default!;

    // ex: "shop.searchExternalProducts"
    public string SourceToolRef { get; set; } = default!;

    // ex: "finance.getBudgetSnapshot"
    public string BudgetToolRef { get; set; } = default!;

    // json paths
    public string SourceArrayPath { get; set; } = "$.products";     // dummyjson returns "products"
    public string PricePath { get; set; } = "$.price";              // per item
    public string BudgetValuePath { get; set; } = "$.availableToSpend";

    public int MaxItems { get; set; } = 12;
}
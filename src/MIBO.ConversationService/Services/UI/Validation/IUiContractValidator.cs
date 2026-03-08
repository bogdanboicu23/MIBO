namespace MIBO.ConversationService.Services.UI.Validation;

public sealed record UiValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

public interface IUiContractValidator
{
    UiValidationResult Validate(object uiV1);
}

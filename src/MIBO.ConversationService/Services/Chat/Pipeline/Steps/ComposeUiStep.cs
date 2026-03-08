using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Composer.Ui;
using MIBO.ConversationService.Services.UI.Validation;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class ComposeUiStep : IChatPipelineStep
{
    private readonly IUiComposer _uiComposer;
    private readonly IUiContractValidator _validator;
    private readonly ChatOrchestratorOptions _opt;
    private readonly ILogger<ComposeUiStep> _logger;

    public ComposeUiStep(
        IUiComposer uiComposer,
        IUiContractValidator validator,
        IOptions<ChatOrchestratorOptions> opt,
        ILogger<ComposeUiStep> logger
    )
    {
        _uiComposer = uiComposer;
        _validator = validator;
        _opt = opt.Value;
        _logger = logger;
    }

    public string Name => "compose_ui";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.SkipPlanning)
        {
            context.UiV1 = null;
            return;
        }

        if (context.Plan.UiIntent is null)
        {
            context.UiV1 = null;
            return;
        }

        var composedUi = await _uiComposer.ComposeUiV1Async(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Plan.UiIntent,
            context.ToolResults,
            ct
        );

        var validation = _validator.Validate(composedUi);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors.FirstOrDefault() ?? "ui_contract_invalid";
            context.Warnings.Add(firstError);
            context.UiV1 = null;

            _logger.LogWarning(
                "Invalid UI generated; strictValidation={Strict}, errors={Errors}, correlationId={CorrelationId}",
                _opt.StrictUiValidation,
                string.Join(" | ", validation.Errors),
                context.CorrelationId
            );

            if (_opt.StrictUiValidation) return;
        }
        else
        {
            foreach (var warning in validation.Warnings)
                context.Warnings.Add(warning);
        }

        context.UiV1 = composedUi;
    }
}

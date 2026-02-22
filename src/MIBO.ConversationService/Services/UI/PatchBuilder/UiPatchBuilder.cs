namespace MIBO.ConversationService.Services.UI.PatchBuilder;

public static class UiPatchBuilder
{
    public static object Replace(string path, object? value)
        => new { op = "replace", path, value };

    public static object Patch(params object[] ops)
        => new { schema = "ui.patch.v1", ops };
}
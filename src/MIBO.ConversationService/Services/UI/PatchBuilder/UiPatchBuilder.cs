namespace MIBO.ConversationService.Services.UI.PatchBuilder;

public static class UiPatchBuilder
{
    public static object Set(string path, object? value)
        => new { op = "set", path, value };

    public static object Replace(string path, object? value)
        => new { op = "replace", path, value };

    public static object Merge(string path, object? value)
        => new { op = "merge", path, value };

    public static object Remove(string path)
        => new { op = "remove", path };

    public static object Patch(string? uiInstanceId = null, params object[] ops)
        => new
        {
            schema = "ui.patch.v1",
            uiInstanceId,
            appliedAtUtc = DateTime.UtcNow,
            ops
        };
}

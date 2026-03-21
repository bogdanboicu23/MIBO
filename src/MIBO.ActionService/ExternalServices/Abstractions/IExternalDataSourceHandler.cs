using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.Abstractions;

public interface IExternalDataSourceHandler
{
    bool CanHandle(string handler);

    Task<object> QueryAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken);

    DataFieldHints GetFieldHints(string handler);
}

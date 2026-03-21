using System.Globalization;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.Pomodoro;

public sealed class PomodoroActionHandler : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "pomodoro.config.get",
    };

    public bool CanHandle(string handler)
    {
        return !string.IsNullOrWhiteSpace(handler) && SupportedHandlers.Contains(handler);
    }

    public Task<object> QueryAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        return handler switch
        {
            "pomodoro.config.get" => Task.FromResult(GetConfig(args)),
            _ => throw new InvalidOperationException($"Unsupported Pomodoro handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "pomodoro.config.get" => new DataFieldHints
            {
                EntityType = "pomodoro_config",
                LabelField = "phase",
                ValueField = "workMinutes",
                TitleField = "phase",
            },
            _ => new DataFieldHints(),
        };
    }

    private static object GetConfig(IReadOnlyDictionary<string, object?> args)
    {
        var workMinutes = GetInt(args, "workMinutes", 25);
        var shortBreakMinutes = GetInt(args, "shortBreakMinutes", 5);
        var longBreakMinutes = GetInt(args, "longBreakMinutes", 15);
        var sessionsBeforeLongBreak = GetInt(args, "sessionsBeforeLongBreak", 4);

        var totalCycleMinutes =
            (workMinutes + shortBreakMinutes) * (sessionsBeforeLongBreak - 1)
            + workMinutes
            + longBreakMinutes;

        return new Dictionary<string, object?>
        {
            ["workMinutes"] = workMinutes,
            ["shortBreakMinutes"] = shortBreakMinutes,
            ["longBreakMinutes"] = longBreakMinutes,
            ["sessionsBeforeLongBreak"] = sessionsBeforeLongBreak,
            ["totalCycleMinutes"] = totalCycleMinutes,
        };
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> args, string key, int defaultValue)
    {
        if (args.TryGetValue(key, out var value) && value is not null)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (int.TryParse(text, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }
}

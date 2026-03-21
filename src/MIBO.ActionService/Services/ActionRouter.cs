using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MIBO.ActionService.ExternalServices.Abstractions;

namespace MIBO.ActionService.Services;

public interface IActionRouter
{
    Task<QueryResponse> QueryAsync(
        DataSourceDefinition dataSource,
        IReadOnlyDictionary<string, object?>? args,
        CancellationToken cancellationToken);

    Task<ActionExecutionResponse> ExecuteAsync(
        ActionDefinition action,
        DataSourceDefinition? dataSource,
        IReadOnlyDictionary<string, DataSourceDefinition>? dataSources,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken);
}

public sealed class ActionRouter(IEnumerable<IExternalDataSourceHandler> externalDataSourceHandlers) : IActionRouter
{
    private readonly IReadOnlyList<IExternalDataSourceHandler> registeredExternalHandlers = externalDataSourceHandlers.ToList();

    public async Task<QueryResponse> QueryAsync(
        DataSourceDefinition dataSource,
        IReadOnlyDictionary<string, object?>? args,
        CancellationToken cancellationToken)
    {
        var mergedArgs = MergeArguments(dataSource.DefaultArgs, args);
        var resolvedHandler = string.IsNullOrWhiteSpace(dataSource.Handler) ? dataSource.Id : dataSource.Handler;
        var rawData = await DispatchAsync(
            resolvedHandler,
            mergedArgs,
            cancellationToken);
        var data = ApplyTransforms(rawData, dataSource.Transforms);
        var inferredHints = MergeManualFieldHints(
            InferFieldHints(resolvedHandler, mergedArgs, data),
            dataSource.FieldHints);

        return new QueryResponse
        {
            DataSourceId = dataSource.Id,
            Handler = resolvedHandler,
            Data = data,
            FieldHints = MergeTransformFieldHints(inferredHints, dataSource.Transforms),
            FetchedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        };
    }

    public async Task<ActionExecutionResponse> ExecuteAsync(
        ActionDefinition action,
        DataSourceDefinition? dataSource,
        IReadOnlyDictionary<string, DataSourceDefinition>? dataSources,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        var normalizedPayload = NormalizeDictionary(payload);
        var targetDataSource = ResolveTargetDataSource(action, dataSource, dataSources);

        QueryResponse? primaryResult = null;
        if (targetDataSource is not null)
        {
            var effectiveSource = targetDataSource with
            {
                Handler = string.IsNullOrWhiteSpace(action.Handler) ? targetDataSource.Handler : action.Handler,
                DefaultArgs = MergeArguments(targetDataSource.DefaultArgs, action.DefaultArgs),
            };

            primaryResult = await QueryAsync(
                effectiveSource,
                MergeArguments(action.DefaultArgs, normalizedPayload),
                cancellationToken);
        }

        var refreshes = new List<QueryResponse>();
        foreach (var refreshId in action.RefreshDataSourceIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var refreshSource = ResolveTargetDataSource(
                action with { DataSourceId = refreshId, Handler = string.Empty },
                null,
                dataSources);

            if (refreshSource is null)
            {
                continue;
            }

            refreshes.Add(await QueryAsync(refreshSource, refreshSource.DefaultArgs, cancellationToken));
        }

        return new ActionExecutionResponse
        {
            ActionId = action.Id,
            ActionType = string.IsNullOrWhiteSpace(action.ActionType) ? "ui.action.execute" : action.ActionType,
            DataSourceId = primaryResult?.DataSourceId ?? targetDataSource?.Id,
            Data = primaryResult?.Data,
            FieldHints = primaryResult?.FieldHints,
            FetchedAtUtc = primaryResult?.FetchedAtUtc,
            Refreshes = refreshes,
        };
    }

    private async Task<object> DispatchAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var externalHandler = ResolveExternalDataSourceHandler(handler);
        if (externalHandler is not null)
        {
            return await externalHandler.QueryAsync(handler, args, cancellationToken);
        }

        return handler switch
        {
            "finance.user.summary" => GetFinanceSummary(args),
            _ => throw new InvalidOperationException($"Unsupported action/data handler '{handler}'."),
        };
    }

    private IExternalDataSourceHandler? ResolveExternalDataSourceHandler(string handler)
    {
        return registeredExternalHandlers.FirstOrDefault(candidate => candidate.CanHandle(handler));
    }

    private static object GetFinanceSummary(IReadOnlyDictionary<string, object?> args)
    {
        var userId = Math.Max(1, GetInt(args, 1, "userId", "id"));
        var balance = (decimal)((userId * 137) % 10000 + 1000);
        var income = (decimal)(3000 + (userId * 53) % 2000);
        var expenses = income * 0.6m;
        var savings = balance + (income - expenses) * 3;

        var chart = new[]
        {
            new { label = "Income", value = Math.Round(income, 2) },
            new { label = "Expenses", value = Math.Round(expenses, 2) },
            new { label = "Savings", value = Math.Round(savings, 2) },
        };

        return new
        {
            userId,
            balance = Math.Round(balance, 2),
            income = Math.Round(income, 2),
            expenses = Math.Round(expenses, 2),
            savings = Math.Round(savings, 2),
            items = new[]
            {
                new { label = "Balance", value = Math.Round(balance, 2), color = "#3b82f6" },
                new { label = "Income", value = Math.Round(income, 2), color = "#10b981" },
                new { label = "Expenses", value = Math.Round(expenses, 2), color = "#f43f5e" },
                new { label = "Savings", value = Math.Round(savings, 2), color = "#6366f1" },
            },
            data = chart,
            chart,
        };
    }

    private static object ApplyTransforms(object data, IReadOnlyList<DataTransformDefinition> transforms)
    {
        if (transforms.Count == 0)
        {
            return data;
        }

        JsonNode? root;
        try
        {
            root = JsonSerializer.SerializeToNode(data);
        }
        catch
        {
            return data;
        }

        if (root is null)
        {
            return data;
        }

        foreach (var transform in transforms.Where(transform => !string.IsNullOrWhiteSpace(transform.Type)))
        {
            ApplyTransform(root, transform);
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(root.ToJsonString());
        }
        catch
        {
            return data;
        }
    }

    private static void ApplyTransform(JsonNode root, DataTransformDefinition transform)
    {
        switch (transform.Type.Trim().ToLowerInvariant())
        {
            case "project_rows":
                ApplyProjectRowsTransform(root, transform);
                break;
        }
    }

    private static void ApplyProjectRowsTransform(JsonNode root, DataTransformDefinition transform)
    {
        if (transform.Fields.Count == 0)
        {
            return;
        }

        var inputNode = ResolveNode(root, transform.InputPath);
        var sourceRows = ResolveArrayNode(inputNode);
        if (sourceRows is null)
        {
            return;
        }

        var projectedRows = new JsonArray();
        foreach (var entry in sourceRows)
        {
            if (entry is not JsonObject rowObject)
            {
                continue;
            }

            var projected = new JsonObject();
            foreach (var field in transform.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key))
                {
                    continue;
                }

                projected[field.Key] = JsonValueFromObject(ResolveTransformFieldValue(field, rowObject, root));
            }

            projectedRows.Add(projected);
        }

        SetNode(root, string.IsNullOrWhiteSpace(transform.OutputPath) ? "rows" : transform.OutputPath!, projectedRows);
    }

    private static object? ResolveTransformFieldValue(
        DataTransformFieldDefinition field,
        JsonObject row,
        JsonNode root)
    {
        if (field.Expression.HasValue && field.Expression.Value.ValueKind != JsonValueKind.Undefined)
        {
            return EvaluateExpression(field.Expression.Value, row, root);
        }

        if (field.Value.HasValue && field.Value.Value.ValueKind != JsonValueKind.Undefined)
        {
            return ConvertJsonElementToObject(field.Value.Value);
        }

        if (!string.IsNullOrWhiteSpace(field.SourceField))
        {
            return ResolveNodeValue(row, field.SourceField)
                ?? ResolveNodeValue(root, field.SourceField);
        }

        return ResolveNodeValue(row, field.Key);
    }

    private static object? EvaluateExpression(JsonElement expression, JsonObject row, JsonNode root)
    {
        if (expression.ValueKind != JsonValueKind.Object)
        {
            return ConvertJsonElementToObject(expression);
        }

        if (expression.TryGetProperty("field", out var fieldElement) && fieldElement.ValueKind == JsonValueKind.String)
        {
            return ResolveNodeValue(row, fieldElement.GetString());
        }

        if (expression.TryGetProperty("path", out var pathElement) && pathElement.ValueKind == JsonValueKind.String)
        {
            return ResolveNodeValue(root, pathElement.GetString());
        }

        if (expression.TryGetProperty("const", out var constElement))
        {
            return ConvertJsonElementToObject(constElement);
        }

        if (!expression.TryGetProperty("op", out var opElement) || opElement.ValueKind != JsonValueKind.String)
        {
            return ConvertJsonElementToObject(expression);
        }

        var op = opElement.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
        var args = ResolveExpressionArguments(expression, row, root);

        return op switch
        {
            "add" => ApplyNumericAggregation(args, 0m, (left, right) => left + right),
            "subtract" => ApplySubtraction(args),
            "multiply" => ApplyNumericAggregation(args, 1m, (left, right) => left * right),
            "divide" => ApplyDivision(args),
            "round" => ApplyRound(args),
            "floor" => ApplyFloor(args),
            "ceil" => ApplyCeil(args),
            "abs" => ApplyAbs(args),
            "max" => ApplyExtrema(args, pickMax: true),
            "min" => ApplyExtrema(args, pickMax: false),
            "concat" => string.Concat(args.Where(value => value is not null).Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))),
            "coalesce" => args.FirstOrDefault(value => value is not null && !string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture))),
            _ => ConvertJsonElementToObject(expression),
        };
    }

    private static List<object?> ResolveExpressionArguments(JsonElement expression, JsonObject row, JsonNode root)
    {
        if (expression.TryGetProperty("args", out var argsElement) && argsElement.ValueKind == JsonValueKind.Array)
        {
            return argsElement.EnumerateArray().Select(arg => EvaluateExpression(arg, row, root)).ToList();
        }

        var args = new List<object?>();
        if (expression.TryGetProperty("left", out var left))
        {
            args.Add(EvaluateExpression(left, row, root));
        }

        if (expression.TryGetProperty("right", out var right))
        {
            args.Add(EvaluateExpression(right, row, root));
        }

        return args;
    }

    private static object? ApplyNumericAggregation(
        IReadOnlyList<object?> args,
        decimal seed,
        Func<decimal, decimal, decimal> operation)
    {
        if (args.Count == 0)
        {
            return null;
        }

        var result = seed;
        var hasAny = false;
        foreach (var arg in args)
        {
            if (!TryToDecimal(arg, out var numeric))
            {
                continue;
            }

            result = hasAny || seed != 0m ? operation(result, numeric) : numeric;
            hasAny = true;
        }

        return hasAny ? result : null;
    }

    private static object? ApplySubtraction(IReadOnlyList<object?> args)
    {
        if (args.Count == 0 || !TryToDecimal(args[0], out var current))
        {
            return null;
        }

        foreach (var arg in args.Skip(1))
        {
            if (TryToDecimal(arg, out var numeric))
            {
                current -= numeric;
            }
        }

        return current;
    }

    private static object? ApplyDivision(IReadOnlyList<object?> args)
    {
        if (args.Count == 0 || !TryToDecimal(args[0], out var current))
        {
            return null;
        }

        foreach (var arg in args.Skip(1))
        {
            if (!TryToDecimal(arg, out var numeric) || numeric == 0m)
            {
                return null;
            }

            current /= numeric;
        }

        return current;
    }

    private static object? ApplyRound(IReadOnlyList<object?> args)
    {
        if (args.Count == 0 || !TryToDecimal(args[0], out var numeric))
        {
            return null;
        }

        var digits = args.Count > 1 && TryToDecimal(args[1], out var rawDigits)
            ? (int)rawDigits
            : 0;

        return Math.Round(numeric, digits);
    }

    private static object? ApplyFloor(IReadOnlyList<object?> args)
    {
        return args.Count > 0 && TryToDecimal(args[0], out var numeric)
            ? Math.Floor(numeric)
            : null;
    }

    private static object? ApplyCeil(IReadOnlyList<object?> args)
    {
        return args.Count > 0 && TryToDecimal(args[0], out var numeric)
            ? Math.Ceiling(numeric)
            : null;
    }

    private static object? ApplyAbs(IReadOnlyList<object?> args)
    {
        return args.Count > 0 && TryToDecimal(args[0], out var numeric)
            ? Math.Abs(numeric)
            : null;
    }

    private static object? ApplyExtrema(IReadOnlyList<object?> args, bool pickMax)
    {
        var values = args
            .Select(arg => TryToDecimal(arg, out var numeric) ? numeric : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        if (values.Count == 0)
        {
            return null;
        }

        return pickMax ? values.Max() : values.Min();
    }

    private static bool TryToDecimal(object? value, out decimal numeric)
    {
        switch (value)
        {
            case null:
                break;
            case decimal decimalValue:
                numeric = decimalValue;
                return true;
            case int intValue:
                numeric = intValue;
                return true;
            case long longValue:
                numeric = longValue;
                return true;
            case float floatValue:
                numeric = (decimal)floatValue;
                return true;
            case double doubleValue:
                numeric = (decimal)doubleValue;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var parsedDecimal):
                numeric = parsedDecimal;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.String:
                if (decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedStringDecimal))
                {
                    numeric = parsedStringDecimal;
                    return true;
                }
                break;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            numeric = parsed;
            return true;
        }

        numeric = 0m;
        return false;
    }

    private static JsonArray? ResolveArrayNode(JsonNode? node)
    {
        if (node is JsonArray array)
        {
            return array;
        }

        if (node is not JsonObject obj)
        {
            return null;
        }

        var preferredKeys = new[] { "rows", "items", "products", "data", "results", "series" };
        foreach (var key in preferredKeys)
        {
            if (obj[key] is JsonArray candidate)
            {
                return candidate;
            }
        }

        return obj.Select(entry => entry.Value).OfType<JsonArray>().FirstOrDefault();
    }

    private static JsonNode? ResolveNode(JsonNode? node, string? path)
    {
        if (node is null || string.IsNullOrWhiteSpace(path))
        {
            return node;
        }

        var current = node;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is not JsonObject currentObject)
            {
                return null;
            }

            current = currentObject[segment];
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private static object? ResolveNodeValue(JsonNode? node, string? path)
    {
        var resolved = ResolveNode(node, path);
        return resolved is null ? null : ConvertJsonNodeToObject(resolved);
    }

    private static void SetNode(JsonNode root, string path, JsonNode? value)
    {
        if (root is not JsonObject rootObject || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return;
        }

        JsonObject current = rootObject;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            var segment = segments[index];
            if (current[segment] is not JsonObject next)
            {
                next = new JsonObject();
                current[segment] = next;
            }

            current = next;
        }

        current[segments[^1]] = value?.DeepClone();
    }

    private static JsonNode? JsonValueFromObject(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToNode(value);
    }

    private static object? ConvertJsonNodeToObject(JsonNode node)
    {
        return JsonSerializer.Deserialize<object>(node.ToJsonString());
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return JsonSerializer.Deserialize<object>(element.GetRawText());
    }

    private DataFieldHints InferFieldHints(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        object data)
    {
        _ = args;

        var baseline = ResolveExternalDataSourceHandler(handler)?.GetFieldHints(handler) ?? handler switch
        {
            "finance.user.summary" => new DataFieldHints
            {
                EntityType = "finance_summary",
                CollectionPath = "items",
                LabelField = "label",
                ValueField = "value",
                TitleField = "label",
                SearchField = "label",
            },
            _ => new DataFieldHints(),
        };

        return EnrichFieldHintsFromData(baseline, data);
    }

    private static DataFieldHints MergeTransformFieldHints(
        DataFieldHints inferred,
        IReadOnlyList<DataTransformDefinition> transforms)
    {
        if (transforms.Count == 0)
        {
            return inferred;
        }

        var projectedTransform = transforms.LastOrDefault(transform =>
            string.Equals(transform.Type, "project_rows", StringComparison.OrdinalIgnoreCase));
        if (projectedTransform is null)
        {
            return inferred;
        }

        var hintedFields = projectedTransform.FieldHints?.Fields?.Count > 0
            ? projectedTransform.FieldHints.Fields
            : projectedTransform.Fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Key))
                .Select(field => new DataFieldDescriptor
                {
                    Name = field.Key,
                    Kind = InferTransformFieldKind(field, inferred),
                })
                .ToList();

        var defaultTableFields = projectedTransform.FieldHints?.DefaultTableFields?.Count > 0
            ? projectedTransform.FieldHints.DefaultTableFields
            : projectedTransform.Fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Key))
                .Select(field => field.Key)
                .ToList();

        var labelField = projectedTransform.FieldHints?.LabelField
            ?? inferred.LabelField
            ?? hintedFields.FirstOrDefault(field => field.Kind == "text")?.Name;
        var valueField = projectedTransform.FieldHints?.ValueField
            ?? inferred.ValueField
            ?? hintedFields.FirstOrDefault(field => field.Kind is "money" or "number" or "integer")?.Name;

        return new DataFieldHints
        {
            EntityType = projectedTransform.FieldHints?.EntityType ?? inferred.EntityType,
            CollectionPath = projectedTransform.FieldHints?.CollectionPath
                ?? projectedTransform.OutputPath
                ?? inferred.CollectionPath,
            LabelField = labelField,
            ValueField = valueField,
            TitleField = projectedTransform.FieldHints?.TitleField ?? inferred.TitleField ?? labelField,
            ImageField = projectedTransform.FieldHints?.ImageField ?? inferred.ImageField,
            CategoryField = projectedTransform.FieldHints?.CategoryField ?? inferred.CategoryField,
            SearchField = projectedTransform.FieldHints?.SearchField ?? inferred.SearchField ?? labelField,
            DefaultTableFields = defaultTableFields,
            TextFields = MergeDistinct(
                inferred.TextFields,
                projectedTransform.FieldHints?.TextFields ?? [],
                hintedFields.Where(field => field.Kind == "text").Select(field => field.Name).ToList()),
            NumericFields = MergeDistinct(
                inferred.NumericFields,
                projectedTransform.FieldHints?.NumericFields ?? [],
                hintedFields.Where(field => field.Kind is "money" or "number" or "integer").Select(field => field.Name).ToList()),
            Fields = hintedFields.Count > 0 ? hintedFields : inferred.Fields,
        };
    }

    private static DataFieldHints MergeManualFieldHints(DataFieldHints inferred, DataFieldHints? explicitHints)
    {
        if (explicitHints is null)
        {
            return inferred;
        }

        return new DataFieldHints
        {
            EntityType = explicitHints.EntityType ?? inferred.EntityType,
            CollectionPath = explicitHints.CollectionPath ?? inferred.CollectionPath,
            LabelField = explicitHints.LabelField ?? inferred.LabelField,
            ValueField = explicitHints.ValueField ?? inferred.ValueField,
            TitleField = explicitHints.TitleField ?? inferred.TitleField,
            ImageField = explicitHints.ImageField ?? inferred.ImageField,
            CategoryField = explicitHints.CategoryField ?? inferred.CategoryField,
            SearchField = explicitHints.SearchField ?? inferred.SearchField,
            DefaultTableFields = explicitHints.DefaultTableFields.Count > 0
                ? explicitHints.DefaultTableFields
                : inferred.DefaultTableFields,
            TextFields = explicitHints.TextFields.Count > 0
                ? explicitHints.TextFields
                : inferred.TextFields,
            NumericFields = explicitHints.NumericFields.Count > 0
                ? explicitHints.NumericFields
                : inferred.NumericFields,
            Fields = explicitHints.Fields.Count > 0
                ? explicitHints.Fields
                : inferred.Fields,
        };
    }

    private static DataFieldHints EnrichFieldHintsFromData(DataFieldHints baseline, object data)
    {
        JsonElement element;
        try
        {
            element = JsonSerializer.SerializeToElement(data);
        }
        catch
        {
            return baseline;
        }

        var profiledFields = BuildFieldProfiles(element, baseline.CollectionPath);
        if (profiledFields.Count == 0)
        {
            return baseline;
        }

        var descriptors = profiledFields
            .Select(field => new DataFieldDescriptor
            {
                Name = field.Name,
                Kind = field.Kind,
            })
            .ToList();

        var labelField = ResolvePreferredField(baseline.LabelField, profiledFields)
            ?? profiledFields.FirstOrDefault(field => field.Kind == "text")?.Name;
        var valueField = ResolvePreferredField(baseline.ValueField, profiledFields)
            ?? profiledFields.FirstOrDefault(field => field.Kind == "number")?.Name
            ?? profiledFields.FirstOrDefault(field => field.Kind == "integer")?.Name;
        var titleField = ResolvePreferredField(baseline.TitleField, profiledFields) ?? labelField;
        var imageField = ResolvePreferredField(baseline.ImageField, profiledFields);
        var categoryField = ResolvePreferredField(baseline.CategoryField, profiledFields);
        var searchField = ResolvePreferredField(baseline.SearchField, profiledFields) ?? labelField;
        var defaultTableFields = baseline.DefaultTableFields.Count > 0
            ? baseline.DefaultTableFields.Where(field => profiledFields.Any(candidate => candidate.Name == field)).ToList()
            : BuildDefaultTableFields(profiledFields, labelField, categoryField, valueField);

        return new DataFieldHints
        {
            EntityType = baseline.EntityType,
            CollectionPath = baseline.CollectionPath,
            LabelField = labelField,
            ValueField = valueField,
            TitleField = titleField,
            ImageField = imageField,
            CategoryField = categoryField,
            SearchField = searchField,
            DefaultTableFields = defaultTableFields,
            TextFields = profiledFields.Where(field => field.Kind == "text").Select(field => field.Name).ToList(),
            NumericFields = profiledFields.Where(field => field.Kind is "number" or "integer").Select(field => field.Name).ToList(),
            Fields = descriptors,
        };
    }

    private static List<string> MergeDistinct(params IEnumerable<string>[] segments)
    {
        var values = new List<string>();
        foreach (var segment in segments)
        {
            foreach (var value in segment.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!values.Contains(value, StringComparer.Ordinal))
                {
                    values.Add(value);
                }
            }
        }

        return values;
    }

    private static string InferTransformFieldKind(DataTransformFieldDefinition field, DataFieldHints inferred)
    {
        if (!string.IsNullOrWhiteSpace(field.Kind))
        {
            return field.Kind!;
        }

        if (!string.IsNullOrWhiteSpace(field.SourceField))
        {
            var matchingField = inferred.Fields.FirstOrDefault(candidate => candidate.Name == field.SourceField);
            if (matchingField is not null && !string.IsNullOrWhiteSpace(matchingField.Kind))
            {
                return matchingField.Kind;
            }
        }

        if (field.Expression.HasValue
            && field.Expression.Value.ValueKind == JsonValueKind.Object
            && field.Expression.Value.TryGetProperty("op", out var opElement)
            && opElement.ValueKind == JsonValueKind.String)
        {
            var op = opElement.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
            if (op is "add" or "subtract" or "multiply" or "divide" or "round" or "floor" or "ceil" or "abs" or "max" or "min")
            {
                return "number";
            }
        }

        return "text";
    }

    private static List<string> BuildDefaultTableFields(
        IReadOnlyList<ProfiledField> fields,
        string? labelField,
        string? categoryField,
        string? valueField)
    {
        var ordered = new List<string>();

        void AddField(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName) || ordered.Contains(fieldName, StringComparer.Ordinal))
            {
                return;
            }

            if (fields.Any(field => field.Name == fieldName))
            {
                ordered.Add(fieldName);
            }
        }

        AddField(labelField);
        AddField(categoryField);
        AddField(valueField);

        foreach (var candidate in fields)
        {
            if (candidate.Kind is "json" or "array" or "image")
            {
                continue;
            }

            AddField(candidate.Name);
            if (ordered.Count >= 5)
            {
                break;
            }
        }

        return ordered;
    }

    private static string? ResolvePreferredField(string? preferredField, IReadOnlyList<ProfiledField> fields)
    {
        if (string.IsNullOrWhiteSpace(preferredField))
        {
            return null;
        }

        return fields.Any(field => field.Name == preferredField) ? preferredField : null;
    }

    private static List<ProfiledField> BuildFieldProfiles(JsonElement element, string? collectionPath)
    {
        var target = ResolveFieldProfileTarget(element, collectionPath);
        if (target.ValueKind == JsonValueKind.Array)
        {
            return BuildArrayFieldProfiles(target);
        }

        if (target.ValueKind == JsonValueKind.Object)
        {
            return BuildObjectFieldProfiles(target);
        }

        return new List<ProfiledField>();
    }

    private static JsonElement ResolveFieldProfileTarget(JsonElement root, string? collectionPath)
    {
        if (!string.IsNullOrWhiteSpace(collectionPath) && TryResolveJsonPath(root, collectionPath, out var direct))
        {
            return direct;
        }

        if (root.ValueKind == JsonValueKind.Array || root.ValueKind == JsonValueKind.Object)
        {
            return root;
        }

        return default;
    }

    private static bool TryResolveJsonPath(JsonElement element, string path, out JsonElement resolved)
    {
        resolved = element;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (resolved.ValueKind != JsonValueKind.Object || !resolved.TryGetProperty(segment, out resolved))
            {
                resolved = default;
                return false;
            }
        }

        return true;
    }

    private static List<ProfiledField> BuildArrayFieldProfiles(JsonElement arrayElement)
    {
        var objects = arrayElement.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Take(12)
            .ToList();

        if (objects.Count == 0)
        {
            return new List<ProfiledField>();
        }

        var propertyOrder = new List<string>();
        foreach (var item in objects)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (!propertyOrder.Contains(property.Name, StringComparer.Ordinal))
                {
                    propertyOrder.Add(property.Name);
                }
            }
        }

        var fields = new List<ProfiledField>();
        foreach (var propertyName in propertyOrder)
        {
            var samples = objects
                .Where(item => item.TryGetProperty(propertyName, out _))
                .Select(item => item.GetProperty(propertyName))
                .ToList();
            if (samples.Count == 0)
            {
                continue;
            }

            var kind = InferFieldKind(samples);
            if (kind is "json" or "array")
            {
                continue;
            }

            fields.Add(new ProfiledField(propertyName, kind));
        }

        return fields;
    }

    private static List<ProfiledField> BuildObjectFieldProfiles(JsonElement objectElement)
    {
        var fields = new List<ProfiledField>();
        foreach (var property in objectElement.EnumerateObject())
        {
            var kind = InferFieldKind([property.Value]);
            if (kind is "json" or "array")
            {
                continue;
            }

            fields.Add(new ProfiledField(property.Name, kind));
        }

        return fields;
    }

    private static string InferFieldKind(IReadOnlyList<JsonElement> samples)
    {
        if (samples.Count == 0)
        {
            return "text";
        }

        if (samples.Any(sample => sample.ValueKind == JsonValueKind.Number))
        {
            return samples
                .Where(sample => sample.ValueKind == JsonValueKind.Number)
                .Any(sample => sample.TryGetDouble(out var number) && Math.Abs(number % 1) > double.Epsilon)
                ? "number"
                : "integer";
        }

        if (samples.Any(sample => sample.ValueKind is JsonValueKind.True or JsonValueKind.False))
        {
            return "boolean";
        }

        if (samples.Any(sample => sample.ValueKind == JsonValueKind.String))
        {
            var values = samples
                .Where(sample => sample.ValueKind == JsonValueKind.String)
                .Select(sample => sample.GetString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            if (values.Count > 0 && values.All(IsImageLikeValue))
            {
                return "image";
            }

            if (values.Count > 0 && values.All(value => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _)))
            {
                return "date";
            }

            return "text";
        }

        if (samples.Any(sample => sample.ValueKind == JsonValueKind.Array))
        {
            return "array";
        }

        return "json";
    }

    private static bool IsImageLikeValue(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                   || uri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                   || uri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                   || uri.AbsolutePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                   || uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record ProfiledField(string Name, string Kind);

    private static DataSourceDefinition? ResolveTargetDataSource(
        ActionDefinition action,
        DataSourceDefinition? directDataSource,
        IReadOnlyDictionary<string, DataSourceDefinition>? dataSources)
    {
        if (!string.IsNullOrWhiteSpace(action.DataSourceId) &&
            dataSources is not null &&
            dataSources.TryGetValue(action.DataSourceId, out var byId))
        {
            return byId;
        }

        if (directDataSource is not null)
        {
            return directDataSource;
        }

        if (!string.IsNullOrWhiteSpace(action.Handler))
        {
            return new DataSourceDefinition
            {
                Id = action.DataSourceId ?? action.Id,
                Handler = action.Handler,
                DefaultArgs = NormalizeDictionary(action.DefaultArgs),
            };
        }

        return null;
    }

    private static Dictionary<string, object?> MergeArguments(
        IReadOnlyDictionary<string, object?>? left,
        IReadOnlyDictionary<string, object?>? right)
    {
        var merged = NormalizeDictionary(left);
        foreach (var (key, value) in NormalizeDictionary(right))
        {
            merged[key] = value;
        }

        return merged;
    }

    private static Dictionary<string, object?> NormalizeDictionary(IReadOnlyDictionary<string, object?>? source)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
        {
            return normalized;
        }

        foreach (var (key, value) in source)
        {
            normalized[key] = NormalizeValue(value);
        }

        return normalized;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement element => NormalizeJsonElement(element),
            Dictionary<string, object?> dictionary => NormalizeDictionary(dictionary),
            IReadOnlyDictionary<string, object?> dictionary => NormalizeDictionary(dictionary),
            IEnumerable<object?> items => items.Select(NormalizeValue).ToList(),
            _ => value,
        };
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => NormalizeJsonElement(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(NormalizeJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> source, int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.TryGetValue(key, out var value) && value is not null)
            {
                if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return fallback;
    }

}

public sealed record DataSourceDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("handler")]
    public string Handler { get; init; } = string.Empty;

    [JsonPropertyName("defaultArgs")]
    public Dictionary<string, object?> DefaultArgs { get; init; } = new();

    [JsonPropertyName("refreshOnLoad")]
    public bool RefreshOnLoad { get; init; }

    [JsonPropertyName("refreshOnConversationOpen")]
    public bool RefreshOnConversationOpen { get; init; }

    [JsonPropertyName("staleAfterMs")]
    public long? StaleAfterMs { get; init; }

    [JsonPropertyName("fieldHints")]
    public DataFieldHints? FieldHints { get; init; }

    [JsonPropertyName("transforms")]
    public List<DataTransformDefinition> Transforms { get; init; } = new();
}

public sealed record ActionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("actionType")]
    public string ActionType { get; init; } = "ui.action.execute";

    [JsonPropertyName("handler")]
    public string Handler { get; init; } = string.Empty;

    [JsonPropertyName("dataSourceId")]
    public string? DataSourceId { get; init; }

    [JsonPropertyName("defaultArgs")]
    public Dictionary<string, object?> DefaultArgs { get; init; } = new();

    [JsonPropertyName("refreshDataSourceIds")]
    public List<string> RefreshDataSourceIds { get; init; } = new();
}

public sealed class QueryResponse
{
    [JsonPropertyName("dataSourceId")]
    public string DataSourceId { get; init; } = string.Empty;

    [JsonPropertyName("handler")]
    public string Handler { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public object Data { get; init; } = new();

    [JsonPropertyName("fieldHints")]
    public DataFieldHints FieldHints { get; init; } = new();

    [JsonPropertyName("fetchedAtUtc")]
    public string FetchedAtUtc { get; init; } = string.Empty;
}

public sealed class ActionExecutionResponse
{
    [JsonPropertyName("actionId")]
    public string ActionId { get; init; } = string.Empty;

    [JsonPropertyName("actionType")]
    public string ActionType { get; init; } = string.Empty;

    [JsonPropertyName("dataSourceId")]
    public string? DataSourceId { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("fieldHints")]
    public DataFieldHints? FieldHints { get; init; }

    [JsonPropertyName("fetchedAtUtc")]
    public string? FetchedAtUtc { get; init; }

    [JsonPropertyName("refreshes")]
    public List<QueryResponse> Refreshes { get; init; } = new();
}

public sealed class DataFieldHints
{
    [JsonPropertyName("entityType")]
    public string? EntityType { get; init; }

    [JsonPropertyName("collectionPath")]
    public string? CollectionPath { get; init; }

    [JsonPropertyName("labelField")]
    public string? LabelField { get; init; }

    [JsonPropertyName("valueField")]
    public string? ValueField { get; init; }

    [JsonPropertyName("titleField")]
    public string? TitleField { get; init; }

    [JsonPropertyName("imageField")]
    public string? ImageField { get; init; }

    [JsonPropertyName("categoryField")]
    public string? CategoryField { get; init; }

    [JsonPropertyName("searchField")]
    public string? SearchField { get; init; }

    [JsonPropertyName("defaultTableFields")]
    public List<string> DefaultTableFields { get; init; } = new();

    [JsonPropertyName("textFields")]
    public List<string> TextFields { get; init; } = new();

    [JsonPropertyName("numericFields")]
    public List<string> NumericFields { get; init; } = new();

    [JsonPropertyName("fields")]
    public List<DataFieldDescriptor> Fields { get; init; } = new();
}

public sealed class DataTransformDefinition
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("inputPath")]
    public string? InputPath { get; init; }

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("fields")]
    public List<DataTransformFieldDefinition> Fields { get; init; } = new();

    [JsonPropertyName("fieldHints")]
    public DataFieldHints? FieldHints { get; init; }
}

public sealed class DataTransformFieldDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("sourceField")]
    public string? SourceField { get; init; }

    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }

    [JsonPropertyName("expression")]
    public JsonElement? Expression { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }
}

public sealed class DataFieldDescriptor
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "text";
}

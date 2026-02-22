using System.Text.Json;

namespace MIBO.ConversationService.Helper;

public static class JsonPathLite
{
    // suport minim: $.a.b[0].c
    public static bool TryGet(JsonElement root, string path, out JsonElement value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(path)) return false;

        var p = path.Trim();
        if (p.StartsWith("$."))
            p = p[2..];
        else if (p.StartsWith("$"))
            p = p[1..];

        var current = root;

        foreach (var token in Tokenize(p))
        {
            if (token.IsIndex)
            {
                if (current.ValueKind != JsonValueKind.Array) return false;
                if (token.Index < 0 || token.Index >= current.GetArrayLength()) return false;
                current = current.EnumerateArray().ElementAt(token.Index);
            }
            else
            {
                if (current.ValueKind != JsonValueKind.Object) return false;
                if (!current.TryGetProperty(token.Name!, out var next)) return false;
                current = next;
            }
        }

        value = current;
        return true;
    }

    private readonly record struct Tok(string? Name, bool IsIndex, int Index);

    private static IEnumerable<Tok> Tokenize(string p)
    {
        // split by '.' but keep [idx] tokens
        var parts = p.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var s = part;
            while (true)
            {
                var i = s.IndexOf('[');
                if (i < 0)
                {
                    yield return new Tok(s, false, -1);
                    break;
                }

                var name = s[..i];
                if (!string.IsNullOrEmpty(name))
                    yield return new Tok(name, false, -1);

                var j = s.IndexOf(']', i + 1);
                var idxStr = s[(i + 1)..j];
                yield return new Tok(null, true, int.Parse(idxStr));

                s = s[(j + 1)..];
                if (string.IsNullOrEmpty(s)) break;
            }
        }
    }
}

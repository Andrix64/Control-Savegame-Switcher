namespace Lss;

/// <summary>
/// Lettore minimo del formato testuale VDF di Steam ("chiave" "valore" e
/// "chiave" { ... }). Solo lettura, chiavi senza distinzione di maiuscole.
/// </summary>
internal static class Vdf
{
    /// <summary>
    /// Nodo VDF: ogni chiave ha un valore (string) o un blocco (Node).
    /// </summary>
    public sealed class Node : Dictionary<string, object>
    {
        public Node() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

    public static Node Parse(string text)
    {
        int position = 0;
        return ParseBlock(text, ref position, topLevel: true);
    }

    /// <summary>
    /// Cerca a qualsiasi profondità una sequenza di chiavi annidate
    /// (es. "apps", "870780", "cloudenabled") e restituisce il valore finale.
    /// </summary>
    public static string? Find(Node node, params string[] path)
    {
        if (TryPath(node, path, out string? value))
            return value;

        foreach (object child in node.Values)
        {
            if (child is Node block && Find(block, path) is string found)
                return found;
        }

        return null;
    }

    private static bool TryPath(Node node, string[] path, out string? value)
    {
        value = null;
        Node current = node;

        for (int i = 0; i < path.Length; i++)
        {
            if (!current.TryGetValue(path[i], out object? next))
                return false;

            if (i == path.Length - 1)
            {
                value = next as string;
                return value != null;
            }

            if (next is not Node block)
                return false;

            current = block;
        }

        return false;
    }

    private static Node ParseBlock(string text, ref int position, bool topLevel)
    {
        var node = new Node();

        while (true)
        {
            string? key = NextToken(text, ref position);

            if (key == null)
            {
                if (!topLevel)
                    throw new FormatException("VDF: blocco non chiuso.");

                return node;
            }

            if (key == "}")
            {
                if (topLevel)
                    throw new FormatException("VDF: '}' in più.");

                return node;
            }

            string? value = NextToken(text, ref position)
                            ?? throw new FormatException("VDF: valore mancante per " + key);

            // Una chiave ripetuta tiene l'ultimo valore, come fa Steam.
            node[key] = value == "{" ? ParseBlock(text, ref position, topLevel: false) : value;
        }
    }

    /// <summary>
    /// Prossimo token: una stringa tra virgolette (senza virgolette), "{" o "}".
    /// Salta spazi e commenti //. null alla fine del testo.
    /// </summary>
    private static string? NextToken(string text, ref int position)
    {
        while (position < text.Length)
        {
            char c = text[position];

            if (char.IsWhiteSpace(c))
            {
                position++;
            }
            else if (c == '/' && position + 1 < text.Length && text[position + 1] == '/')
            {
                while (position < text.Length && text[position] != '\n')
                    position++;
            }
            else if (c is '{' or '}')
            {
                position++;
                return c.ToString();
            }
            else if (c == '"')
            {
                return ReadQuoted(text, ref position);
            }
            else
            {
                throw new FormatException($"VDF: carattere inatteso '{c}'.");
            }
        }

        return null;
    }

    private static string ReadQuoted(string text, ref int position)
    {
        var value = new System.Text.StringBuilder();
        position++;

        while (position < text.Length)
        {
            char c = text[position++];

            if (c == '"')
                return value.ToString();

            if (c == '\\' && position < text.Length)
            {
                char escaped = text[position++];
                value.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    _ => escaped
                });
            }
            else
            {
                value.Append(c);
            }
        }

        throw new FormatException("VDF: stringa non chiusa.");
    }
}

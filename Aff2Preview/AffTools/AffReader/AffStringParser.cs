namespace AffTools.AffReader;

public class AffStringParser(string str)
{
    private int pos;
    private string str = str;

    public void Skip(int length)
    {
        pos += length;
    }

    public float ReadFloat(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        float value = float.Parse(str[pos..end]);
        pos += end - pos + 1;
        return value;
    }

    public int ReadInt(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        int value = int.Parse(str[pos..end]);
        pos += end - pos + 1;
        return value;
    }

    public bool ReadBool(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        bool value = bool.Parse(str.AsSpan(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public string ReadString(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        string value = str[pos..end];
        pos += end - pos + 1;
        return value;
    }

    public string ReadString(string[] optionalTerminator, out string realTerminator)
    {
        realTerminator = string.Empty;
        int end = -1;
        foreach (string terminator in optionalTerminator)
        {
            int terminatorPosition = str.IndexOf(terminator, pos);
            if (terminatorPosition > end && !str[pos..terminatorPosition].Contains('['))
            {
                end = terminatorPosition;
                realTerminator = terminator;
            }
        }
        if (end == -1)
        {
            end += str.Length;
        }
        string value = str[pos..end];
        pos += end - pos + 1;

        return value;
    }

    public string Current
    {
        get
        {
            return str[pos].ToString();
        }
    }

    public string Peek(int count)
    {
        return str.Substring(pos, count);
    }
}

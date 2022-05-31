namespace AffTools.AffReader;

public class AffStringParser
{
    private int pos;
    private string str;

    public AffStringParser(string str)
    {
        this.str = str;
    }

    public void Skip(int length)
    {
        pos += length;
    }

    public float ReadFloat(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        float value = float.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public int ReadInt(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        int value = int.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public bool ReadBool(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        bool value = bool.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public string ReadString(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        string value = str.Substring(pos, end - pos);
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

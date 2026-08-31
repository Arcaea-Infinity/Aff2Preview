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
        float value = float.Parse(
            str.Substring(pos, end - pos),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture);
        pos += end - pos + 1;
        return value;
    }

    public int ReadInt(string? terminator = null)
    {
        int end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        int value = int.Parse(
            str.Substring(pos, end - pos),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture);
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

    public string ReadString(IReadOnlyList<string> terminators, out string actualTerminator)
    {
        int end = str.Length;
        actualTerminator = "";

        foreach (string terminator in terminators)
        {
            int terminatorPosition = str.IndexOf(terminator, pos, StringComparison.Ordinal);
            if (terminatorPosition >= 0 && terminatorPosition < end)
            {
                end = terminatorPosition;
                actualTerminator = terminator;
            }
        }

        if (actualTerminator.Length == 0)
            throw new FormatException("找不到字段结束符");

        string value = str.Substring(pos, end - pos);
        pos = end + actualTerminator.Length;
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

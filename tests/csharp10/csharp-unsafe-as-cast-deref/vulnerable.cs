public sealed class UnsafeCast
{
    public string Describe(object value)
    {
        var text = value as string;
        return text.ToString();
    }
}

public sealed class CoalescedCast
{
    public string Describe(object value)
    {
        var text = value as string;
        text = text ?? "empty";
        return text.ToString();
    }
}

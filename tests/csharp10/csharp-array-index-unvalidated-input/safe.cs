using System;

public sealed class ValidatedSlot
{
    public char ReadSlot(string[] data)
    {
        var input = Console.ReadLine();
        var index = int.Parse(input);
        return data[index];
    }
}

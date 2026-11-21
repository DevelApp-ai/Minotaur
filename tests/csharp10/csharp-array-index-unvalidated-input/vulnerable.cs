using System;

public sealed class UnvalidatedSlot
{
    public char ReadSlot(string[] data)
    {
        var input = Console.ReadLine();
        return data[input];
    }
}

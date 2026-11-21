using System;
using System.Collections.Generic;
using System.Linq;

public sealed class UnguardedMatch
{
    public string Render(List<string> list)
    {
        var item = list.FirstOrDefault();
        return item.ToString();
    }
}

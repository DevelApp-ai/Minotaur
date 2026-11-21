using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GuardedMatch
{
    public string Render(List<string> list)
    {
        var item = list.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(item);
        return item.ToString();
    }
}

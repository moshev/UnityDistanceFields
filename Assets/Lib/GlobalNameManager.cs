using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal class GlobalNameManager
{
    private Dictionary<string, int> mangleMap = new Dictionary<string, int>();

    public string makeUnique(string name)
    {
        int suffix;
        mangleMap.TryGetValue(name, out suffix);
        suffix++;
        mangleMap[name] = suffix;
        return String.Format("{0:s}_{1:d}", name, suffix);
    }
}
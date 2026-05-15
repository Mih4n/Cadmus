using System;
using System.Linq;
using Silk.NET.Core.Native;

class Probe
{
    static void Main()
    {
        var ctors = typeof(VkHandle).GetConstructors();
        Console.WriteLine($"VkHandle ctors: {ctors.Length}");
        foreach (var c in ctors)
        {
            Console.WriteLine($"  {string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))}");
        }
        var fields = typeof(VkHandle).GetFields();
        foreach (var f in fields)
        {
            Console.WriteLine($"Field: {f.FieldType.Name} {f.Name}");
        }
    }
}

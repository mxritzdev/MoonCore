using MoonCore.Attributes;

namespace MoonCore.Test;

[RunAfter(typeof(Class1))]
public class Class2 : IStartupLayer
{
    public Task Run()
    {
        Console.WriteLine("2");
        return Task.CompletedTask;
    }
}
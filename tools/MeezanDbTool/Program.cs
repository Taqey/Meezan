using System;
using System.Threading.Tasks;

namespace MeezanDbTool;

public class Program
{
    public static async Task Main(string[] args)
    {
        await RecalcSectors.RunAsync();
    }
}

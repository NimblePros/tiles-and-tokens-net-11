using TilesAndTokens.Demo.Closed;
using TilesAndTokens.Demo.Legacy;
using TilesAndTokens.Demo.Unions;

namespace TilesAndTokens.Demo;

internal static class DemoRunner
{
    // One entry per milestone, in the order `all` runs them. Each milestone adds its demo here.
    private static readonly IReadOnlyList<IDemo> Demos =
    [
        new LegacyDemo(),
        new UnionsDemo(),
        new ClosedDemo(),
    ];

    public static int Run(string? command, bool noPause)
    {
        if (command == "all")
        {
            RunAll(noPause);
            return 0;
        }

        var demo = Demos.FirstOrDefault(d => d.Command == command);
        if (demo is null)
        {
            PrintUsage();
            return command is null ? 0 : 1;
        }

        Narrator.Header(demo.Title);
        demo.Run();
        return 0;
    }

    private static void RunAll(bool noPause)
    {
        for (var i = 0; i < Demos.Count; i++)
        {
            Narrator.Header($"{i + 1}/{Demos.Count}: {Demos[i].Title}");
            Demos[i].Run();

            if (i < Demos.Count - 1 && !noPause)
            {
                Narrator.Pause();
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run --project src/TilesAndTokens.Demo -- <command> [--no-pause]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        foreach (var demo in Demos)
        {
            Console.WriteLine($"  {demo.Command,-8} {demo.Title}");
        }

        Console.WriteLine($"  {"all",-8} Run every demo in order, pausing between sections");
    }
}

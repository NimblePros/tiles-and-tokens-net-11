namespace TilesAndTokens.Demo;

/// <summary>Console helpers for the narrated walkthroughs shown during the talk.</summary>
internal static class Narrator
{
    public static void Header(string title)
    {
        var rule = new string('=', title.Length + 4);
        WriteLine(ConsoleColor.Cyan, $"\n{rule}\n  {title}\n{rule}");
    }

    public static void Step(string title) => WriteLine(ConsoleColor.Yellow, $"\n--- {title} ---");

    public static void Say(string text) => Console.WriteLine(text);

    public static void Code(string code) => WriteLine(ConsoleColor.DarkGray, $"    {code}");

    public static void Outcome(string text) => WriteLine(ConsoleColor.Green, $"  -> {text}");

    public static void Problem(string text) => WriteLine(ConsoleColor.Red, $"  !! {text}");

    public static void Pause()
    {
        if (Console.IsInputRedirected)
        {
            return;
        }

        WriteLine(ConsoleColor.DarkGray, "\n(press any key to continue)");
        Console.ReadKey(intercept: true);
    }

    private static void WriteLine(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

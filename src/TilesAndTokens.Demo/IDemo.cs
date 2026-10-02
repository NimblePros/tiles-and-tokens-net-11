namespace TilesAndTokens.Demo;

internal interface IDemo
{
    /// <summary>The command-line name, e.g. "legacy".</summary>
    string Command { get; }

    string Title { get; }

    void Run();
}

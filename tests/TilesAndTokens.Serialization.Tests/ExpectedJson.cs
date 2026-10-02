using System.Runtime.CompilerServices;

namespace TilesAndTokens.Serialization.Tests;

/// <summary>Compares JSON against a committed file in the Expected folder.</summary>
/// <remarks>
/// On a mismatch (or a missing file), the actual JSON is written next to the expected file as
/// <c>name.received.json</c>. Review it; if the change is intended, rename it over the expected file.
/// </remarks>
internal static class ExpectedJson
{
    public static void ShouldMatch(string actual, string name, [CallerFilePath] string callerPath = "")
    {
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Expected", name);
        var receivedPath = Path.Combine(Path.GetDirectoryName(callerPath)!, "Expected", Path.ChangeExtension(name, ".received.json"));

        var expected = File.Exists(expectedPath) ? Normalize(File.ReadAllText(expectedPath)) : null;
        if (expected == Normalize(actual))
        {
            File.Delete(receivedPath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(receivedPath)!);
        File.WriteAllText(receivedPath, Normalize(actual));
        throw new ShouldAssertException(expected is null
            ? $"No expected file Expected/{name}. Wrote the actual JSON to {receivedPath} for review."
            : $"JSON doesn't match Expected/{name}. Wrote the actual JSON to {receivedPath} for review.");
    }

    private static string Normalize(string json) => json.ReplaceLineEndings("\n").TrimEnd() + "\n";
}

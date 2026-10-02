using System.Collections.Concurrent;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TilesAndTokens.Persistence.Tests;

/// <summary>
/// The talk's closing demo: the real AppHost starts Postgres and runs `demo all`.
/// Slow (it seeds 1,000 games), so it runs once, against its own Postgres.
/// </summary>
public class AppHostDemoTests
{
    [Fact]
    public async Task AppHost_RunsTheWholeSeries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TilesAndTokens_AppHost>(cancellationToken);

        // The testing builder forwards each resource's console output to ILogger, category
        // "<AppHost>.Resources.<resource>". Capture the demo runner's.
        var demoOutput = new CapturingLoggerProvider("TilesAndTokens.AppHost.Resources.demo");
        builder.Services.AddLogging(logging => logging.AddProvider(demoOutput));

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var finished = await app.ResourceNotifications
            .WaitForResourceAsync("demo", e => e.Snapshot.State?.Text == KnownResourceStates.Finished, cancellationToken)
            .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // let the last lines arrive

        var output = demoOutput.Text;
        finished.Snapshot.ExitCode.ShouldBe(0, output);
        output.ShouldContain("Post 1: The \"before\" picture");
        output.ShouldContain("Post 4: System.Text.Json and union state");
        output.ShouldContain("Applied migrations at startup");
        output.ShouldContain("IX_SavedGames_Summary_Leader_PlayerId"); // only appears in a query plan
        output.ShouldContain("Edge query finds it for alice-finale: True");
        output.ShouldNotContain("Skipped: no database");
    }

    private sealed class CapturingLoggerProvider(string category) : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _lines = new();

        public string Text => string.Join('\n', _lines);

        public ILogger CreateLogger(string categoryName) =>
            categoryName == category ? new CapturingLogger(_lines) : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                lines.Enqueue(formatter(state, exception));
        }
    }
}

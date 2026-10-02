using TilesAndTokens.Demo;

var noPause = args.Contains("--no-pause");
var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));

return DemoRunner.Run(command, noPause);

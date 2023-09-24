using DockerGen;
using Spectre.Console.Cli;

var app = new CommandApp<GenerateCommand>();
return await app.RunAsync(args);
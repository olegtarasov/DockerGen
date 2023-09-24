using Fluid;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DockerGen;

public class GenerateCommand : AsyncCommand<GenerateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, GenerateSettings settings)
    {
        string workingDir = Path.IsPathRooted(settings.Directory)
            ? settings.Directory
            : PathHelpers.MakeAbsolute(settings.Directory, Environment.CurrentDirectory);
        string templatePath = Path.IsPathRooted(settings.Template)
            ? settings.Template
            : Path.Combine(workingDir, settings.Template);
        string source = File.ReadAllText(templatePath);
        
        var projects = PathHelpers.FindFilesRecursive(workingDir, "*.csproj", true)
            .Select(x => new ProjectModel
                         {
                             RelativeDir = Path.GetDirectoryName(PathHelpers.MakeRelative(x, workingDir)),
                             FileName = Path.GetFileName(x),
                             Name = Path.GetFileNameWithoutExtension(x)
                         })
            .ToArray();
        var namedProjects = projects.ToDictionary(x => x.Name, x => x);
        string solution = settings.Solution;
        if (string.IsNullOrEmpty(solution))
        {
            var solutions = Directory.EnumerateFiles(workingDir, "*.sln").ToArray();
            if (solutions.Length == 0)
                throw new InvalidOperationException("Couldn't find a single solution in working directory");

            solution = Path.GetFileName(solutions[0]);
            
            if (solutions.Length > 1)
                AnsiConsole.WriteLine($"There are more than one solution in working directory, using {solution}");
        }

        var model = new DockerfileModel
                    {
                        Projects = projects,
                        PublishProjects = settings.PublishProjects.Select(x =>
                        {
                            if (!namedProjects.TryGetValue(x, out var project))
                                throw new InvalidOperationException($"Project {x} not found!");

                            return project;
                        }).ToArray(),
                        Solution = solution
                    };

        var parser = new FluidParser();
        if (!parser.TryParse(source, out var template))
        {
            throw new InvalidDataException("Failed to parse the template!");
        }

        var ctx = new TemplateContext(model, new TemplateOptions
                                                {
                                                    MemberAccessStrategy = new UnsafeMemberAccessStrategy()
                                                });
        string text = await template.RenderAsync(ctx);

        string outFileName = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(templatePath));

        File.WriteAllText(outFileName, text);
        
        return 0;
    }
}
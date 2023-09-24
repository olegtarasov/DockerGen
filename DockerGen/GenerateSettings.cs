using Spectre.Console;
using Spectre.Console.Cli;

namespace DockerGen;

public class GenerateSettings : CommandSettings
{
    [CommandArgument(0, "[project]")]
    public string[] PublishProjects { get; set; } = Array.Empty<string>();

    [CommandOption("-s|--solution")]
    public string Solution { get; set; } = string.Empty;
    
    [CommandOption("-t|--template")]
    public string Template { get; set; } = string.Empty;
    
    [CommandOption("-d|--directory")]
    public string Directory { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrEmpty(Template))
            Template = "Dockerfile.liquid";
        
        if (string.IsNullOrEmpty(Directory))
            Directory = Path.GetDirectoryName(Template);
        
        return ValidationResult.Success();
    }
}
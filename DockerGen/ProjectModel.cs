namespace DockerGen;

public class ProjectModel
{
    public required string FileName { get; set; } = string.Empty;
    public required string RelativeDir { get; set; } = string.Empty;
    public required string Name { get; set; } = string.Empty;
}
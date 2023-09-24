namespace DockerGen;

public class DockerfileModel
{
    public required ProjectModel[] Projects { get; set; } = Array.Empty<ProjectModel>();
    public required ProjectModel[] PublishProjects { get; set; } = Array.Empty<ProjectModel>();
    public required string Solution { get; set; } = string.Empty;
}
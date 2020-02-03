namespace Packages.Rider.Editor.ProjectGeneration
{
  public interface IGUIDGenerator
  {
    string ProjectGuid(string projectName, string assemblyName);
    string SolutionGuid(string projectName, string extension);
  }
}
using System.Collections.Generic;

namespace Packages.Rider.Editor.ProjectGeneration
{
  public interface IGenerator
  {
    bool SyncIfNeeded(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles);
    void Sync();
    bool HasSolutionBeenGenerated();
    string SolutionFile();
    string ProjectDirectory { get; }
    IAssemblyNameProvider AssemblyNameProvider { get; }
    void GenerateAll(bool generateAll);
  }
}
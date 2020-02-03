namespace Packages.Rider.Editor.ProjectGeneration
{
  public interface IFileIO
  {
    bool Exists(string fileName);

    string ReadAllText(string fileName);
    void WriteAllText(string fileName, string content);
  }
}
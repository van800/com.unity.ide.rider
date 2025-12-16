using System.Text;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal sealed class SdkStyleProjectGeneration : ProjectGeneration
  {
    public SdkStyleProjectGeneration() : base() {}

    public SdkStyleProjectGeneration(string projectDirectory, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIoProvider, IGUIDGenerator guidGenerator)
      : base(projectDirectory, assemblyNameProvider, fileIoProvider, guidGenerator) {}

    protected override void AppendProjectStartTag(StringBuilder stringBuilder)
    {
      stringBuilder.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
    }

    protected override void AppendProjectFooter(StringBuilder stringBuilder)
    {
    }

    protected override void AppendProjectProperties(StringBuilder stringBuilder)
    {
      stringBuilder
        .AppendLine(
          "    <!-- hardcoded target framework has no real effect, Unity provides all the BCL dlls anyway. May affect Nuget, need to check later -->")
        .AppendLine("    <TargetFramework>netstandard2.1</TargetFramework>")
        .AppendLine("    <NoStandardLibraries>true</NoStandardLibraries>")
        .AppendLine("    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>")
        .AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>")
        .AppendLine("    <EnableDefaultItems>false</EnableDefaultItems>")
        .AppendLine("    <RestoreProjectStyle>None</RestoreProjectStyle>")
        ;      
    }
  }
}
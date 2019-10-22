using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Moq;
using Packages.Rider.Editor.ProjectGeneration;
using UnityEditor.Compilation;
using UnityEditor.VisualStudioIntegration;

namespace Packages.Rider.Editor.Tests
{
    public class SynchronizerBuilder
    {
        class BuilderError : Exception
        {
            public BuilderError(string message)
                : base(message) { }
        }

        IGenerator m_Synchronizer;
        Mock<IAssemblyNameProvider> m_AssemblyProvider = new Mock<IAssemblyNameProvider>();
        const string k_ProjectDirectory = "/FullPath/Example";

        MockFileIO m_FileIoMock = new MockFileIO();
        Mock<IGUIDGenerator> m_GUIDGenerator = new Mock<IGUIDGenerator>();

        public string ReadFile(string fileName) => m_FileIoMock.ReadAllText(fileName);
        public string ReadProjectFile(Assembly assembly) => ReadFile(Path.Combine(k_ProjectDirectory, $"{assembly.name}.csproj"));
        public bool FileExists(string fileName) => m_FileIoMock.Exists(fileName);
        public int WriteTimes => m_FileIoMock.WriteTimes;
        public int ReadTimes => m_FileIoMock.ReadTimes;

        public Assembly Assembly
        {
            get
            {
                if (m_Assemblies.Length > 0)
                {
                    return m_Assemblies[0];
                }

                throw new BuilderError("An empty list of assemblies has been populated, and then the first assembly was requested.");
            }
        }

        Assembly[] m_Assemblies;

        public SynchronizerBuilder()
        {
            WithAssemblyData();
        }

        public IGenerator Build()
        {
            return m_Synchronizer = new ProjectGeneration.ProjectGeneration(k_ProjectDirectory, m_AssemblyProvider.Object, m_FileIoMock, m_GUIDGenerator.Object);
        }

        public SynchronizerBuilder WithSolutionText(string solutionText)
        {
            if (m_Synchronizer == null)
            {
                throw new BuilderError("You need to call Build() before calling this method.");
            }

            m_FileIoMock.WriteAllText(m_Synchronizer.SolutionFile(), solutionText);
            return this;
        }

        public SynchronizerBuilder WithSolutionGuid(string solutionGuid)
        {
            m_GUIDGenerator.Setup(x => x.SolutionGuid(Path.GetFileName(k_ProjectDirectory), "cs")).Returns(solutionGuid);
            return this;
        }

        public SynchronizerBuilder WithProjectGuid(string projectGuid, Assembly assembly)
        {
            m_GUIDGenerator.Setup(x => x.ProjectGuid(Path.GetFileName(k_ProjectDirectory), assembly.name)).Returns(projectGuid);
            return this;
        }

        public SynchronizerBuilder WithAssemblies(Assembly[] assemblies)
        {
            m_Assemblies = assemblies;
            m_AssemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(m_Assemblies);
            return this;
        }

        public SynchronizerBuilder WithAssemblyData(string[] files = null, string[] defines = null, Assembly[] assemblyReferences = null, string[] compiledAssemblyReferences = null)
        {
            return WithAssembly(
                new Assembly(
                    "Test",
                    "some/path/file.dll",
                    files ?? new[] { "test.cs" },
                    defines ?? new string[0],
                    assemblyReferences ?? new Assembly[0],
                    compiledAssemblyReferences ?? new string[0],
                    AssemblyFlags.None)
            );
        }

        public SynchronizerBuilder WithAssembly(Assembly assembly)
        {
            return WithAssemblies(new[] { assembly });
        }

        public SynchronizerBuilder WithAssetFiles(string[] files)
        {
            m_AssemblyProvider.Setup(x => x.GetAllAssetPaths()).Returns(files);
            return this;
        }

        public SynchronizerBuilder AssignFilesToAssembly(string[] files, Assembly assembly)
        {
            m_AssemblyProvider.Setup(x => x.GetAssemblyNameFromScriptPath(It.Is<string>(file => files.Contains(file.Substring(0, file.Length - ".cs".Length))))).Returns(assembly.name);
            return this;
        }

        public SynchronizerBuilder WithResponseFileData(Assembly assembly, string responseFile, string[] defines = null, string[] errors = null, string[] fullPathReferences = null, string[] otherArguments = null, bool _unsafe = false)
        {
            assembly.compilerOptions.ResponseFiles = new[] { responseFile };
            m_AssemblyProvider.Setup(x => x.ParseResponseFile(responseFile, k_ProjectDirectory, It.IsAny<string[]>())).Returns(new ResponseFileData
            {
                Defines = defines ?? new string[0],
                Errors = errors ?? new string[0],
                FullPathReferences = fullPathReferences ?? new string[0],
                OtherArguments = otherArguments ?? new string[0],
                Unsafe = _unsafe,
            });
            return this;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Moq;
using Packages.Rider.Editor.ProjectGeneration;
using Packages.Rider.Editor.Util;
using UnityEditor.Compilation;

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
        public static string ProjectDirectory = Path.GetFullPath("/FullPath/Example");

        MockFileIO m_FileIoMock = new MockFileIO();
        Mock<IGUIDGenerator> m_GUIDGenerator = new Mock<IGUIDGenerator>();

        public string ReadFile(string fileName) => m_FileIoMock.ReadAllText(fileName);
        public static string ProjectFilePath(Assembly assembly) => Path.Combine(ProjectDirectory, $"{assembly.name}.csproj").NormalizePath();
        public string ReadProjectFile(Assembly assembly) => ReadFile(ProjectFilePath(assembly));
        public bool FileExists(string fileName) => m_FileIoMock.Exists(fileName);
        public void DeleteFile(string fileName) => m_FileIoMock.DeleteFile(fileName);
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

        internal IGenerator Build()
        {
            return m_Synchronizer = new ProjectGeneration.ProjectGeneration(ProjectDirectory, m_AssemblyProvider.Object, m_FileIoMock, m_GUIDGenerator.Object);
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

        public SynchronizerBuilder WithProjectGuid(string projectGuid, Assembly assembly)
        {
            m_GUIDGenerator.Setup(x => x.ProjectGuid(Path.GetFileName(ProjectDirectory) + assembly.name)).Returns(projectGuid);
            return this;
        }

        public SynchronizerBuilder WithAssemblies(Assembly[] assemblies)
        {
            m_Assemblies = assemblies;
            m_AssemblyProvider.Setup(x => x.GetAllAssemblies()).Returns(m_Assemblies);
            foreach (var assembly in assemblies)
            {
                m_AssemblyProvider.Setup(x => x.GetProjectName(assembly.name, assembly.defines)).Returns(assembly.name);
                m_AssemblyProvider.Setup(x => x.GetNamedAssembly(assembly.name)).Returns(assembly);
            }
            return this;
        }

        public SynchronizerBuilder WithAssemblyData
        (string[] files = null,
            string[] defines = null,
            Assembly[] assemblyReferences = null,
            string[] compiledAssemblyReferences = null,
            ScriptCompilerOptions options = null,
            string rootNamespace = "")
        {
            // ReSharper disable once ConvertToNullCoalescingCompoundAssignment
            options = options ?? new ScriptCompilerOptions();
            Assembly assembly = CreateAssembly(
                "Test",
                "some/path/file.dll",
                files ?? new[] {"test.cs"},
                defines ?? new string[0],
                assemblyReferences ?? new Assembly[0],
                compiledAssemblyReferences ?? new string[0],
                AssemblyFlags.None,
                options,
                rootNamespace
            );

            var builder = WithAssembly(assembly);
            foreach (var assemblyReference in assembly.assemblyReferences)
            {
                builder.WithNameForDefines(assemblyReference.defines, assemblyReference.name, assemblyReference.name);
            }

            return builder;
        }

#if UNITY_2020_2_OR_NEWER
        private Assembly CreateAssembly(string name, string path, string[] files, string[] defines, Assembly[] assemblyReferences, string[] compiledAssemblyReferences, AssemblyFlags flags, ScriptCompilerOptions options, string rootNamespace)
        {
            return new Assembly(name, path, files, defines, assemblyReferences, compiledAssemblyReferences, flags, options, rootNamespace);
        }
#else
        private Assembly CreateAssembly(string name, string path, string[] files, string[] defines, Assembly[] assemblyReferences, string[] compiledAssemblyReferences, AssemblyFlags flags, ScriptCompilerOptions options, string rootNamespace)
        {
            return new Assembly(name, path, files, defines, assemblyReferences, compiledAssemblyReferences, flags, options);
        }
#endif

        public SynchronizerBuilder WithAssembly(Assembly assembly)
        {
            return WithAssemblies(new[] { assembly });
        }

        public SynchronizerBuilder WithAssetFiles(string[] files)
        {
            m_AssemblyProvider.Setup(x => x.GetAllAssetPaths()).Returns(files);
            return this;
        }

#if UNITY_2020_2_OR_NEWER
        public SynchronizerBuilder WithRoslynAnalyzerRulesetPath(string roslynAnalyzerRuleSetPath)
        {
            foreach (var assembly in m_Assemblies)
            {
                assembly.compilerOptions.RoslynAnalyzerRulesetPath = roslynAnalyzerRuleSetPath;
            }
            return this;
        }

        public SynchronizerBuilder WithRoslynAnalyzers(string[] roslynAnalyzerDllPaths)
        {
#if !ROSLYN_ANALYZER_FIX
            m_AssemblyProvider.Setup(x => x.GetRoslynAnalyzerPaths()).Returns(roslynAnalyzerDllPaths);
#else
            foreach (var assembly in m_Assemblies)
            {
                assembly.compilerOptions.RoslynAnalyzerDllPaths = roslynAnalyzerDllPaths;
            }
#endif
            return this;
        }
#endif

        public SynchronizerBuilder AssignFilesToAssembly(string[] files, Assembly assembly)
        {
            m_AssemblyProvider.Setup(x => x.GetAssemblyNameFromScriptPath(It.Is<string>(file => files.Contains(file.Substring(0, file.Length - ".cs".Length))))).Returns(assembly.name);
            return this;
        }

        public SynchronizerBuilder WithResponseFileData(Assembly assembly, string responseFile, string[] defines = null, string[] errors = null, string[] fullPathReferences = null, string[] otherArguments = null, bool _unsafe = false)
        {
            assembly.compilerOptions.ResponseFiles = new[] { responseFile };
            m_AssemblyProvider.Setup(x => x.ParseResponseFile(responseFile, ProjectDirectory.NormalizePath(), It.IsAny<string[]>())).Returns(new ResponseFileData
            {
                Defines = defines ?? new string[0],
                Errors = errors ?? new string[0],
                FullPathReferences = fullPathReferences ?? new string[0],
                OtherArguments = otherArguments ?? new string[0],
                Unsafe = _unsafe,
            });
            return this;
        }

        public SynchronizerBuilder WithPackageInfo(string assetPath)
        {
            m_AssemblyProvider.Setup(x => x.FindForAssetPath(assetPath)).Returns(default(UnityEditor.PackageManager.PackageInfo));
            return this;
        }

        public SynchronizerBuilder WithPackageAsset(string assetPath, bool isInternalPackageAsset)
        {
            m_AssemblyProvider.Setup(x => x.IsInternalizedPackagePath(assetPath)).Returns(isInternalPackageAsset);
            return this;
        }

        public SynchronizerBuilder WithProjectSupportedExtensions(string[] extensions)
        {
            m_AssemblyProvider.Setup(x => x.ProjectSupportedExtensions).Returns(extensions);
            return this;
        }

        public SynchronizerBuilder WithNameForDefines(string[] defines, string assemblyName, string resAssemblyName)
        {
            m_AssemblyProvider.Setup(x => x.GetProjectName(assemblyName, defines)).Returns(resAssemblyName);
            return this;
        }
    }
}

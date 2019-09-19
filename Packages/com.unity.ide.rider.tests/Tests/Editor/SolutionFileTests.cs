using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Packages.Rider.Tests.Editor.CSProjectGeneration;
using Unity.CodeEditor;
using UnityEngine;
using UnityEditor.Compilation;

namespace Packages.Rider.Tests.Editor
{
    namespace SolutionGeneration
    {
        public class Synchronization
        {
            const string k_ProjectDirectory = "/FullPath/Example";
            string m_EditorPath;

            [OneTimeSetUp]
            public void OneTimeSetUp()
            {
                m_EditorPath = CodeEditor.CurrentEditorInstallation;
                CodeEditor.SetExternalScriptEditor("NotSet");
            }

            [OneTimeTearDown]
            public void OneTimeTearDown()
            {
                CodeEditor.SetExternalScriptEditor(m_EditorPath);
            }

            [Test]
            public void IfNotExisting_Synchronize()
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Test", "some/path/file.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var fileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, fileIO, new MockGUIDProvider());

                synchronizer.Sync();

                Assert.True(fileIO.Exists(synchronizer.SolutionFile()), "Should create solution file.");
            }

            [Test]
            public void DoesNotChange_WhenSyncedTwice()
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Test", "some/path/file.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var fileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, fileIO, new MockGUIDProvider());

                synchronizer.Sync();
                var textBefore = fileIO.ReadAllText(synchronizer.SolutionFile());
                synchronizer.Sync();
                var textAfter = fileIO.ReadAllText(synchronizer.SolutionFile());

                Assert.AreEqual(textBefore, textAfter, "Content does not change on re-sync");
            }

            [Test]
            public void Overwrite_EmptySolutionFile()
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Test", "some/path/file.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var fileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, fileIO, new MockGUIDProvider());

                string originalText = "Microsoft Visual Studio Solution File, Format Version 10.00\n# Visual Studio 2008\nGlobal\nEndGlobal";
                // Pre-seed solution file with empty property section
                fileIO.WriteAllText(synchronizer.SolutionFile(), originalText);

                synchronizer.Sync();

                var syncedSolutionText = fileIO.ReadAllText(synchronizer.SolutionFile());
                Assert.AreNotEqual(originalText, syncedSolutionText, "Should rewrite solution text");
            }

            [TestCase("reimport.dll", true)]
            [TestCase("reimport.asmdef", true)]
            [TestCase("dontreimport.someOther", false)]
            public void AfterSync_WillResync_WhenReimportFileTypes(string reimportedFile, bool expected)
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Test", "some/path/file.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, new MockFileIO(), new MockGUIDProvider());

                synchronizer.Sync();

                Assert.AreEqual(expected, synchronizer.SyncIfNeeded(Enumerable.Empty<string>(), new[] { reimportedFile }));
            }
        }

        public class Format
        {
            const string k_ProjectDirectory = "/FullPath/Example";

            [Test]
            public void Header_MatchesVSVersion()
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Test", "some/path/file.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var fileIO = new MockFileIO();
                var mockGUIDProvider = new MockGUIDProvider();
                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, fileIO, mockGUIDProvider);

                synchronizer.Sync();

                string[] syncedSolutionText = fileIO.ReadAllText(synchronizer.SolutionFile()).Split(new[] { "\r\n" }, StringSplitOptions.None);
                Assert.IsTrue(syncedSolutionText.Length >= 4);
                Assert.AreEqual(@"", syncedSolutionText[0]);
                Assert.AreEqual(@"Microsoft Visual Studio Solution File, Format Version 11.00", syncedSolutionText[1]);
                Assert.AreEqual(@"# Visual Studio 2010", syncedSolutionText[2]);
                Assert.IsTrue(syncedSolutionText[3].StartsWith($"Project(\"{{{mockGUIDProvider.SolutionGuid("Example", "cs")}}}\")"));
            }

            [Test]
            public void Matches_DefaultProjectGeneration()
            {
                var assemblyProvider = new Mock<IAssemblyNameProvider>();
                var assembly = new Assembly("Assembly2", "/User/Test/Assembly2.dll", new[] { "File.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assemblyProvider.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var mockGUIDProvider = new MockGUIDProvider();
                var synchronizer = new ProjectGeneration(k_ProjectDirectory, assemblyProvider.Object, mockFileIO, mockGUIDProvider);

                synchronizer.Sync();

                // solutionguid, solutionname, projectguid
                var solutionExpected = string.Join("\r\n", new[]
                {
                    @"",
                    @"Microsoft Visual Studio Solution File, Format Version 11.00",
                    @"# Visual Studio 2010",
                    @"Project(""{{{0}}}"") = ""{2}"", ""{2}.csproj"", ""{{{1}}}""",
                    @"EndProject",
                    @"Global",
                    @"    GlobalSection(SolutionConfigurationPlatforms) = preSolution",
                    @"        Debug|Any CPU = Debug|Any CPU",
                    @"        Release|Any CPU = Release|Any CPU",
                    @"    EndGlobalSection",
                    @"    GlobalSection(ProjectConfigurationPlatforms) = postSolution",
                    @"        {{{1}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                    @"        {{{1}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
                    @"        {{{1}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
                    @"        {{{1}}}.Release|Any CPU.Build.0 = Release|Any CPU",
                    @"    EndGlobalSection",
                    @"    GlobalSection(SolutionProperties) = preSolution",
                    @"        HideSolutionNode = FALSE",
                    @"    EndGlobalSection",
                    @"EndGlobal",
                    @""
                }).Replace("    ", "\t");

                var solutionTemplate = string.Format(
                    solutionExpected,
                    mockGUIDProvider.SolutionGuid("Example", "cs"),
                    mockGUIDProvider.ProjectGuid("Example", assembly.outputPath),
                    assembly.name);

                Assert.AreEqual(solutionTemplate, mockFileIO.ReadAllText(synchronizer.SolutionFile()));
            }
        }
    }
}
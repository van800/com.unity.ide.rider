using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Moq;
using NUnit.Framework;
using Unity.CodeEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Packages.Rider.Tests.Editor
{
    namespace CSProjectGeneration
    {
        static class Util
        {
            internal static bool MatchesRegex(this string input, string pattern)
            {
                return Regex.Match(input, pattern).Success;
            }
        }

        public class MockFileIO : FileIO
        {
            Dictionary<string, string> fileToContent = new Dictionary<string, string>();
            public int WriteTimes { get; private set; }
            public int ReadTimes { get; private set; }
            public int ExistTimes { get; private set; }

            public bool Exists(string fileName)
            {
                ++ExistTimes;
                return fileToContent.ContainsKey(fileName);
            }

            public string ReadAllText(string fileName)
            {
                ++ReadTimes;
                return fileToContent[fileName];
            }

            public void WriteAllText(string fileName, string content)
            {
                ++WriteTimes;
                var utf8 = Encoding.UTF8;
                byte[] utfBytes = utf8.GetBytes(content);
                fileToContent[fileName] = utf8.GetString(utfBytes, 0, utfBytes.Length);
            }
        }

        public class MockGUIDProvider : GUIDGenerator
        {
            public string ProjectGuid(string projectName, string assemblyName)
            {
                return projectName + assemblyName;
            }

            public string SolutionGuid(string projectName, string extension)
            {
                return projectName + extension;
            }
        }

        public class Formatting
        {
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

            [TestCase(@"x & y.cs", @"x &amp; y.cs")]
            [TestCase(@"x ' y.cs", @"x &apos; y.cs")]
            [TestCase(@"Dimmer&/foo.cs", @"Dimmer&amp;\foo.cs")]
            public void Escape_SpecialCharsInFileName(string input, string expected)
            {
                var mock = new Mock<IAssemblyNameProvider>();

                var assembly = new Assembly("Assembly", "/User/Test/Assembly.dll", new[] { input }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojContent = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.DoesNotContain(input, csprojContent);
                StringAssert.Contains(expected, csprojContent);
            }
        }

        public class GUID
        {
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
            public void ProjectReference_MatchAssemblyGUID()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var projectDirectory = "/FullPath/Example";
                var assemblyB = new Assembly("Test", "Temp/Test.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assemblyA = new Assembly("Test2", "some/path/file.dll", files, new string[0], new[] { assemblyB }, new[] { "Library.ScriptAssemblies.Test.dll" }, AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assemblyA, assemblyB });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var assemblyACSproject = Path.Combine(projectDirectory, $"{assemblyA.name}.csproj");
                var assemblyBCSproject = Path.Combine(projectDirectory, $"{assemblyB.name}.csproj");

                Assert.True(mockFileIO.Exists(assemblyACSproject));
                Assert.True(mockFileIO.Exists(assemblyBCSproject));

                XmlDocument scriptProject = XMLUtilities.FromText(mockFileIO.ReadAllText(assemblyACSproject));
                XmlDocument scriptPluginProject = XMLUtilities.FromText(mockFileIO.ReadAllText(assemblyBCSproject));

                var xmlNamespaces = new XmlNamespaceManager(scriptProject.NameTable);
                xmlNamespaces.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");

                var a = scriptPluginProject.SelectSingleNode("/msb:Project/msb:PropertyGroup/msb:ProjectGuid", xmlNamespaces).InnerText;
                var b = scriptProject.SelectSingleNode("/msb:Project/msb:ItemGroup/msb:ProjectReference/msb:Project", xmlNamespaces).InnerText;
                Assert.AreEqual(a, b);
            }
        }

        public class Synchronization
        {
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
            public void WontSynchronize_WhenNoFilesChanged()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());
                synchronizer.Sync();
                Assert.AreEqual(2, mockFileIO.WriteTimes, "One write for solution and one write for csproj");

                synchronizer.Sync();
                Assert.AreEqual(2, mockFileIO.WriteTimes, "No more files should be written");
            }
        }

        public class SourceFiles
        {
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
            public void NotContributedAnAssembly_WillNotGetAdded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "File.cs",
                };
                var assembly = new Assembly("Assembly2", "/User/Test/Assembly2.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(string.Empty);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(new[] { "File/Not/In/Assembly.hlsl" });

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();
                var csprojContent = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.DoesNotContain("Assembly.hlsl", csprojContent);
            }

            [Test]
            public void RelativePackages_GetsPathResolvedCorrectly()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "/FullPath/ExamplePackage/Packages/Asset.cs",
                };
                var assembly = new Assembly("ExamplePackage", "/FullPath/Example/ExamplePackage/ExamplePackage.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(string.Empty);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(files);
                mock.Setup(x => x.FindForAssetPath("/FullPath/ExamplePackage/Packages/Asset.cs")).Returns(default(UnityEditor.PackageManager.PackageInfo));

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                StringAssert.Contains(files[0].Replace('/', '\\'), mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj")));
            }

            [Test]
            public void CSharpFiles_WillBeIncluded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "Assets/Script.cs",
                };
                var assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(string.Empty);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(files);

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                StringAssert.Contains(files[0].Replace('/', '\\'), mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj")));
            }

            [Test]
            public void NonCSharpFiles_AddedToNonCompileItems()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "Script.cs",
                };
                var nonCompileItems = new[]
                {
                    "ClassDiagram1.cd",
                    "text.txt",
                    "Test.shader",
                };
                var assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(assembly.name);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(files.Concat(nonCompileItems));

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojectContent = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                var xmlDocument = XMLUtilities.FromText(csprojectContent);
                XMLUtilities.AssertCompileItemsMatchExactly(xmlDocument, new[] { files[0] });
                XMLUtilities.AssertNonCompileItemsMatchExactly(xmlDocument, files.Skip(1));
            }

            [Test]
            public void AddedAfterSync_WillBeSynced()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var filesBefore = new[]
                {
                    "Script.cs",
                };
                var filesAfter = new[]
                {
                    "Script.cs",
                    "Newfile.cs",
                };
                var assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesBefore, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(assembly.name);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(new string[0]);

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojContentBefore = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.DoesNotContain(filesAfter[1], csprojContentBefore);

                assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesAfter, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                Assert.True(synchronizer.SyncIfNeeded(filesAfter.Skip(1), new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.Contains(filesAfter[1], csprojContentAfter);
            }

            [Test]
            public void Moved_WillBeResynced()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var filesBefore = new[]
                {
                    "OldScript.cs",
                };
                var filesAfter = new[]
                {
                    "NewScript.cs",
                };
                var assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesBefore, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(assembly.name);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(new string[0]);

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesAfter, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                Assert.True(synchronizer.SyncIfNeeded(filesAfter, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.Contains(filesAfter[0], csprojContentAfter);
                StringAssert.DoesNotContain(filesBefore[0], csprojContentAfter);
            }

            [Test]
            public void Deleted_WillBeRemoved()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var filesBefore = new[]
                {
                    "WillBeDeletedScript.cs",
                    "Script.cs",
                };
                var filesAfter = new[]
                {
                    "Script.cs",
                };
                var assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesBefore, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAssemblyNameFromScriptPath(It.IsAny<string>())).Returns(assembly.name);
                mock.Setup(x => x.GetAllAssetPaths()).Returns(new string[0]);

                var projectDirectory = "/FullPath/Example";
                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojContentBefore = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.Contains(filesBefore[0], csprojContentBefore);
                StringAssert.Contains(filesBefore[1], csprojContentBefore);

                assembly = new Assembly("Assembly", "/Path/To/Assembly.dll", filesAfter, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                Assert.True(synchronizer.SyncIfNeeded(filesAfter, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));
                StringAssert.Contains(filesAfter[0], csprojContentAfter);
                StringAssert.DoesNotContain(filesBefore[0], csprojContentAfter);
            }
        }

        public class CompilerOptions
        {
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
            public void AllowUnsafeBlock()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var responseFileData = new ResponseFileData
                {
                    Defines = new string[0],
                    FullPathReferences = new string[0],
                    Errors = new string[0],
                    OtherArguments = new string[0],
                    Unsafe = true,
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assembly.compilerOptions.ResponseFiles = new[] { "csc.rsp" };
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.ParseResponseFile("csc.rsp", projectDirectory, It.IsAny<string[]>())).Returns(responseFileData);

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                StringAssert.Contains("<AllowUnsafeBlocks>True</AllowUnsafeBlocks>", csprojFileContents);
            }
        }

        public class References
        {
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
            public void PathWithSpaces_IsParsedCorrectly()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var responseFileData = new ResponseFileData
                {
                    Defines = new string[0],
                    FullPathReferences = new[] { "Folder/Path With Space/Goodbye.dll" },
                    Errors = new string[0],
                    OtherArguments = new string[0],
                    Unsafe = false,
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assembly.compilerOptions.ResponseFiles = new[] { "csc.rsp" };
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.ParseResponseFile("csc.rsp", projectDirectory, It.IsAny<string[]>())).Returns(responseFileData);

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Goodbye\">\\W*<HintPath>Folder/Path With Space/Goodbye.dll\\W*</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void Multiple_CanBeAdded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var responseFileData = new ResponseFileData
                {
                    Defines = new string[0],
                    FullPathReferences = new[] { "MyPlugin.dll", "Hello.dll" },
                    Errors = new string[0],
                    OtherArguments = new string[0],
                    Unsafe = false,
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assembly.compilerOptions.ResponseFiles = new[] { "csc.rsp" };
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.ParseResponseFile("csc.rsp", projectDirectory, It.IsAny<string[]>())).Returns(responseFileData);

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Hello\">\\W*<HintPath>Hello.dll</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"MyPlugin\">\\W*<HintPath>MyPlugin.dll</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void AssemblyReference_AreAdded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var assemblyReferences = new[]
                {
                    new Assembly("MyPlugin", "/some/path/MyPlugin.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                    new Assembly("Hello", "/some/path/Hello.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], assemblyReferences, new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex($"<Reference Include=\"{assemblyReferences[0].name}\">\\W*<HintPath>{assemblyReferences[0].outputPath}</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex($"<Reference Include=\"{assemblyReferences[1].name}\">\\W*<HintPath>{assemblyReferences[1].outputPath}</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void CompiledAssemblyReference_AreAdded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var compiledAssemblyReferences = new[]
                {
                    "/some/path/MyPlugin.dll",
                    "/some/other/path/Hello.dll",
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], compiledAssemblyReferences, AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Hello\">\\W*<HintPath>/some/other/path/Hello.dll</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"MyPlugin\">\\W*<HintPath>/some/path/MyPlugin.dll</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void AddsProjectReference_FromLibraryReferences()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var projectDirectory = "/FullPath/Example";
                var projectAssembly = new Assembly("ProjectAssembly", "/path/to/project.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new[] { projectAssembly }, new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsFalse(csprojFileContents.MatchesRegex($"<Reference Include=\"{projectAssembly.name}\">\\W*<HintPath>{projectAssembly.outputPath}</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void ReferenceNotInAssembly_WontBeAdded()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var projectDirectory = "/FullPath/Example";
                var projectAssembly = new Assembly("ProjectAssembly", "/path/to/project.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new[] { projectAssembly }, new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.GetAllAssetPaths()).Returns(new[] { "some.dll" });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                StringAssert.DoesNotContain("some.dll", csprojFileContents);
            }
        }

        public class Defines
        {
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
            public void ResponseFiles_CanAddDefines()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var responseFileData = new ResponseFileData
                {
                    Defines = new[] { "DEF1", "DEF2" },
                    FullPathReferences = new string[0],
                    Errors = new string[0],
                    OtherArguments = new string[0],
                    Unsafe = false,
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                assembly.compilerOptions.ResponseFiles = new[] { "csc.rsp" };
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });
                mock.Setup(x => x.ParseResponseFile("csc.rsp", projectDirectory, It.IsAny<string[]>())).Returns(responseFileData);

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF1.*</DefineConstants>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF2.*</DefineConstants>"));
            }

            [Test]
            public void Assembly_CanAddDefines()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var projectDirectory = "/FullPath/Example";
                var assembly = new Assembly("Test", "some/path/file.dll", files, new[] { "DEF1", "DEF2" }, new Assembly[0], new string[0], AssemblyFlags.None);
                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assembly });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var csprojFileContents = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assembly.name}.csproj"));

                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF1.*</DefineConstants>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF2.*</DefineConstants>"));
            }

            [Test]
            public void ResponseFileDefines_OverrideRootResponseFile()
            {
                var mock = new Mock<IAssemblyNameProvider>();
                var files = new[]
                {
                    "test.cs",
                };
                var assemblyA = new Assembly("A", "some/root/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None)
                {
                    compilerOptions = { ResponseFiles = new[] { "A.rsp" } }
                };
                var assemblyB = new Assembly("B", "some/root/child/anotherfile.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None)
                {
                    compilerOptions = { ResponseFiles = new[] { "B.rsp" } }
                };
                var projectDirectory = "/FullPath/Example";

                mock.Setup(x => x.GetAssemblies(It.IsAny<Func<string, bool>>())).Returns(new[] { assemblyA, assemblyB });
                mock.Setup(x => x.ParseResponseFile("A.rsp", projectDirectory, It.IsAny<string[]>())).Returns(new ResponseFileData
                {
                    Defines = new[] { "RootedDefine" },
                    Errors = new string[0],
                    Unsafe = false,
                    OtherArguments = new string[0],
                    FullPathReferences = new string[0],
                });
                mock.Setup(x => x.ParseResponseFile("B.rsp", projectDirectory, It.IsAny<string[]>())).Returns(new ResponseFileData
                {
                    Defines = new[] { "CHILD_DEFINE" },
                    Errors = new string[0],
                    Unsafe = false,
                    OtherArguments = new string[0],
                    FullPathReferences = new string[0],
                });

                var mockFileIO = new MockFileIO();
                var synchronizer = new ProjectGeneration(projectDirectory, mock.Object, mockFileIO, new MockGUIDProvider());

                synchronizer.Sync();

                var aCsprojContent = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assemblyA.name}.csproj"));
                var bCsprojContent = mockFileIO.ReadAllText(Path.Combine(projectDirectory, $"{assemblyB.name}.csproj"));

                Assert.IsTrue(bCsprojContent.MatchesRegex("<DefineConstants>.*;CHILD_DEFINE.*</DefineConstants>"));
                Assert.IsFalse(bCsprojContent.MatchesRegex("<DefineConstants>.*;RootedDefine.*</DefineConstants>"));
                Assert.IsFalse(aCsprojContent.MatchesRegex("<DefineConstants>.*;CHILD_DEFINE.*</DefineConstants>"));
                Assert.IsTrue(aCsprojContent.MatchesRegex("<DefineConstants>.*;RootedDefine.*</DefineConstants>"));
            }
        }
    }
}
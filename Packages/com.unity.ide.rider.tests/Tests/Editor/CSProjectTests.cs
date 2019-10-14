using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
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

        public class Formatting : ProjectGenerationTestBase
        {
            [TestCase(@"x & y.cs", @"x &amp; y.cs")]
            [TestCase(@"x ' y.cs", @"x &apos; y.cs")]
            [TestCase(@"Dimmer&/foo.cs", @"Dimmer&amp;\foo.cs")]
            public void Escape_SpecialCharsInFileName(string illegalFormattedFileName, string expectedFileName)
            {
                var synchronizer = m_Builder.WithAssemblyData(files: new[] { illegalFormattedFileName }).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.DoesNotContain(illegalFormattedFileName, csprojContent);
                StringAssert.Contains(expectedFileName, csprojContent);
            }
        }

        public class GUID : ProjectGenerationTestBase
        {
            [Test]
            public void ProjectReference_MatchAssemblyGUID()
            {
                string[] files = { "test.cs" };
                var assemblyB = new Assembly("Test", "Temp/Test.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assemblyA = new Assembly("Test2", "some/path/file.dll", files, new string[0], new[] { assemblyB }, new[] { "Library.ScriptAssemblies.Test.dll" }, AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblies(new[] { assemblyA, assemblyB }).Build();

                synchronizer.Sync();

                var assemblyACSproject = Path.Combine(synchronizer.ProjectDirectory, $"{assemblyA.name}.csproj");
                var assemblyBCSproject = Path.Combine(synchronizer.ProjectDirectory, $"{assemblyB.name}.csproj");

                Assert.True(m_Builder.FileExists(assemblyACSproject));
                Assert.True(m_Builder.FileExists(assemblyBCSproject));

                XmlDocument scriptProject = XMLUtilities.FromText(m_Builder.ReadFile(assemblyACSproject));
                XmlDocument scriptPluginProject = XMLUtilities.FromText(m_Builder.ReadFile(assemblyBCSproject));

                var xmlNamespaces = new XmlNamespaceManager(scriptProject.NameTable);
                xmlNamespaces.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");

                var a = scriptPluginProject.SelectSingleNode("/msb:Project/msb:PropertyGroup/msb:ProjectGuid", xmlNamespaces).InnerText;
                var b = scriptProject.SelectSingleNode("/msb:Project/msb:ItemGroup/msb:ProjectReference/msb:Project", xmlNamespaces).InnerText;
                Assert.AreEqual(a, b);
            }
        }

        public class Synchronization : ProjectGenerationTestBase
        {
            [Test]
            public void WontSynchronize_WhenNoFilesChanged()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();
                Assert.AreEqual(2, m_Builder.WriteTimes, "One write for solution and one write for csproj");

                synchronizer.Sync();
                Assert.AreEqual(2, m_Builder.WriteTimes, "No more files should be written");
            }
        }

        public class SourceFiles : ProjectGenerationTestBase
        {
            [Test]
            public void NotContributedAnAssembly_WillNotGetAdded()
            {
                var synchronizer = m_Builder.WithAssetFiles(new[] { "Assembly.hlsl" }).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.DoesNotContain("Assembly.hlsl", csprojContent);
            }

            [Test]
            public void InRelativePackages_GetsPathResolvedCorrectly()
            {
                var assetPath = "/FullPath/ExamplePackage/Packages/Asset.cs";
                var assembly = new Assembly("ExamplePackage", "/FullPath/Example/ExamplePackage/ExamplePackage.dll", new[] { assetPath }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblies(new[] { assembly }).Build();

                synchronizer.Sync();

                StringAssert.Contains(assetPath.Replace('/', '\\'), m_Builder.ReadProjectFile(assembly));
            }

            [Test]
            public void CSharpFiles_WillBeIncluded()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();

                var assembly = m_Builder.Assembly;
                StringAssert.Contains(assembly.sourceFiles[0].Replace('/', '\\'), m_Builder.ReadProjectFile(assembly));
            }

            [Test]
            public void NonCSharpFiles_AddedToNonCompileItems()
            {
                var nonCompileItems = new[]
                {
                    "ClassDiagram1.cd",
                    "text.txt",
                    "Test.shader",
                };
                var synchronizer = m_Builder
                    .WithAssetFiles(nonCompileItems)
                    .AssignFilesToAssembly(nonCompileItems, m_Builder.Assembly)
                    .Build();

                synchronizer.Sync();

                var csprojectContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                var xmlDocument = XMLUtilities.FromText(csprojectContent);
                XMLUtilities.AssertCompileItemsMatchExactly(xmlDocument, m_Builder.Assembly.sourceFiles);
                XMLUtilities.AssertNonCompileItemsMatchExactly(xmlDocument, nonCompileItems);
            }

            [Test]
            public void AddedAfterSync_WillBeSynced()
            {
                var synchronizer = m_Builder.Build();
                synchronizer.Sync();
                const string newFile = "Newfile.cs";
                var newFileArray = new[] { newFile };
                m_Builder.WithAssemblyData(files: m_Builder.Assembly.sourceFiles.Concat(newFileArray).ToArray());

                Assert.True(synchronizer.SyncIfNeeded(newFileArray, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(newFile, csprojContentAfter);
            }

            [Test]
            public void Moved_WillBeResynced()
            {
                var synchronizer = m_Builder.Build();
                synchronizer.Sync();
                var filesBefore = m_Builder.Assembly.sourceFiles;
                const string newFile = "Newfile.cs";
                var newFileArray = new[] { newFile };
                m_Builder.WithAssemblyData(files: newFileArray);

                Assert.True(synchronizer.SyncIfNeeded(newFileArray, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(newFile, csprojContentAfter);
                foreach (var file in filesBefore)
                {
                    StringAssert.DoesNotContain(file, csprojContentAfter);
                }
            }

            [Test]
            public void Deleted_WillBeRemoved()
            {
                var filesBefore = new[]
                {
                    "WillBeDeletedScript.cs",
                    "Script.cs",
                };
                var synchronizer = m_Builder.WithAssemblyData(files: filesBefore).Build();

                synchronizer.Sync();

                var csprojContentBefore = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(filesBefore[0], csprojContentBefore);
                StringAssert.Contains(filesBefore[1], csprojContentBefore);

                var filesAfter = filesBefore.Skip(1).ToArray();
                m_Builder.WithAssemblyData(files: filesAfter);

                Assert.True(synchronizer.SyncIfNeeded(filesAfter, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(filesAfter[0], csprojContentAfter);
                StringAssert.DoesNotContain(filesBefore[0], csprojContentAfter);
            }
        }

        public class CompilerOptions : ProjectGenerationTestBase
        {
            [Test]
            public void AllowUnsafeBlock()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, _unsafe: true)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains("<AllowUnsafeBlocks>True</AllowUnsafeBlocks>", csprojFileContents);
            }

            [TestCase(new object[] {"C:/Analyzer.dll"})]
            [TestCase(new object[] {"C:/Analyzer.dll", "C:/Analyzer2.dll"})]
            [TestCase(new object[] {"../Analyzer.dll"})]
            [TestCase(new object[] {"../Analyzer.dll", "C:/Analyzer2.dll"})]
            public void AddAnalyzers(params string[] paths)
            {
                var combined = string.Join(";", paths);

                var expectedOutput = string.Empty;

                const string expectedTemplate = @"  <ItemGroup>
{0}
  </ItemGroup>";
                const string additionalFileTemplate = @"    <Analyzer Include=""{0}"" />";

                expectedOutput = string.Format(expectedTemplate, string.Join("\r\n",paths.Select(x => string.Format(additionalFileTemplate, x))));

                CheckOtherArgument(new[] {$"-a:{combined}"}, expectedOutput);
                CheckOtherArgument(new[] {$"-analyzer:{combined}"}, expectedOutput);
                CheckOtherArgument(new[] {$"/a:{combined}"}, expectedOutput);
                CheckOtherArgument(new[] {$"/analyzer:{combined}"}, expectedOutput);
            }
            
            [TestCase(new object[] {"C:/Analyzer.dll"})]
            [TestCase(new object[] {"C:/Analyzer.dll", "C:/Analyzer2.dll"})]
            [TestCase(new object[] {"../Analyzer.dll"})]
            [TestCase(new object[] {"../Analyzer.dll", "C:/Analyzer2.dll"})]
            public void AddAdditionalFile(params string[] paths)
            {
                var combined = string.Join(";", paths);

                string expectedOutput = string.Empty;

                const string expectedTemplate = @"  <ItemGroup>
{0}
  </ItemGroup>";
                const string additionalFileTemplate = @"    <AdditionalFiles Include=""{0}"" />";

                expectedOutput = string.Format(expectedTemplate, string.Join("\r\n",paths.Select(x => string.Format(additionalFileTemplate, x))));

                CheckOtherArgument(new[] {$"-additionalfile:{combined}"}, expectedOutput);
                CheckOtherArgument(new[] {$"/additionalfile:{combined}"}, expectedOutput);
            }
            
            [TestCase("0169", "0123")]
            [TestCase("0169")]
            [TestCase("0169;0123", "0234")]
            public void SetWarnAsError(params string[] errorCodes)
            {
                var combined = string.Join(";", errorCodes);

                string expectedOutput = string.Empty;

                expectedOutput = $"<WarningsAsErrors>{string.Join(";", errorCodes)}</WarningsAsErrors>";

                CheckOtherArgument(new[] {$"-warnaserror:{combined}"}, expectedOutput);
                CheckOtherArgument(new[] {$"/warnaserror:{combined}"}, expectedOutput);
            }

            [TestCase(true, "0169", "0123")]
            [TestCase(false, "0169", "0123")]
            public void SetWarnAsError(bool state, params string[] errorCodes)
            {
                string value = state ? "+" : "-";
                CheckOtherArgument(new[] {$"-warnaserror{value}"}, $"<TreatWarningsAsErrors>{state}</TreatWarningsAsErrors>");
                CheckOtherArgument(new[] {$"/warnaserror{value}"}, $"<TreatWarningsAsErrors>{state}</TreatWarningsAsErrors>");
            }
            
            [TestCase(true)]
            [TestCase(false)]
            public void SetWarnAsErrorCombined(bool state, params string[] errorCodes)
            {
                var combined = string.Join(";", errorCodes);

                string expectedWarningsAsErrorsOutput = $"<WarningsAsErrors>{string.Join(";", errorCodes)}</WarningsAsErrors>";
                string expectedTreatWarningsAsErrors = $"<TreatWarningsAsErrors>{state}</TreatWarningsAsErrors>";
                string value = state ? "+" : "-";
                CheckOtherArgument(new[] {$"-warnaserror{value}", $"-warnaserror:{combined}"}, expectedTreatWarningsAsErrors, expectedWarningsAsErrorsOutput);
                CheckOtherArgument(new[] {$"/warnaserror{value}", $"/warnaserror:{combined}"}, expectedTreatWarningsAsErrors, expectedWarningsAsErrorsOutput);
            }
            
            [TestCase(0)]
            [TestCase(4)]
            public void SetWarningLevel(int level)
            {
                string warningLevelString = $"<WarningLevel>{level}</WarningLevel>";
                CheckOtherArgument(new[] {$"-w:{level}"}, warningLevelString);
                CheckOtherArgument(new[] {$"-warn:{level}"}, warningLevelString);
                CheckOtherArgument(new[] {$"/w:{level}"}, warningLevelString);
                CheckOtherArgument(new[] {$"/warn:{level}"}, warningLevelString);
            }
            
            [TestCase("C:/rules.ruleset")]
            [TestCase("../rules.ruleset")]
            [TestCase(new object[]{"../rules.ruleset", "C:/rules.ruleset"})]
            public void SetRuleset(params string[] paths)
            {
                string rulesetTemplate = "<CodeAnalysisRuleSet>{0}</CodeAnalysisRuleSet>";
                CheckOtherArgument(paths.Select(x=>$"-ruleset:{x}").ToArray(), paths.Select(x=>string.Format(rulesetTemplate, x)).ToArray());
                CheckOtherArgument(paths.Select(x=>$"/ruleset:{x}").ToArray(), paths.Select(x=>string.Format(rulesetTemplate, x)).ToArray());
            }
            
            [TestCase("C:/docs.xml")]
            [TestCase("../docs.xml")]
            [TestCase(new object[]{"../docs.xml", "C:/docs.xml"})]
            public void SetDocumentationFile(params string[] paths)
            {
                string docTemplate = "<DocumentationFile>{0}</DocumentationFile>";
                CheckOtherArgument(paths.Select(x=>$"-doc:{x}").ToArray(), paths.Select(x=>string.Format(docTemplate, x)).ToArray());
                CheckOtherArgument(paths.Select(x=>$"/doc:{x}").ToArray(), paths.Select(x=>string.Format(docTemplate, x)).ToArray());
            }
            
            [Test]
            public void CheckDefaultWarningLevel()
            {
                CheckOtherArgument(new string[0], $"<WarningLevel>4</WarningLevel>");
            }
            
            [TestCase(new []{"-nowarn:10"}, ",10")]
            [TestCase(new []{"-nowarn:10,11"}, ",10,11")]
            [TestCase(new []{"-nowarn:10,11", "-nowarn:12"}, ",10,11,12")]
            public void CheckNoWarn(string[] args, string expected)
            {
                CheckOtherArgument(args, $"<NoWarn>0169{expected}</NoWarn>");
            }

            [Test]
            public void CheckLangVersion()
            {
                CheckOtherArgument(new []{"-langversion:7.2"}, $"<LangVersion>7.2</LangVersion>");
            }
            
            [Test]
            public void CheckDefaultLangVersion()
            {
                CheckOtherArgument(new string[0], $"<LangVersion>latest</LangVersion>");
            }

            public void CheckOtherArgument(string[] argumentString, params string[] expectedContents)
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, otherArguments: argumentString)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                foreach (string expectedContent in expectedContents)
                {
                    StringAssert.Contains(expectedContent, csprojFileContents,  "Arguments: " + string.Join(";", argumentString));
                }
            }
        }

        public class References : ProjectGenerationTestBase
        {
            [Test]
            public void Containing_PathWithSpaces_IsParsedCorrectly()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, fullPathReferences: new[] { "Folder/Path With Space/Goodbye.dll" })
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Goodbye\">\\W*<HintPath>Folder/Path With Space/Goodbye.dll\\W*</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void Multiple_AreAdded()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, fullPathReferences: new[] { "MyPlugin.dll", "Hello.dll" })
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);

                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Hello\">\\W*<HintPath>Hello.dll</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"MyPlugin\">\\W*<HintPath>MyPlugin.dll</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void AssemblyReference_IsAdded()
            {
                string[] files = { "test.cs" };
                var assemblyReferences = new[]
                {
                    new Assembly("MyPlugin", "/some/path/MyPlugin.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                    new Assembly("Hello", "/some/path/Hello.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                };
                var synchronizer = m_Builder.WithAssemblyData(assemblyReferences: assemblyReferences).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsTrue(csprojFileContents.MatchesRegex($"<Reference Include=\"{assemblyReferences[0].name}\">\\W*<HintPath>{assemblyReferences[0].outputPath}</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex($"<Reference Include=\"{assemblyReferences[1].name}\">\\W*<HintPath>{assemblyReferences[1].outputPath}</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void CompiledAssemblyReference_IsAdded()
            {
                var compiledAssemblyReferences = new[]
                {
                    "/some/path/MyPlugin.dll",
                    "/some/other/path/Hello.dll",
                };
                var synchronizer = m_Builder.WithAssemblyData(compiledAssemblyReferences: compiledAssemblyReferences).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"Hello\">\\W*<HintPath>/some/other/path/Hello.dll</HintPath>\\W*</Reference>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<Reference Include=\"MyPlugin\">\\W*<HintPath>/some/path/MyPlugin.dll</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void ProjectReference_FromLibraryReferences_IsAdded()
            {
                var projectAssembly = new Assembly("ProjectAssembly", "/path/to/project.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblyData(assemblyReferences: new[] { projectAssembly }).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsFalse(csprojFileContents.MatchesRegex($"<Reference Include=\"{projectAssembly.name}\">\\W*<HintPath>{projectAssembly.outputPath}</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void NotInAssembly_WontBeAdded()
            {
                var fileOutsideAssembly = "some.dll";
                var fileArray = new[] { fileOutsideAssembly };
                var synchronizer = m_Builder.WithAssetFiles(fileArray).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.DoesNotContain("some.dll", csprojFileContents);
            }
        }

        public class Defines : ProjectGenerationTestBase
        {
            [Test]
            public void ResponseFiles_CanAddDefines()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, defines: new[] { "DEF1", "DEF2" })
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF1.*</DefineConstants>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF2.*</DefineConstants>"));
            }

            [Test]
            public void Assembly_CanAddDefines()
            {
                var synchronizer = m_Builder.WithAssemblyData(defines: new[] { "DEF1", "DEF2" }).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF1.*</DefineConstants>"));
                Assert.IsTrue(csprojFileContents.MatchesRegex("<DefineConstants>.*;DEF2.*</DefineConstants>"));
            }

            [Test]
            public void ResponseFileDefines_OverrideRootResponseFile()
            {
                string[] files = { "test.cs" };
                var assemblyA = new Assembly("A", "some/root/file.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assemblyB = new Assembly("B", "some/root/child/anotherfile.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder
                    .WithAssemblies(new[] { assemblyA, assemblyB })
                    .WithResponseFileData(assemblyA, "A.rsp", defines: new[] { "RootedDefine" })
                    .WithResponseFileData(assemblyB, "B.rsp", defines: new[] { "CHILD_DEFINE" })
                    .Build();

                synchronizer.Sync();

                var aCsprojContent = m_Builder.ReadProjectFile(assemblyA);
                var bCsprojContent = m_Builder.ReadProjectFile(assemblyB);
                Assert.IsTrue(bCsprojContent.MatchesRegex("<DefineConstants>.*;CHILD_DEFINE.*</DefineConstants>"));
                Assert.IsFalse(bCsprojContent.MatchesRegex("<DefineConstants>.*;RootedDefine.*</DefineConstants>"));
                Assert.IsFalse(aCsprojContent.MatchesRegex("<DefineConstants>.*;CHILD_DEFINE.*</DefineConstants>"));
                Assert.IsTrue(aCsprojContent.MatchesRegex("<DefineConstants>.*;RootedDefine.*</DefineConstants>"));
            }
        }
    }
}

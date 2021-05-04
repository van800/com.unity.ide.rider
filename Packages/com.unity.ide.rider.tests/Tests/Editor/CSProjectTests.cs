using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
{
    namespace CSProjectGeneration
    {
        class Formatting : ProjectGenerationTestBase
        {
            [TestCase(@"x & y.cs", @"x &amp; y.cs")]
            [TestCase(@"x ' y.cs", @"x &apos; y.cs")]
            [TestCase(@"Dimmer&\foo.cs", @"Dimmer&amp;\foo.cs")]
            [TestCase(@"C:\Dimmer/foo.cs", @"C:\Dimmer\foo.cs")]
            public void Escape_SpecialCharsInFileName(string illegalFormattedFileName, string expectedFileName)
            {
                var synchronizer = m_Builder.WithAssemblyData(files: new[] { illegalFormattedFileName }).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.DoesNotContain(illegalFormattedFileName, csprojContent);
                StringAssert.Contains(expectedFileName, csprojContent);
            }

            [Test]
            public void AbsoluteSourceFilePaths_WillBeMadeRelativeToProjectDirectory()
            {
                var absoluteFilePath = Path.Combine(SynchronizerBuilder.projectDirectory, "dimmer.cs");
                var synchronizer = m_Builder.WithAssemblyData(files: new[] { absoluteFilePath }).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                XmlDocument scriptProject = XMLUtilities.FromText(csprojContent);
                XMLUtilities.AssertCompileItemsMatchExactly(scriptProject, new[] { "dimmer.cs" });
            }

            [Test]
            public void ProjectGeneration_UseAssemblyNameProvider_ForOutputPath()
            {
                var expectedAssemblyName = "my.AssemblyName";
                var synchronizer = m_Builder.WithOutputPathForAssemblyPath(m_Builder.Assembly.outputPath, m_Builder.Assembly.name, expectedAssemblyName).Build();

                synchronizer.Sync();

                Assert.That(m_Builder.FileExists(Path.Combine(SynchronizerBuilder.projectDirectory, $"{expectedAssemblyName}.csproj")));
            }

            [Test]
            public void DefaultSyncSettings_WhenSynced_CreatesProjectFileFromDefaultTemplate()
            {
                var projectGuid = "ProjectGuid";
                var synchronizer = m_Builder.WithProjectGuid(projectGuid, m_Builder.Assembly).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                var content = new[]
                {
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">",
                    "  <PropertyGroup>",
                    $"    <LangVersion>{Helper.GetLangVersion()}</LangVersion>",
                    "    <_TargetFrameworkDirectories>non_empty_path_generated_by_unity.rider.package</_TargetFrameworkDirectories>",
                    "    <_FullFrameworkReferenceAssemblyPaths>non_empty_path_generated_by_unity.rider.package</_FullFrameworkReferenceAssemblyPaths>",
                    "    <DisableHandlePackageFileConflicts>true</DisableHandlePackageFileConflicts>",
                    "  </PropertyGroup>",
                    "  <PropertyGroup>",
                    "    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>",
                    "    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>",
                    "    <ProductVersion>10.0.20506</ProductVersion>",
                    "    <SchemaVersion>2.0</SchemaVersion>",
                    "    <RootNamespace></RootNamespace>",
                    $"    <ProjectGuid>{{{projectGuid}}}</ProjectGuid>",
                    "    <ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>",
                    "    <OutputType>Library</OutputType>",
                    "    <AppDesignerFolder>Properties</AppDesignerFolder>",
                    $"    <AssemblyName>{m_Builder.Assembly.name}</AssemblyName>",
                    "    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>",
                    "    <FileAlignment>512</FileAlignment>",
                    "    <BaseDirectory>.</BaseDirectory>",
                    "  </PropertyGroup>",
                    "  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">",
                    "    <DebugSymbols>true</DebugSymbols>",
                    "    <DebugType>full</DebugType>",
                    "    <Optimize>false</Optimize>",
                    $"    <OutputPath>{m_Builder.Assembly.outputPath}</OutputPath>",
                    "    <DefineConstants></DefineConstants>",
                    "    <ErrorReport>prompt</ErrorReport>",
                    "    <WarningLevel>4</WarningLevel>",
                    $"    <NoWarn>{ProjectGeneration.ProjectGeneration.GenerateNoWarn(new List<string>())}</NoWarn>",
                    "    <AllowUnsafeBlocks>False</AllowUnsafeBlocks>",
                    "    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>",
                    "  </PropertyGroup>",
                    "  <PropertyGroup>",
                    "    <NoConfig>true</NoConfig>",
                    "    <NoStdLib>true</NoStdLib>",
                    "    <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>",
                    "    <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>",
                    "    <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>",
                    "  </PropertyGroup>",
                    "  <ItemGroup>",
                    "     <Compile Include=\"test.cs\" />",
                    "  </ItemGroup>",
                    "  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />",
                    "  <!-- To modify your build process, add your task inside one of the targets below and uncomment it.",
                    "       Other similar extension points exist, see Microsoft.Common.targets.",
                    "  <Target Name=\"BeforeBuild\">",
                    "  </Target>",
                    "  <Target Name=\"AfterBuild\">",
                    "  </Target>",
                    "  -->",
                    "</Project>",
                    ""
                };

                StringAssert.AreEqualIgnoringCase(string.Join(Environment.NewLine, content), csprojContent);
            }
        }

        class GUID : ProjectGenerationTestBase
        {
            [Test]
            public void ProjectReference_MatchAssemblyGUID()
            {
                string[] files = { "test.cs" };
                var assemblyB = new Assembly("Test", "Temp/Test.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assemblyA = new Assembly("Test2", "some/path/file.dll", files, new string[0], new[] { assemblyB }, new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblies(new[] { assemblyA, assemblyB }).Build();

                synchronizer.Sync();

                var assemblyACSproject = Path.Combine(SynchronizerBuilder.projectDirectory, $"{assemblyA.name}.csproj");
                var assemblyBCSproject = Path.Combine(SynchronizerBuilder.projectDirectory, $"{assemblyB.name}.csproj");

                Assert.That(m_Builder.FileExists(assemblyACSproject));
                Assert.That(m_Builder.FileExists(assemblyBCSproject));

                XmlDocument scriptProject = XMLUtilities.FromText(m_Builder.ReadFile(assemblyACSproject));
                XmlDocument scriptPluginProject = XMLUtilities.FromText(m_Builder.ReadFile(assemblyBCSproject));

                var a = XMLUtilities.GetInnerText(scriptPluginProject, "/msb:Project/msb:PropertyGroup/msb:ProjectGuid");
                var b = XMLUtilities.GetInnerText(scriptProject, "/msb:Project/msb:ItemGroup/msb:ProjectReference/msb:Project");
                StringAssert.AreEqualIgnoringCase(a, b);
            }
        }

        class Synchronization : ProjectGenerationTestBase
        {
            [Test]
            public void WontSynchronize_WhenNoFilesChanged()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();
                Assert.That(m_Builder.WriteTimes, Is.EqualTo(2), "One write for solution and one write for csproj");

                synchronizer.Sync();
                Assert.That(m_Builder.WriteTimes, Is.EqualTo(2), "No more files should be written");
            }

            [Test]
            public void WhenSynchronized_WillCreateCSProjectForAssembly()
            {
                var synchronizer = m_Builder.Build();

                Assert.That(!m_Builder.FileExists(m_Builder.ProjectFilePath(m_Builder.Assembly)));

                synchronizer.Sync();

                Assert.That(m_Builder.FileExists(m_Builder.ProjectFilePath(m_Builder.Assembly)));
            }

            [Test]
            public void WhenSynchronized_WithTwoAssemblies_TwoProjectFilesAreGenerated()
            {
                var assemblyA = new Assembly("assemblyA", "path/to/a.dll", new[] { "file.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var assemblyB = new Assembly("assemblyB", "path/to/b.dll", new[] { "file.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblies(new[] { assemblyA, assemblyB }).Build();

                synchronizer.Sync();

                Assert.That(m_Builder.FileExists(m_Builder.ProjectFilePath(assemblyA)));
                Assert.That(m_Builder.FileExists(m_Builder.ProjectFilePath(assemblyB)));
            }

            [Test]
            public void NotInInternalizedPackage_WillResync()
            {
                var synchronizer = m_Builder.Build();
                synchronizer.Sync();
                var packageAsset = "packageAsset.cs";
                m_Builder.WithPackageAsset(packageAsset, false);
                Assert.That(synchronizer.SyncIfNeeded(new[] { packageAsset }, new string[0]));
            }
        }

#if UNITY_2020_2_OR_NEWER
        class RootNamespace : ProjectGenerationTestBase
        {
            [Test]
            public void RootNamespaceFromAssembly_AddBlockToCsproj()
            {
                var @namespace = "TestNamespace";

                var synchronizer = m_Builder
                    .WithAssemblyData(rootNamespace: @namespace)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains($"<RootNamespace>{@namespace}</RootNamespace>", csprojFileContents);
            }
        }
#endif

        class SourceFiles : ProjectGenerationTestBase
        {
            [Test] // RIDER-53082 - Generate csproj without cs files, when there are any assets inside
            public void ShaderWithoutCompileScript_WillGetAdded()
            {
                var assembly = new Assembly("name", "Temp/Bin/Debug", new string[0], new string[0], new Assembly[0],
                    new string[0], AssemblyFlags.EditorAssembly);

                var synchronizer = m_Builder
                    .WithOutputPathForAssemblyPath(assembly.outputPath, assembly.name, assembly.name)
                    .WithAssetFiles(new[] {"file.hlsl"})
                    .AssignFilesToAssembly(new[] {"file.hlsl"}, assembly)
                    .Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(assembly);
                StringAssert.Contains("file.hlsl", csprojContent);
            }
            
            [Test] // RIDER-60508 Don't have support for Shaderlab
            public void ShaderWithoutCompileScript_WithReference_WillGetAdded()
            {
                var assembly = new Assembly("name", "Temp/Bin/Debug", new string[0], new string[0], new Assembly[0],
                    new string[0], AssemblyFlags.EditorAssembly);
                var riderAssembly = new Assembly("Unity.Rider.Editor", "Temp/Bin/Debug", new string[0], new string[0],
                    new Assembly[0],
                    new[] {"UnityEditor.dll"}, AssemblyFlags.EditorAssembly);

                var synchronizer = m_Builder
                    .WithAssemblies(new []{riderAssembly})
                    .WithOutputPathForAssemblyPath(assembly.outputPath, assembly.name, assembly.name)
                    .WithAssetFiles(new[] {"file.hlsl"})
                    .AssignFilesToAssembly(new[] {"file.hlsl"}, assembly)
                    .Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(assembly);
                StringAssert.Contains("file.hlsl", csprojContent);
                StringAssert.Contains("UnityEditor.dll", csprojContent);
            }
            
            [Test]
            public void NotContributedAnAssembly_WillNotGetAdded()
            {
                var synchronizer = m_Builder.WithAssetFiles(new[] { "Assembly.hlsl" }).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.DoesNotContain("Assembly.hlsl", csprojContent);
            }

            [Test]
            public void MultipleSourceFiles_WillAllBeAdded()
            {
                var files = new[] { "fileA.cs", "fileB.cs", "fileC.cs" };
                var synchronizer = m_Builder
                    .WithAssemblyData(files: files)
                    .Build();

                synchronizer.Sync();

                var csprojectContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                var xmlDocument = XMLUtilities.FromText(csprojectContent);
                XMLUtilities.AssertCompileItemsMatchExactly(xmlDocument, files);
            }

            [Test]
            public void FullPathAsset_WillBeConvertedToRelativeFromProjectDirectory()
            {
                var assetPath = "Assets/Asset.cs";
                var synchronizer = m_Builder
                    .WithAssemblyData(files: new[] { Path.Combine(SynchronizerBuilder.projectDirectory, assetPath) })
                    .Build();

                synchronizer.Sync();

                var csprojectContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                var xmlDocument = XMLUtilities.FromText(csprojectContent);
                XMLUtilities.AssertCompileItemsMatchExactly(xmlDocument, new[] { assetPath });
            }

            [Test]
            public void InRelativePackages_GetsPathResolvedCorrectly()
            {
                var assetPath = "/FullPath/ExamplePackage/Packages/Asset.cs";
                var assembly = new Assembly("ExamplePackage", "/FullPath/Example/ExamplePackage/ExamplePackage.dll", new[] { assetPath }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder
                    .WithAssemblies(new[] { assembly })
                    .WithPackageInfo(assetPath)
                    .Build();

                synchronizer.Sync();

                StringAssert.Contains(assetPath.Replace('/', '\\'), m_Builder.ReadProjectFile(assembly));
            }

            [Test]
            public void InInternalizedPackage_WillBeAddedToCompileInclude()
            {
                var synchronizer = m_Builder.WithPackageAsset(m_Builder.Assembly.sourceFiles[0], true).Build();
                synchronizer.Sync();
                StringAssert.Contains(m_Builder.Assembly.sourceFiles[0], m_Builder.ReadProjectFile(m_Builder.Assembly));
            }

            [Test]
            public void NotInInternalizedPackage_WillBeAddedToCompileInclude()
            {
                var synchronizer = m_Builder.WithPackageAsset(m_Builder.Assembly.sourceFiles[0], false).Build();
                synchronizer.Sync();
                StringAssert.Contains(m_Builder.Assembly.sourceFiles[0], m_Builder.ReadProjectFile(m_Builder.Assembly));
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
                    "UnityShader.uss",
                    "ComputerGraphic.cginc",
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
            public void UnsupportedExtensions_WillNotBeAdded()
            {
                var unsupported = new[] { "file.unsupported" };
                var synchronizer = m_Builder
                    .WithAssetFiles(unsupported)
                    .AssignFilesToAssembly(unsupported, m_Builder.Assembly)
                    .Build();

                synchronizer.Sync();

                var csprojectContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                var xmlDocument = XMLUtilities.FromText(csprojectContent);
                XMLUtilities.AssertCompileItemsMatchExactly(xmlDocument, m_Builder.Assembly.sourceFiles);
                XMLUtilities.AssertNonCompileItemsMatchExactly(xmlDocument, new string[0]);
            }

            [Test]
            public void UnsupportedExtension_IsOverWrittenBy_ProjectSupportedExtensions()
            {
                var unsupported = new[] { "file.unsupported" };
                var synchronizer = m_Builder
                    .WithAssetFiles(unsupported)
                    .AssignFilesToAssembly(unsupported, m_Builder.Assembly)
                    .WithProjectSupportedExtensions(new[] { "unsupported" })
                    .Build();
                synchronizer.Sync();
                var xmlDocument = XMLUtilities.FromText(m_Builder.ReadProjectFile(m_Builder.Assembly));
                XMLUtilities.AssertNonCompileItemsMatchExactly(xmlDocument, unsupported);
            }

            [TestCase(@"path\com.unity.cs")]
            [TestCase(@"..\path\file.cs")]
            public void IsValidFileName(string filePath)
            {
                var synchronizer = m_Builder
                    .WithAssemblyData(files: new[] { filePath })
                    .Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(filePath, csprojContent);
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

                Assert.That(synchronizer.SyncIfNeeded(newFileArray, new string[0]), "Should sync when file in assembly changes");

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
                var synchronizer = m_Builder.WithAssemblyData(filesBefore).Build();

                synchronizer.Sync();

                var csprojContentBefore = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(filesBefore[0], csprojContentBefore);
                StringAssert.Contains(filesBefore[1], csprojContentBefore);

                var filesAfter = filesBefore.Skip(1).ToArray();
                m_Builder.WithAssemblyData(filesAfter);

                Assert.That(synchronizer.SyncIfNeeded(filesAfter, new string[0]), "Should sync when file in assembly changes");

                var csprojContentAfter = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains(filesAfter[0], csprojContentAfter);
                StringAssert.DoesNotContain(filesBefore[0], csprojContentAfter);
            }

            [Test] // https://github.cds.internal.unity3d.com/unity/com.unity.ide.rider/issues/121
            public void EmptyPathWouldNotBrake()
            {
                var filesBefore = new[] { "Script.cs", string.Empty }; // empty path should not cause exception
                var synchronizer = m_Builder.WithAssemblyData(filesBefore).Build();
                Assert.False(synchronizer.SyncIfNeeded(new string[]{}, filesBefore), "Guarantees that all code paths were tried.");
            }

            [Test, TestCaseSource(nameof(s_BuiltinSupportedExtensionsForSourceFiles))]
            public void BuiltinSupportedExtensions_InsideAssemblySourceFiles_WillBeAddedToCompileItems(string fileExtension)
            {
                var compileItem = new[] { "file.cs", $"anotherFile.{fileExtension}" };
                var synchronizer = m_Builder.WithAssemblyData(files: compileItem).Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                XmlDocument scriptProject = XMLUtilities.FromText(csprojContent);
                XMLUtilities.AssertCompileItemsMatchExactly(scriptProject, compileItem);
            }

            static string[] s_BuiltinSupportedExtensionsForSourceFiles =
            {
                "asmdef", "cs", "uxml", "uss", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace"
            };

            [Test, TestCaseSource(nameof(s_BuiltinSupportedExtensionsForAssets))]
            public void BuiltinSupportedExtensions_InsideAssetFolder_WillBeAddedToNonCompileItems(string fileExtension)
            {
                var nonCompileItem = new[] { $"anotherFile.{fileExtension}" };
                var synchronizer = m_Builder
                    .WithAssetFiles(files: nonCompileItem)
                    .AssignFilesToAssembly(nonCompileItem, m_Builder.Assembly)
                    .Build();

                synchronizer.Sync();

                var csprojContent = m_Builder.ReadProjectFile(m_Builder.Assembly);
                XmlDocument scriptProject = XMLUtilities.FromText(csprojContent);
                XMLUtilities.AssertCompileItemsMatchExactly(scriptProject, m_Builder.Assembly.sourceFiles);
                XMLUtilities.AssertNonCompileItemsMatchExactly(scriptProject, nonCompileItem);
            }

            static string[] s_BuiltinSupportedExtensionsForAssets =
            {
                "uxml", "uss", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace"
            };
        }

        class CompilerOptions : ProjectGenerationTestBase
        {
            [Test]
            public void AllowUnsafeFromResponseFile_AddBlockToCsproj()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, _unsafe: true)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains("<AllowUnsafeBlocks>True</AllowUnsafeBlocks>", csprojFileContents);
            }

            [TestCase(new object[] { "C:/Analyzer.dll" })]
            [TestCase(new object[] { "C:/Analyzer.dll", "C:/Analyzer2.dll" })]
            [TestCase(new object[] { "../Analyzer.dll" })]
            [TestCase(new object[] { "../Analyzer.dll", "C:/Analyzer2.dll" })]
            public void AddAnalyzers(params string[] paths)
            {
                var combined = string.Join(";", paths);
                const string additionalFileTemplate = @"    <Analyzer Include=""{0}"" />";
                var expectedOutput = paths.Select(x => string.Format(additionalFileTemplate, x)).ToArray();

                CheckOtherArgument(new[] { $"-a:{combined}" }, expectedOutput);
                CheckOtherArgument(new[] { $"-analyzer:{combined}" }, expectedOutput);
                CheckOtherArgument(new[] { $"/a:{combined}" }, expectedOutput);
                CheckOtherArgument(new[] { $"/analyzer:{combined}" }, expectedOutput);
            }

            [TestCase(new object[] { "C:/Analyzer.dll" })]
            [TestCase(new object[] { "C:/Analyzer.dll", "C:/Analyzer2.dll" })]
            [TestCase(new object[] { "../Analyzer.dll" })]
            [TestCase(new object[] { "../Analyzer.dll", "C:/Analyzer2.dll" })]
            public void AddAdditionalFile(params string[] paths)
            {
                var combined = string.Join(";", paths);
                const string additionalFileTemplate = @"    <AdditionalFiles Include=""{0}"" />";
                var expectedOutput = paths.Select(x => string.Format(additionalFileTemplate, x)).ToArray();

                CheckOtherArgument(new[] { $"-additionalfile:{combined}" }, expectedOutput);
                CheckOtherArgument(new[] { $"/additionalfile:{combined}" }, expectedOutput);
            }

            [TestCase("0169", "0123")]
            [TestCase("0169")]
            [TestCase("0169;0123", "0234")]
            public void SetWarnAsError(params string[] errorCodes)
            {
                var combined = string.Join(";", errorCodes);

                var expectedOutput = $"<WarningsAsErrors>{string.Join(";", errorCodes)}</WarningsAsErrors>";

                CheckOtherArgument(new[] { $"-warnaserror:{combined}" }, expectedOutput);
                CheckOtherArgument(new[] { $"/warnaserror:{combined}" }, expectedOutput);
            }

            [TestCase(true, "0169", "0123")]
            [TestCase(false, "0169", "0123")]
            public void SetWarnAsError(bool state, params string[] errorCodes)
            {
                string value = state ? "+" : "-";
                CheckOtherArgument(new[] { $"-warnaserror{value}" }, $"<TreatWarningsAsErrors>{state}</TreatWarningsAsErrors>");
                CheckOtherArgument(new[] { $"/warnaserror{value}" }, $"<TreatWarningsAsErrors>{state}</TreatWarningsAsErrors>");
            }
            
            [Test]
            public void SetWarnAsError2()
            {
                CheckOtherArgument(new[] { $"-warnaserror" }, "<TreatWarningsAsErrors>True</TreatWarningsAsErrors>");
                CheckOtherArgument(new[] { $"-warnaserror+" }, "<TreatWarningsAsErrors>True</TreatWarningsAsErrors>");
                CheckOtherArgument(new[] { $"-warnaserror-" }, "<TreatWarningsAsErrors>False</TreatWarningsAsErrors>");
            }
            
            [Test]
            public void SetWarnAsErrorCombined1()
            {
                var expectedOutput = "<TreatWarningsAsErrors>True</TreatWarningsAsErrors>";
                var expectedOutput2 = "<WarningsNotAsErrors>0169;0123</WarningsNotAsErrors>";

                CheckOtherArgument(new[] { "-warnaserror+", "-warnaserror-:0169;0123" }, expectedOutput, expectedOutput2);
            }
            
            [Test]
            public void SetWarnAsErrorCombined2()
            {
                var expectedOutput = "<TreatWarningsAsErrors>False</TreatWarningsAsErrors>";
                var expectedOutput2 = "<WarningsAsErrors>0169;0123</WarningsAsErrors>";

                CheckOtherArgument(new[] { "-warnaserror-", "-warnaserror+:0169;0123" }, expectedOutput, expectedOutput2);
            }

            [TestCase(0)]
            [TestCase(4)]
            public void SetWarningLevel(int level)
            {
                string warningLevelString = $"<WarningLevel>{level}</WarningLevel>";
                CheckOtherArgument(new[] { $"-w:{level}" }, warningLevelString);
                CheckOtherArgument(new[] { $"-warn:{level}" }, warningLevelString);
                CheckOtherArgument(new[] { $"/w:{level}" }, warningLevelString);
                CheckOtherArgument(new[] { $"/warn:{level}" }, warningLevelString);
            }

            [TestCase("C:/rules.ruleset")]
            [TestCase("../rules.ruleset")]
            [TestCase(new object[] { "../rules.ruleset", "C:/rules.ruleset" })]
            public void SetRuleset(params string[] paths)
            {
                string rulesetTemplate = "<CodeAnalysisRuleSet>{0}</CodeAnalysisRuleSet>";
                CheckOtherArgument(paths.Select(x => $"-ruleset:{x}").ToArray(), paths.Select(x => string.Format(rulesetTemplate, x)).ToArray());
                CheckOtherArgument(paths.Select(x => $"/ruleset:{x}").ToArray(), paths.Select(x => string.Format(rulesetTemplate, x)).ToArray());
            }

            [TestCase("C:/docs.xml")]
            [TestCase("../docs.xml")]
            [TestCase(new object[] { "../docs.xml", "C:/docs.xml" })]
            public void SetDocumentationFile(params string[] paths)
            {
                string docTemplate = "<DocumentationFile>{0}</DocumentationFile>";
                CheckOtherArgument(paths.Select(x => $"-doc:{x}").ToArray(), paths.Select(x => string.Format(docTemplate, x)).ToArray());
                CheckOtherArgument(paths.Select(x => $"/doc:{x}").ToArray(), paths.Select(x => string.Format(docTemplate, x)).ToArray());
            }

            [Test]
            public void CheckDefaultWarningLevel()
            {
                CheckOtherArgument(new string[0], "<WarningLevel>4</WarningLevel>");
            }

            [TestCase(new[] { "-nowarn:10" }, "10")]
            [TestCase(new[] { "-nowarn:10,11" }, "10,11")]
            [TestCase(new[] { "-nowarn:10,11", "-nowarn:12" }, "10,11,12")]
            public void CheckNoWarn(string[] args, string expected)
            {
                var commonPart = ProjectGeneration.ProjectGeneration.GenerateNoWarn(new List<string>());
                if (!string.IsNullOrEmpty(commonPart))
                    expected = $"{expected},{commonPart}";
                CheckOtherArgument(args, $"<NoWarn>{expected}</NoWarn>");
            }

            [Test]
            public void CheckLangVersion()
            {
                CheckOtherArgument(new[] { "-langversion:7.2" }, "<LangVersion>7.2</LangVersion>");
            }

            [Test]
            public void CheckDefaultLangVersion()
            {
                CheckOtherArgument(new string[0], $"<LangVersion>{Helper.GetLangVersion()}</LangVersion>");
            }
            
            [Test]
            public void CheckNullable()
            {
                CheckOtherArgument(new[] { "-nullable" }, "<Nullable>enable</Nullable>");
                CheckOtherArgument(new[] { "-nullable:enable" }, "<Nullable>enable</Nullable>");
                CheckOtherArgument(new[] { "-nullable+" }, "<Nullable>enable</Nullable>");
                CheckOtherArgument(new[] { "-nullable:disable" }, "<Nullable>disable</Nullable>");
                CheckOtherArgument(new[] { "-nullable-" }, "<Nullable>disable</Nullable>");
                CheckOtherArgument(new[] { "-nullable:warnings" }, "<Nullable>warnings</Nullable>");
                CheckOtherArgument(new[] { "-nullable:annotations" }, "<Nullable>annotations</Nullable>");
            }

            void CheckOtherArgument(string[] argumentString, params string[] expectedContents)
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, otherArguments: argumentString)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                foreach (string expectedContent in expectedContents)
                {
                    StringAssert.Contains(
                        expectedContent,
                        csprojFileContents,
                        $"Arguments: {string.Join(";", argumentString)} {Environment.NewLine}"
                        + Environment.NewLine
                        + $"Expected: {expectedContent.Replace("\r", "\\r").Replace("\n", "\\n")}"
                        + Environment.NewLine
                        + $"Actual: {csprojFileContents.Replace("\r", "\\r").Replace("\n", "\\n")}");
                }
            }

#if UNITY_2020_2_OR_NEWER
            [TestCase("8.0")]
            [TestCase("13.14")]
            [TestCase("42")]
            public void LanguageVersionFromAssembly_WillBeSet(string languageVersion)
            {
                var options = new ScriptCompilerOptions();
                typeof(ScriptCompilerOptions).GetProperty("LanguageVersion")?.SetValue(options, languageVersion, null);
                var synchronizer = m_Builder
                    .WithAssemblyData(options: options)
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains($"<LangVersion>{languageVersion}</LangVersion>", csprojFileContents);
            }
#endif
            
            [Test]
            public void AllowUnsafeFromAssemblySettings_AddBlockToCsproj()
            {
                var synchronizer = m_Builder
                    .WithAssemblyData(options: new ScriptCompilerOptions{AllowUnsafeCode = true})
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                StringAssert.Contains("<AllowUnsafeBlocks>True</AllowUnsafeBlocks>", csprojFileContents);
            }
        }

        class References : ProjectGenerationTestBase
        {
            [Test]
            public void RoslynAnalyzerDlls_WillBeIncluded()
            {
                var roslynAnalyzerDllPath = "Assets\\RoslynAnalyzer.dll";
                var synchronizer = m_Builder.WithRoslynAnalyzers(new[] { roslynAnalyzerDllPath }).Build();

                synchronizer.Sync();

                string projectFile = m_Builder.ReadProjectFile(m_Builder.Assembly);
                XmlDocument projectFileXml = XMLUtilities.FromText(projectFile);
                XMLUtilities.AssertAnalyzerItemsMatchExactly(projectFileXml, new[] { roslynAnalyzerDllPath });
            }

#if UNITY_2020_2_OR_NEWER
            [Test]
            public void RoslynAnalyzerRulesetFiles_WillBeIncluded()
            {
                var roslynAnalyzerRuleSetPath = "Assets/RoslynRuleSet.ruleset";

                m_Builder.WithAssemblyData(files: new[] {"file.cs"}, roslynAnalyzerRulesetPath: roslynAnalyzerRuleSetPath).Build().Sync();
                var csProjectFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                XmlDocument csProjectXmlFile = XMLUtilities.FromText(csProjectFileContents);
                XMLUtilities.AssertAnalyzerRuleSetsMatchExactly(csProjectXmlFile, roslynAnalyzerRuleSetPath);
            }
#endif
            
            [Test]
            public void Containing_PathWithSpaces_IsParsedCorrectly()
            {
                const string responseFile = "csc.rsp";
                var synchronizer = m_Builder
                    .WithResponseFileData(m_Builder.Assembly, responseFile, fullPathReferences: new[] { "Folder/Path With Space/Goodbye.dll" })
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.That(csprojFileContents, Does.Match($"<Reference Include=\"Goodbye\">\\W*<HintPath>{SynchronizerBuilder.projectDirectory}/Folder/Path With Space/Goodbye.dll\\W*</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void Containing_PathWithDotCS_IsParsedCorrectly()
            {
                var assembly = new Assembly("name", "/path/with.cs/assembly.dll", new[] { "file.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder
                    .WithAssemblyData(assemblyReferences: new[] { assembly })
                    .Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.That(csprojFileContents, Does.Match($@"<ProjectReference Include=""{assembly.name}\.csproj"">[\S\s]*?</ProjectReference>"));
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

                Assert.That(csprojFileContents, Does.Match($@"<Reference Include=""Hello"">\W*<HintPath>{SynchronizerBuilder.projectDirectory}/Hello\.dll</HintPath>\W*</Reference>"));
                Assert.That(csprojFileContents, Does.Match($@"<Reference Include=""MyPlugin"">\W*<HintPath>{SynchronizerBuilder.projectDirectory}/MyPlugin\.dll</HintPath>\W*</Reference>"));
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
                Assert.That(csprojFileContents, Does.Match($@"<ProjectReference Include=""{assemblyReferences[0].name}\.csproj"">[\S\s]*?</ProjectReference>"));
                Assert.That(csprojFileContents, Does.Match($@"<ProjectReference Include=""{assemblyReferences[1].name}\.csproj"">[\S\s]*?</ProjectReference>"));
            }

            [Test]
            public void AssemblyReferenceFromInternalizedPackage_IsAddedAsReference()
            {
                string[] files = { "test.cs" };
                var assemblyReferences = new[]
                {
                    new Assembly("MyPlugin", "/some/path/MyPlugin.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                    new Assembly("Hello", "/some/path/Hello.dll", files, new string[0], new Assembly[0], new string[0], AssemblyFlags.None),
                };
                var synchronizer = m_Builder.WithPackageAsset(files[0], true).WithAssemblyData(assemblyReferences: assemblyReferences).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.That(csprojFileContents, Does.Not.Match($@"<ProjectReference Include=""{assemblyReferences[0].name}\.csproj"">[\S\s]*?</ProjectReference>"));
                Assert.That(csprojFileContents, Does.Not.Match($@"<ProjectReference Include=""{assemblyReferences[1].name}\.csproj"">[\S\s]*?</ProjectReference>"));
                Assert.That(csprojFileContents, Does.Match($"<Reference Include=\"{assemblyReferences[0].name}\">\\W*<HintPath>{assemblyReferences[0].outputPath}</HintPath>\\W*</Reference>"));
                Assert.That(csprojFileContents, Does.Match($"<Reference Include=\"{assemblyReferences[1].name}\">\\W*<HintPath>{assemblyReferences[1].outputPath}</HintPath>\\W*</Reference>"));
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
                Assert.That(csprojFileContents, Does.Match("<Reference Include=\"Hello\">\\W*<HintPath>/some/other/path/Hello.dll</HintPath>\\W*</Reference>"));
                Assert.That(csprojFileContents, Does.Match("<Reference Include=\"MyPlugin\">\\W*<HintPath>/some/path/MyPlugin.dll</HintPath>\\W*</Reference>"));
            }

            [Test]
            public void ProjectReference_FromLibraryReferences_IsAdded()
            {
                var projectAssembly = new Assembly("ProjectAssembly", "/path/to/project.dll", new[] { "test.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None);
                var synchronizer = m_Builder.WithAssemblyData(assemblyReferences: new[] { projectAssembly }).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.That(csprojFileContents, Does.Not.Match($"<Reference Include=\"{projectAssembly.name}\">\\W*<HintPath>{projectAssembly.outputPath}</HintPath>\\W*</Reference>"));
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

        class Defines : ProjectGenerationTestBase
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
                Assert.That(csprojFileContents, Does.Match("<DefineConstants>DEF1;DEF2</DefineConstants>"));
            }

            [Test]
            public void Assembly_CanAddDefines()
            {
                var synchronizer = m_Builder.WithAssemblyData(defines: new[] { "DEF1", "DEF2" }).Build();

                synchronizer.Sync();

                var csprojFileContents = m_Builder.ReadProjectFile(m_Builder.Assembly);
                Assert.That(csprojFileContents, Does.Match("<DefineConstants>DEF1;DEF2</DefineConstants>"));
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
                Assert.That(bCsprojContent, Does.Match("<DefineConstants>CHILD_DEFINE</DefineConstants>"));
                Assert.That(bCsprojContent, Does.Not.Match("<DefineConstants>RootedDefine</DefineConstants>"));
                Assert.That(aCsprojContent, Does.Not.Match("<DefineConstants>CHILD_DEFINE</DefineConstants>"));
                Assert.That(aCsprojContent, Does.Match("<DefineConstants>RootedDefine</DefineConstants>"));
            }
        }
    }
}
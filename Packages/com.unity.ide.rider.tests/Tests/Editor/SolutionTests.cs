using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
{
    namespace SolutionGeneration
    {
        class Synchronization : ProjectGenerationTestBase
        {
            [Test]
            public void EmptyProject_WhenSynced_ShouldNotGenerateSolutionFile()
            {
                var synchronizer = m_Builder.WithAssemblies(new Assembly[0]).Build();

                synchronizer.Sync();

                Assert.False(m_Builder.ReadFile(synchronizer.SolutionFile()).Contains("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\")"), "Should not create project entry with no assemblies.");
            }

            [Test]
            public void NoSolution_WhenSynced_CreatesSolutionFile()
            {
                var synchronizer = m_Builder.Build();

                Assert.False(m_Builder.FileExists(synchronizer.SolutionFile()), "Should not create solution file before we call sync.");

                synchronizer.Sync();

                Assert.True(m_Builder.FileExists(synchronizer.SolutionFile()), "Should create solution file.");
            }

            [Test]
            public void WhenSynced_ThenDeleted_SolutionFileDoesNotExist()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();
                m_Builder.DeleteFile(synchronizer.SolutionFile());

                Assert.False(m_Builder.FileExists(synchronizer.SolutionFile()), "Synchronizer should sync state with file system, after file has been deleted.");
            }

            [Test]
            public void ContentWithoutChanges_WhenSynced_DoesNotReSync()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();
                Assert.AreEqual(2, m_Builder.WriteTimes); // Once for csproj and once for solution

                synchronizer.Sync();
                Assert.AreEqual(2, m_Builder.WriteTimes, "When content doesn't change we shouldn't re-sync");
            }

            [Test]
            public void AssemblyChanged_AfterSync_PerformsReSync()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();
                Assert.AreEqual(2, m_Builder.WriteTimes); // Once for csproj and once for solution

                m_Builder.WithAssemblies(new[] { new Assembly("Another", "path/to/Assembly.dll", new[] { "file.cs" }, new string[0], new Assembly[0], new string[0], AssemblyFlags.None) });

                synchronizer.Sync();
                Assert.AreEqual(4, m_Builder.WriteTimes, "Should re-sync the solution file and the ");
            }

            [Test]
            public void EmptySolutionFile_WhenSynced_OverwritesTheFile()
            {
                var synchronizer = m_Builder.Build();

                // Pre-seed solution file with empty property section
                var solutionText = "Microsoft Visual Studio Solution File, Format Version 10.00\n# Visual Studio 2008\nGlobal\nEndGlobal";
                m_Builder.WithSolutionText(solutionText);

                synchronizer.Sync();

                Assert.AreNotEqual(solutionText, m_Builder.ReadFile(synchronizer.SolutionFile()), "Should rewrite solution text");
            }

            [TestCase("asmref")]
            [TestCase("asmdef")]
            public void AfterSync_WillResync_WhenReimportWithSpecialFileExtensions(string reimportedFile)
            {
                var synchronizer = m_Builder.Build();

                Assert.True(synchronizer.SyncIfNeeded(new string[0], new[] { $"reimport.{reimportedFile}" }), "Before sync has been called, we should allow SyncIfNeeded");

                synchronizer.Sync();

                Assert.IsTrue(synchronizer.SyncIfNeeded(new string[0], new[] { $"reimport.{reimportedFile}" }));
            }

            [Test]
            public void AfterSync_WontResync_WhenReimportWithoutSpecialFileExtensions()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();

                Assert.IsFalse(synchronizer.SyncIfNeeded(new string[0], new[] { "ShouldNotSync.txt" }));
            }

            [Test]
            public void AfterSync_WontReimport_WithoutSpecificAffectedFileExtension()
            {
                var synchronizer = m_Builder.Build();

                synchronizer.Sync();

                Assert.IsFalse(synchronizer.SyncIfNeeded(new[] { "reimport.random" }, new string[0]));
            }

            [Test]
            public void AfterSync_WillResync_IfExtensionIsInProjectSupportedExtension()
            {
                var synchronizer = m_Builder.Build();
                synchronizer.Sync();
                m_Builder.WithProjectSupportedExtensions(new[] { "random" });
                Assert.IsTrue(synchronizer.SyncIfNeeded(new[] { "reimport.random" }, new string[0]));
            }

            [Test, TestCaseSource(nameof(s_ExtensionsRequireReSync))]
            public void AfterSync_WillResync_WhenAffectedFileTypes(string fileExtension)
            {
                var synchronizer = m_Builder.Build();

                Assert.True(synchronizer.SyncIfNeeded(new[] { $"reimport.{fileExtension}" }, new string[0]), "Before sync has been called, we should allow SyncIfNeeded");

                synchronizer.Sync();

                Assert.IsTrue(synchronizer.SyncIfNeeded(new[] { $"reimport.{fileExtension}" }, new string[0]));
            }

            static string[] s_ExtensionsRequireReSync =
            {
                "dll", "asmdef", "cs", "uxml", "uss", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace"
            };
        }

        class Format : ProjectGenerationTestBase
        {
            [Test]
            public void DefaultSyncSettings_WhenSynced_CreatesSolutionFileFromDefaultTemplate()
            {
                var solutionGUID = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
                var projectGUID = "ProjectGUID";
                var synchronizer = m_Builder
                    .WithProjectGuid(projectGUID, m_Builder.Assembly)
                    .Build();

                // solutionguid, solutionname, projectguid
                var solutionExpected = string.Join(Environment.NewLine, new[]
                {
                    @"",
                    @"Microsoft Visual Studio Solution File, Format Version 11.00",
                    @"# Visual Studio 2010",
                    @"Project(""{{{0}}}"") = ""{2}"", ""{2}.csproj"", ""{{{1}}}""",
                    @"EndProject",
                    @"Global",
                    @"    GlobalSection(SolutionConfigurationPlatforms) = preSolution",
                    @"        Debug|Any CPU = Debug|Any CPU",
                    @"    EndGlobalSection",
                    @"    GlobalSection(ProjectConfigurationPlatforms) = postSolution",
                    @"        {{{1}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                    @"        {{{1}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
                    @"    EndGlobalSection",
                    @"    GlobalSection(SolutionProperties) = preSolution",
                    @"        HideSolutionNode = FALSE",
                    @"    EndGlobalSection",
                    @"EndGlobal",
                    @""
                }).Replace("    ", "\t");

                var solutionTemplate = string.Format(
                    solutionExpected,
                    solutionGUID,
                    projectGUID,
                    m_Builder.Assembly.name);

                synchronizer.Sync();

                Assert.AreEqual(solutionTemplate, m_Builder.ReadFile(synchronizer.SolutionFile()));
            }
        }
    }
}

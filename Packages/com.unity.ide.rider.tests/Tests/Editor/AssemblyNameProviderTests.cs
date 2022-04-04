using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Packages.Rider.Editor.ProjectGeneration;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
{
    public class AssemblyNameProviderTests
    {
        AssemblyNameProvider m_AssemblyNameProvider;
        ProjectGenerationFlag m_Flag;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_AssemblyNameProvider = new AssemblyNameProvider();
            m_Flag = m_AssemblyNameProvider.ProjectGenerationFlag;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.None);
            m_AssemblyNameProvider.ToggleProjectGeneration(m_Flag);
        }

        [SetUp]
        public void SetUp()
        {
            m_AssemblyNameProvider.ResetProjectGenerationFlag();
        }

        [Test]
        public void ProjectNameForDefines1()
        {
            Assert.AreEqual("name", m_AssemblyNameProvider.GetProjectName("name", new []{"UNITY_EDITOR"}));
        }

        [Test]
        public void AllEditorAssemblies_AreCollected()
        {
            if (m_AssemblyNameProvider.ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.PlayerAssemblies))
            {
                m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.None);
            }
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            var names = collectedAssemblies.Select(assembly => assembly.name);

            foreach (var assembly in collectedAssemblies)
            {
                Assert.That(assembly.outputPath, Is.EqualTo($@"Temp\Bin\Debug\{assembly.name}\"), $"{assembly.name}: had wrong output path: {assembly.outputPath}");
            }
            foreach (Assembly editorAssembly in editorAssemblies)
            {
                CollectionAssert.Contains(names, editorAssembly.name);
            }
        }

#if UNITY_2020_2_OR_NEWER
        [Test]
        public void EditorAssemblies_WillIncludeRootNamespace()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            var editorTestAssembly = editorAssemblies.Single(a => a.name == "Unity.Rider.EditorTests");
            Assert.AreEqual("Packages.Rider.Editor.Tests", editorTestAssembly.rootNamespace);

            var collectedTestAssembly = collectedAssemblies.Single(a => a.name == editorTestAssembly.name);
            Assert.AreEqual(editorTestAssembly.rootNamespace, collectedTestAssembly.rootNamespace);
        }
#endif

        [Test]
        public void AllEditorAssemblies_HaveAReferenceToUnityEditorAndUnityEngine()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            foreach (Assembly editorAssembly in editorAssemblies)
            {
                Assert.IsTrue(editorAssembly.allReferences.Any(reference => reference.EndsWith("UnityEngine.dll")), $"UnityEngine.dll not found among {editorAssembly.allReferences.Length} references");
                Assert.IsTrue(editorAssembly.allReferences.Any(reference => reference.EndsWith("UnityEditor.dll")), $"UnityEditor.dll not found among {editorAssembly.allReferences.Length} references");
            }
        }

        [Test]
        public void PlayerAssemblies_AreNotCollected_BeforeToggling()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsFalse(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name && assembly.outputPath == @"Temp\Bin\Debug\Player\"), $"{playerAssembly.name}: was found in collection.");
            }
        }

        [Test]
        public void AllPlayerAssemblies_AreCollected_AfterToggling()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.PlayerAssemblies);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsTrue(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name && assembly.outputPath == $@"Temp\Bin\Debug\{playerAssembly.name}\Player\"), $"{playerAssembly.name}: was not found in collection.");
            }
        }
    }
}

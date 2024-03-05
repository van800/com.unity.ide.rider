using System.IO;
using NUnit.Framework;
using Unity.CodeEditor;
using UnityEditor;

namespace Packages.Rider.Editor.Tests
{
    public class ProjectGenerationTestBase
    {
        string m_EditorPath;
        protected SynchronizerBuilder m_Builder;

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

        [SetUp]
        public void SetUp()
        {
            m_Builder = new SynchronizerBuilder();
        }
            
        protected static string MakeAbsolutePathTestImplementation(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(SynchronizerBuilder.ProjectDirectory, path);
        }
        
        protected static string MakeAbsolutePath(string path)
        {
            return Path.IsPathRooted(path) ? path : FileUtil.GetPhysicalPath(path);
        }
    }
}

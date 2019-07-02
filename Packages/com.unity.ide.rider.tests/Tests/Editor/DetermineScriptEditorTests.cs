using System.IO;
using Moq;
using NUnit.Framework;
using Packages.Rider.Editor;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Packages.Rider.Tests.Editor
{
    [TestFixture]
    public class DetermineScriptEditorTests
    {
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void OSXPathDiscovery()
        {
            var path = Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).FullName,
                @"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\Rider.app");
            Discover(path);
        }
        
        
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Test]
        public void WindowsPathDiscovery()
        {
            var path = Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).FullName,
                @"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\191.7141.355\bin\rider64.exe");
            Discover(path);
        }

        [UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void LinuxPathDiscovery()
        {
            var path = Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).FullName,
                @"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\191.7141.355\bin\rider.sh");
            Discover(path);
        }

        static void Discover(string path)
        {
            var discovery = new Mock<IDiscovery>();
            var generator = new Mock<IGenerator>();

            discovery.Setup(x => x.PathCallback()).Returns(new [] {
                new CodeEditor.Installation
                {
                    Path = path,
                    Name = path.Contains("Insiders") ? "Visual Studio Code Insiders" : "Visual Studio Code"
                }
            });

            var editor = new RiderScriptEditor(discovery.Object, generator.Object);

            editor.TryGetInstallationForPath(path, out var installation);

            Assert.AreEqual(path, installation.Path);
        }
    }
}

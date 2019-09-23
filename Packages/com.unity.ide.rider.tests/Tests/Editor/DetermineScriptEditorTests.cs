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
            var path = new FileInfo(@"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\Rider.app").FullName;
            Discover(path);
        }
        
        
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [Test]
        public void WindowsPathDiscovery()
        {
            var path = new FileInfo(@"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\191.7141.355\bin\rider64.exe").FullName;
            Discover(path);
        }

        [UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void LinuxPathDiscovery()
        {
            var path = new FileInfo(@"Packages\com.unity.ide.rider.tests\Tests\Data\DetermineScriptEditorTests\191.7141.355\bin\rider.sh").FullName;
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
                    Name = "Rider"
                }
            });

            var editor = new RiderScriptEditor(discovery.Object, generator.Object);

            editor.TryGetInstallationForPath(path, out var installation);

            Assert.AreEqual(path, installation.Path);
        }
    }
}

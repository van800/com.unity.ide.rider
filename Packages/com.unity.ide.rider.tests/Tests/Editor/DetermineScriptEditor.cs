using NUnit.Framework;
using Moq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using Unity.CodeEditor;

namespace RiderEditor.Editor_spec
{
    [TestFixture]
    public class DetermineScriptEditor
    {
        [TestCase("/Applications/Rider EAP.app")]
        [TestCase("/Applications/Rider.app")]
        [TestCase("/Applications/Rider 2017.2.1.app")]
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void OSXPathDiscovery(string path)
        {
            Discover(path);
        }

        [TestCase(@"C:\Program Files\Rider\bin\rider.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider32.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider64.exe")]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void WindowsPathDiscovery(string path)
        {
            Discover(path);
        }

        [TestCase("/home/thatguy/Rider/bin/rider.sh")]
        [UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void LinuxPathDiscovery(string path)
        {
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

using System.IO;
using NUnit.Framework;
using Packages.Rider.Editor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Packages.Rider.Editor.Tests
{
    [TestFixture]
    public class RiderScriptEditorTests
    {
        [TestCase("/Applications/Rider EAP.app")]
        [TestCase("/Applications/Rider.app")]
        [TestCase("/Applications/Rider 2017.2.1.app")]
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void MacIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderInstallation(path));
        }
        
        [TestCase(@"C:\Program Files\Rider\bin\rider.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider32.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider64.exe")]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void WindowsIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderInstallation(path));
        }
        
        [TestCase("/home/thatguy/Rider/bin/rider.sh")]
        [UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void LinuxIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderInstallation(path));
        }
    }
}
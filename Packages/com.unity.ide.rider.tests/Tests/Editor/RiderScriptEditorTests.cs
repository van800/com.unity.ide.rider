using NUnit.Framework;
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
        [TestCase("/Applications/Fleet EAP.app")]
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void MacIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderOrFleetInstallation(path));
        }
        
        [TestCase(@"C:\Program Files\Rider\bin\rider.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider32.exe")]
        [TestCase(@"C:\Program Files\Rider\bin\rider64.exe")]
        [TestCase(@"C:\Users\user name\AppData\Local\JetBrains\Toolbox\apps\Fleet\ch-2\1.6.54\Fleet.exe")]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void WindowsIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderOrFleetInstallation(path));
        }
        
        [TestCase("/home/thatguy/Rider/bin/rider.sh")]
        [TestCase("/home/thatguy/Rider/bin/Fleet")]
        [UnityPlatform(RuntimePlatform.LinuxEditor)]
        public void LinuxIsRiderInstallationTest(string path)
        {
            Assert.IsTrue(RiderScriptEditor.IsRiderOrFleetInstallation(path));
        }
    }
}
using NUnit.Framework;
using Packages.Rider.Editor.Util;

namespace Packages.Rider.Editor.Tests
{
    [TestFixture]
    public class CommandLineParserTests
    {
        [Test]
        public void DuplicateArgsTest()
        {
            var args = new[]
            {
                "-projectPath", "/tmp/tmp-3277VO4shfR8EqMh",
                "-batchmode",
                "-automated",
                "-automated",
                "-testPlatform", "editmode",
            };
            var commandlineParser = new CommandLineParser(args);
            Assert.AreEqual(4, commandlineParser.Options.Count);
            Assert.True(commandlineParser.Options.ContainsKey("-automated"));
            Assert.AreEqual(commandlineParser.Options["-testPlatform"], "editmode");
        }
    }
}
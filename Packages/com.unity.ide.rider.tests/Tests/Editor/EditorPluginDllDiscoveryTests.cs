using System;
using NUnit.Framework;
using Packages.Rider.Editor.Util;

namespace Packages.Rider.Editor.Tests
{
    public class EditorPluginDllDiscoveryTests
    {
        [Test]
        public void VersionIsLowerTest()
        {
            const string prefix = "some_prefix_";
            var unityVersion = new Version(2020, 3);
            var closestMatch = UnityVersionUtils.FindClosestMatch(prefix, unityVersion,
                new[] { $"{prefix}2021.1", $"{prefix}2021.2", $"{prefix}2021.3" });
            Assert.IsNull(closestMatch);
        }

        [Test]
        public void SameVersionMatchTest()
        {
            const string prefix = "some_prefix_";
            var unityVersion = new Version(2021, 2);
            var requiredVersion = $"{prefix}2021.2";
            var closestMatch = UnityVersionUtils.FindClosestMatch(prefix, unityVersion,
                new[] { $"{prefix}2021.1", requiredVersion, $"{prefix}2021.3" });
            Assert.AreEqual(requiredVersion, closestMatch);
        }

        [Test]
        public void VersionIsHigherTest()
        {
            const string prefix = "some_prefix_";
            var unityVersion = new Version(6000, 2);
            var requiredVersion = $"{prefix}2021.2";
            var closestMatch =
                UnityVersionUtils.FindClosestMatch(prefix, unityVersion, new[] { $"{prefix}2021.1", requiredVersion });
            Assert.AreEqual(requiredVersion, closestMatch);
        }
    }
}
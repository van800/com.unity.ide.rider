using NUnit.Framework;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite;

namespace RiderEditor
{
    public class Package
    {
        [Test]
        public void Validate()
        {
            Assert.True(ValidationSuite.ValidatePackage("com.unity.ide.rider@1.1.0", ValidationType.LocalDevelopment));
        }
    }
}

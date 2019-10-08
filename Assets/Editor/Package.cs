using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.PackageManager.ValidationSuite;

namespace RiderEditor
{
    public class Package
    {
        [Test][UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor })]
        public void Validate()
        {
            const string package = "com.unity.ide.rider@1.1.2-preview.3";
            var result = ValidationSuite.ValidatePackage(package, ValidationType.LocalDevelopment);
            Debug.Log(ValidationSuite.GetValidationSuiteReport(package));
            Assert.True(result);
        }
    }
}

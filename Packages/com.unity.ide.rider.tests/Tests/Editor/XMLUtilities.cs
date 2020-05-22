using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Packages.Rider.Editor.Tests
{
    static class XMLUtilities
    {
        public static void AssertCompileItemsMatchExactly(XmlDocument projectXml, IEnumerable<string> expectedCompileItems)
        {
            var compileItems = projectXml.SelectAttributeValues("/msb:Project/msb:ItemGroup/msb:Compile/@Include", GetModifiedXmlNamespaceManager(projectXml)).ToArray();
            CollectionAssert.AreEquivalent(RelativeAssetPathsFor(expectedCompileItems), compileItems);
        }

        public static void AssertAnalyzerItemsMatchExactly(XmlDocument projectXml, IEnumerable<string> expectedAnalyzers)
        {
            CollectionAssert.AreEquivalent(
                expected: RelativeAssetPathsFor(expectedAnalyzers),
                actual: projectXml.SelectAttributeValues("/msb:Project/msb:ItemGroup/msb:Analyzer/@Include", GetModifiedXmlNamespaceManager(projectXml)).ToArray());
        }

        public static void AssertAnalyzerRuleSetsMatchExactly(XmlDocument projectXml, string expectedRuleSetFile)
        {
            CollectionAssert.Contains(
                projectXml.SelectElementValues("/msb:Project/msb:PropertyGroup/msb:CodeAnalysisRuleSet",
                    GetModifiedXmlNamespaceManager(projectXml)).ToArray(), expectedRuleSetFile);
        }
        public static void AssertNonCompileItemsMatchExactly(XmlDocument projectXml, IEnumerable<string> expectedNoncompileItems)
        {
            var nonCompileItems = projectXml.SelectAttributeValues("/msb:Project/msb:ItemGroup/msb:None/@Include", GetModifiedXmlNamespaceManager(projectXml)).ToArray();
            CollectionAssert.AreEquivalent(RelativeAssetPathsFor(expectedNoncompileItems), nonCompileItems);
        }

        public static void AssertOutputPath(XmlDocument projectXml, string expectedOutputPath)
        {
            var debugOutputPath = projectXml.SelectInnerText("/msb:Project/msb:PropertyGroup/msb:OutputPath", GetModifiedXmlNamespaceManager(projectXml)).First();
            Assert.AreEqual(expectedOutputPath, debugOutputPath);
        }

        static XmlNamespaceManager GetModifiedXmlNamespaceManager(XmlDocument projectXml)
        {
            var xmlNamespaces = new XmlNamespaceManager(projectXml.NameTable);
            xmlNamespaces.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
            return xmlNamespaces;
        }

        static IEnumerable<string> RelativeAssetPathsFor(IEnumerable<string> fileNames)
        {
            return fileNames.Select(fileName => fileName.Replace('/', '\\')).ToArray();
        }

        static IEnumerable<string> SelectAttributeValues(this XmlDocument xmlDocument, string xpathQuery, XmlNamespaceManager xmlNamespaceManager)
        {
            var result = xmlDocument.SelectNodes(xpathQuery, xmlNamespaceManager);
            foreach (XmlAttribute attribute in result)
                yield return attribute.Value;
        }

        static IEnumerable<string> SelectElementValues(this XmlDocument xmlDocument, string xPathQuery, XmlNamespaceManager xmlNamespaceManager)
        {
            var xmlNodeList = xmlDocument.SelectNodes(xPathQuery, xmlNamespaceManager);
            foreach (XmlElement node in xmlNodeList)
            {
                yield return node.InnerText;
            }
        }

        public static XmlDocument FromText(string textContent)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(textContent);
            return xmlDocument;
        }

        public static string GetInnerText(XmlDocument xmlDocument, string xpathQuery)
        {
            return xmlDocument.SelectSingleNode(xpathQuery, GetModifiedXmlNamespaceManager(xmlDocument)).InnerText;
        }
    }
}

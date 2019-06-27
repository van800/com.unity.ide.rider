using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class NewTestScript
    {
        public struct MultiplyTestCase
        {
            public int result;
            public (int, int) input;
        }

        public int Multiply(int x, int y) => x * y;

        private static readonly List<MultiplyTestCase> _testCases =
            new List<MultiplyTestCase>
            {
                new MultiplyTestCase {result = 4, input = (2, 2),},
                // Fail test case.
                new MultiplyTestCase {result = 5, input = (2, 3),},
                // Success test case.
                new MultiplyTestCase {result = 9, input = (3, 3),}
            };

        [Test, TestCaseSource(nameof(_testCases))]
        public void TestMultiply(MultiplyTestCase testCase)
        {
            Assert.AreEqual(testCase.result, Multiply(testCase.input.Item1, testCase.input.Item2));
        }
        
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            throw new Exception("");
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}

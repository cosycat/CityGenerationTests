#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils {
    public class UnitTesting : MonoBehaviour {
        
        [ContextMenu("Test Everything")]
        private void TestEverything() {
            List<ITestable> testClasses = new();
            // this loop from https://stackoverflow.com/a/12602220/12581784
            foreach (var mytype in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                         .Where(mytype => mytype .GetInterfaces().Contains(typeof(ITestable)))) {
                if (mytype.IsAbstract) continue;
                if (mytype.IsInterface) continue;
                if (mytype.IsGenericType) continue;
                if (mytype.GetConstructors().All(c => c.GetParameters().Length > 0)) continue;
                var testable = (ITestable)Activator.CreateInstance(mytype);
                testClasses.Add(testable);
            }

            var results = new List<(string testGroupName, TestResult[] results)>();
            foreach (var testable in testClasses) {
                var result = testable.TestAll();
                results.Add((testable.Name, result));
                
            }
            
            foreach (var (testGroupName, testResults) in results) {
                Debug.Log($"Test group {testGroupName}:");
                foreach (var testResult in testResults) {
                    if (testResult.Result) {
                        Debug.Log($"  {testResult.Name}: OK");
                    } else {
                        Debug.LogError($"  {testResult.Name}: FAIL - {testResult.ErrorMessage}");
                    }
                }
            }
        }
        
    }

    public struct TestResult {
        public bool Result;
        public string Name;
        public string? ErrorMessage;
        
        public static TestResult BuildResult(string name, params (bool result, string errorMessage)[] results) {
            var result = true;
            var errorMessage = "";
            foreach (var (r, e) in results) {
                if (!r) {
                    result = false;
                    errorMessage += e + "\n";
                }
            }

            return new TestResult {
                Result = result,
                Name = name,
                ErrorMessage = errorMessage
            };
        }
    }

    public interface ITestable {
        string Name { get; }
        TestResult[] TestAll() {
            var testResults = GetType().GetMethods().Where(m => m.ReturnType == typeof(TestResult)).Select(m => (TestResult)m.Invoke(this, null)).ToArray();
            return testResults;
        }
    }
    
    public class TestSystemTest : ITestable {
        public string Name => "Test System Test";
        
        public TestResult TestTestSystem() {
            return new TestResult {
                Result = true,
                Name = "Initial Test",
                ErrorMessage = null
            };
        }
    }
    
}
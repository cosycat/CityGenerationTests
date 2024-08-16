#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils {
    public class UnitTesting : MonoBehaviour {
        [MenuItem("UnitTesting/Test Everything")]
        [ContextMenu("Test Everything")]
        private static void TestEverything() {
            var newScene = CreateNewScene(out var previousScene);

            if (!newScene.IsValid()) {
                Debug.LogError("Failed to create new scene for testing.");
                return;
            }

            RunAllTests(out var results);

            LogResults(results);

            RestorePreviousScene(previousScene);
        }

        private static void LogResults(IEnumerable<(string testGroupName, TestResult[] results)> results) {
            foreach (var (testGroupName, testResults) in results) {
                Debug.Log($"Test group {testGroupName}:");
                foreach (var testResult in testResults) {
                    if (testResult.Result) {
                        Debug.Log($"  {testResult.Name}: OK");
                    }
                    else {
                        Debug.LogError($"  {testResult.Name}: FAIL - {testResult.ErrorMessage}");
                    }
                }
            }
        }

        private static void RunAllTests(out List<(string testGroupName, TestResult[] results)> results) {
            results = new List<(string testGroupName, TestResult[] results)>();
            foreach (var testable in GetAllTestClasses()) {
                var result = testable.TestAll();
                results.Add((testable.Name, result));
            }
        }

        private static IEnumerable<ITestable> GetAllTestClasses() {
            // main parts of loop from https://stackoverflow.com/a/12602220/12581784
            return (from mytype in System.Reflection.Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(mytype => mytype.GetInterfaces().Contains(typeof(ITestable)))
                where !mytype.IsAbstract
                where !mytype.IsInterface
                where !mytype.IsGenericType
                where !mytype.GetConstructors().All(c => c.GetParameters().Length > 0)
                select (ITestable)Activator.CreateInstance(mytype)).ToList();
        }

        private static Scene CreateNewScene(out string previousScenePath) {
            var previousScene = SceneManager.GetActiveScene();
            Debug.Log($"Previous scene: {previousScene.name}: {previousScene.path}");
            previousScenePath = previousScene.path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            return scene;
        }

        private static void RestorePreviousScene(string previousScenePath) {
            EditorSceneManager.OpenScene(previousScenePath);
            Debug.Log($"Restored previous scene: {previousScenePath}");
        }
    }

    public struct TestResult {
        public TestResult(bool result, string name, string? errorMessage) {
            Result = result;
            Name = name;
            ErrorMessage = errorMessage;
        }

        public bool Result { get; private set; }
        public string Name { get; private set; }
        public string? ErrorMessage { get; private set; }

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

        public TestResult AppendErrorMessageIfFalse(string errorMessage, bool newline = true) {
            if (Result) return this;
            ErrorMessage += (newline ? "\n" : "") + errorMessage;
            return this;
        }
    }

    
    public interface ITestable {
        string Name { get; }

        TestResult[] TestAll() {
            var testResults = GetType().GetMethods()
                .Where(m => m.ReturnType == typeof(TestResult))
                .Where(m => m.GetParameters().Length == 0)
                .Select(m => (TestResult)m.Invoke(this, null)).ToArray();
            return testResults;
        }
    }

    public class TestSystemTest : ITestable {
        public string Name => "Test System Test";

        public TestResult TestTestSystem() {
            return new TestResult(result: true, name: "Initial Test", errorMessage: "Failed to test the test system.");
        }
        
        public TestResult TestCombiningMultipleResults() {
            return TestResult.BuildResult("Combining Multiple Results",
                (true, "This should pass."),
                (false, "This should fail."),
                (true, "This should pass too."),
                (false, "This should fail too."));
        }
    }
}
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
            CreateNewScene(out var previousScene);
            
            var results = new List<(string testGroupName, TestResult[] results)>();
            foreach (var testable in GetAllTestClasses()) {
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
            
            RestorePreviousScene(previousScene);
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
            if (!scene.IsValid()) {
                Debug.LogError("Failed to create new scene for testing.");
            }

            return scene;
        }

        private static void RestorePreviousScene(string previousScenePath) {
            Debug.Log($"Restoring previous scene: {previousScenePath}");
            EditorSceneManager.OpenScene(previousScenePath);
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
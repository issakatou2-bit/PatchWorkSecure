using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PatchWorkSecure.EditorTools
{
    // Next-19：印のないテストも、完全名の一覧で実行する。Explicit撮影は混ぜない。
    [InitializeOnLoad]
    public static class CompanyOpsTestGroups
    {
        private const string ActiveKey = "pws_test_group_result_path";
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        [Serializable] public sealed class Entry { public string name, group; }
        [Serializable] public sealed class Inventory { public int total, normal, capture, longer, explicitCount; public List<Entry> tests = new List<Entry>(); }
        [Serializable] public sealed class TestResult { public string name, status, message, stackTrace; public double seconds; }
        [Serializable] public sealed class RunResult
        {
            public string group, status, startedUtc, finishedUtc, jobId;
            public int expected, passed, failed, skipped, inconclusive;
            public double seconds, wallSeconds;
            public string[] selected;
            public List<TestResult> tests = new List<TestResult>();
        }
        static CompanyOpsTestGroups() { TestRunnerApi.RegisterTestCallback(new Callbacks()); }
        public static void Inspect(string output = "Artifacts/Next19/groups.json")
        {
            EnsureIdle();
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(TestMode.PlayMode, tree =>
            {
                try { Write(Resolve(output), Collect(tree)); }
                finally { UnityEngine.Object.DestroyImmediate(api); }
            });
        }
        public static void Start(string group, string output, string[] only = null)
        {
            EnsureIdle();
            if (!new[] { "Normal", "Capture", "Long", "RegularCapture", "All" }.Contains(group))
                throw new ArgumentException("組はNormal/Capture/Long/RegularCapture/Allです。");
            string path = Resolve(output);
            if (File.Exists(path)) throw new IOException("既存の実行記録を上書きしません: " + output);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            // 一覧の取得中にも二重開始を防ぐ。再読み込みをまたぐ情報はSessionStateとJSONへ。
            SessionState.SetString(ActiveKey, path);
            api.RetrieveTestList(TestMode.PlayMode, tree =>
            {
                try
                {
                    var inventory = Collect(tree);
                    var selected = inventory.tests.Where(t => group == "All" || t.group == group ||
                        (group == "RegularCapture" && t.group != "Long")).Select(t => t.name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                    if (only != null)
                    {
                        if (only.Length == 0 || only.Distinct().Count() != only.Length || only.Except(selected).Any())
                            throw new ArgumentException("再実行の完全名がこの組の一覧と一致しません。");
                        selected = only;
                    }
                    if (selected.Length == 0) throw new InvalidOperationException("対象0件は成功扱いにしません。");
                    var run = new RunResult { group = group, status = "starting", expected = selected.Length,
                        selected = selected, startedUtc = DateTime.UtcNow.ToString("O") };
                    Write(path, run);
                    run.jobId = api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, testNames = selected }));
                    // Execute中にRunStartedが呼ばれた場合も、その状態を保つ。
                    var current = JsonUtility.FromJson<RunResult>(File.ReadAllText(path));
                    current.jobId = run.jobId; Write(path, current);
                }
                catch (Exception e)
                {
                    Write(path, new RunResult { group = group, status = "error", tests = new List<TestResult> {
                        new TestResult { status = "Error", message = e.ToString() } } });
                    SessionState.EraseString(ActiveKey); Debug.LogException(e);
                }
                finally { UnityEngine.Object.DestroyImmediate(api); }
            });
        }
        private static void EnsureIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                SessionState.GetBool("pws_tests_active", false) || !string.IsNullOrEmpty(SessionState.GetString(ActiveKey, "")))
                throw new InvalidOperationException("実行中または再コンパイル中です。中断実行は回数に数えません。");
        }
        private static Inventory Collect(ITestAdaptor tree)
        {
            var inventory = new Inventory(); Collect(tree, inventory);
            if (inventory.tests.Select(t => t.name).Distinct().Count() != inventory.tests.Count)
                throw new InvalidOperationException("テストの完全名が重複しています。");
            inventory.tests = inventory.tests.OrderBy(t => t.name, StringComparer.Ordinal).ToList();
            inventory.total = inventory.tests.Count;
            inventory.normal = inventory.tests.Count(t => t.group == "Normal");
            inventory.capture = inventory.tests.Count(t => t.group == "Capture");
            inventory.longer = inventory.tests.Count(t => t.group == "Long");
            return inventory;
        }
        private static void Collect(ITestAdaptor test, Inventory inventory)
        {
            bool explicitTest = test.RunState.ToString() == "Explicit" ||
                (test.Method?.MethodInfo?.GetCustomAttributes(false).Any(a => a.GetType().Name == "ExplicitAttribute") ?? false) ||
                (test.TypeInfo?.Type?.GetCustomAttributes(false).Any(a => a.GetType().Name == "ExplicitAttribute") ?? false);
            if (explicitTest) { inventory.explicitCount += test.TestCaseCount; return; }
            if (!test.HasChildren && !test.IsSuite)
            {
                var categories = test.Categories ?? Array.Empty<string>();
                bool longer = categories.Contains("Long"), capture = categories.Contains("Capture");
                if (longer && capture) throw new InvalidOperationException("組の重複: " + test.FullName);
                inventory.tests.Add(new Entry { name = test.FullName, group = longer ? "Long" : capture ? "Capture" : "Normal" });
            }
            else if (test.HasChildren) foreach (var child in test.Children) Collect(child, inventory);
        }
        private static string Resolve(string output)
        {
            string path = Path.GetFullPath(Path.Combine(Root, output));
            if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("出力先はプロジェクト内にしてください。");
            return path;
        }
        private static void Write<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(value, true));
        }
        private sealed class Callbacks : ICallbacks
        {
            private static void Update(Action<RunResult> action)
            {
                string path = SessionState.GetString(ActiveKey, "");
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                var run = JsonUtility.FromJson<RunResult>(File.ReadAllText(path)); action(run); Write(path, run);
            }
            public void RunStarted(ITestAdaptor testsToRun) { Update(run => run.status = "running"); }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite || result.HasChildren) return;
                Update(run => run.tests.Add(new TestResult { name = result.FullName, status = result.ResultState,
                    message = result.Message, stackTrace = result.StackTrace, seconds = result.Duration }));
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path = SessionState.GetString(ActiveKey, "");
                if (string.IsNullOrEmpty(path)) return;
                Update(run =>
                {
                    run.finishedUtc = DateTime.UtcNow.ToString("O"); run.wallSeconds = (DateTime.UtcNow - DateTime.Parse(run.startedUtc).ToUniversalTime()).TotalSeconds;
                    run.seconds = result.Duration; run.passed = result.PassCount; run.failed = result.FailCount;
                    run.skipped = result.SkipCount; run.inconclusive = result.InconclusiveCount;
                    bool exact = run.tests.Count == run.expected && run.tests.Select(t => t.name).Distinct().Count() == run.expected &&
                        !run.selected.Except(run.tests.Select(t => t.name)).Any();
                    run.status = run.tests.Any(t => t.status.Contains("Cancelled")) ? "cancelled" :
                        exact && run.passed == run.expected && run.failed == 0 && run.skipped == 0 && run.inconclusive == 0 ? "passed" : "failed";
                });
                TestRunnerApi.SaveResultToFile(result, Path.ChangeExtension(path, ".xml"));
                SessionState.EraseString(ActiveKey);
            }
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestCheckout.Editor
{
    public static class CheckoutBuild
    {
        const string Scene = "Assets/Scenes/QuestCheckout.unity";
        [MenuItem("Quest Checkout/Build Windows app")]
        public static void Build()
        {
            CreateScene();
            VerifyLogic();
            PlayerSettings.companyName = "CIAC";
            PlayerSettings.productName = "Quest Checkout";
            PlayerSettings.defaultScreenWidth = 1200;
            PlayerSettings.defaultScreenHeight = 854;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = "Builds/Windows/QuestCheckout.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
            Debug.Log("QUEST_CHECKOUT_BUILD_PASS");
        }
        [MenuItem("Quest Checkout/Create or open checkout scene")]
        public static void CreateScene()
        {
            CanvasSceneAuthoring.EnsureScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Quest Checkout/Verify checkout logic")]
        public static void VerifyLogic()
        {
            string directory = System.IO.Path.GetFullPath("Temp/CheckoutTests/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "loans.json");
            var now = new DateTime(2026, 10, 12, 16, 0, 0, DateTimeKind.Utc);
            var store = new LoanStore(path);
            bool[] yes = { true, true, true };
            store.Reset(false, now);
            Assert(store.AvailableCount(now) == 4, "Scenario A");
            var loan = store.Checkout(1, 90, yes, now);
            Assert(loan.dueUtc == now.AddMinutes(90).Ticks && store.AvailableCount(now) == 3, "90 minute checkout");
            Reject(() => store.Checkout(1, 90, yes, now), "Duplicate checkout");
            Reject(() => store.Checkout(0, 90, yes, now), "Invalid kit zero");
            Reject(() => store.Checkout(7, 90, yes, now), "Invalid kit seven");
            Reject(() => store.Checkout(3, 45, yes, now), "Invalid duration");
            Reject(() => store.Checkout(3, 90, new[] { true, false, true }, now), "Incomplete contents");
            var edited = store.Checkout(4, 60, yes, now);
            Assert(edited.kit == 4 && edited.dueUtc == now.AddMinutes(60).Ticks && store.Available(3, now), "Edited kit and duration");
            var restarted = new LoanStore(path);
            Assert(!restarted.Available(1, now) && restarted.Data.loans[0].dueUtc == loan.dueUtc, "Saved data after restart");
            Assert(restarted.Available(1, now.AddMinutes(90)), "Return deadline releases kit");
            store.Reset(true, now);
            Assert(store.AvailableCount(now) == 0 && store.NextAvailable(now) == now.AddMinutes(40), "No availability and next return");
            Reject(() => store.Checkout(3, 90, yes, now), "Unavailable kit");
            store.Reset(false, now);
            Assert(new LoanStore(path).AvailableCount(now) == 4, "Scenario reset persisted");
            var tomorrow = now.ToLocalTime().Date.AddDays(1);
            var reserved = store.Reserve(1, 90, yes, tomorrow, now);
            Assert(new DateTime(reserved.startUtc, DateTimeKind.Utc).ToLocalTime() == tomorrow.AddHours(9), "Future pickup at 9 AM");
            Assert(reserved.dueUtc - reserved.startUtc == TimeSpan.FromMinutes(90).Ticks, "Future duration");
            Assert(!new LoanStore(path).Available(1, now), "Future reservation persists and holds kit");
            Reject(() => store.Reserve(1, 90, yes, tomorrow.AddDays(1), now), "Cannot overwrite future reservation");
            Reject(() => store.Reserve(3, 90, yes, tomorrow.AddDays(-2), now), "Past day rejected");
            Reject(() => store.Reserve(3, 90, yes, tomorrow.AddDays(6), now), "Outside seven-day range rejected");
            store.Reset(false, now);
            File.WriteAllText(path, "broken json");
            var damaged = new LoanStore(path);
            Assert(!damaged.CanWrite && damaged.AvailableCount(now) == 0, "Corrupt save blocks checkout");
            damaged.Reset(false, now);
            Assert(damaged.CanWrite && new LoanStore(path).AvailableCount(now) == 4, "Corrupt save recoverable through reset");
            var unwritable = new LoanStore(directory); // Existing directory cannot be replaced by a file.
            Reject(() => unwritable.Checkout(1, 90, yes, now), "Save failure");
            Assert(unwritable.Available(1, now), "Save failure leaves inventory unchanged");
            var genericPath = System.IO.Path.Combine(directory, "generic.json");
            var generic = new LoanStore(genericPath, 3);
            generic.Reset(false, now);
            Assert(generic.AvailableCount(now) == 1 && generic.Available(1, now), "Generic prototype inventory");
            Reject(() => generic.Checkout(2, 90, yes, now), "Unavailable generic kit");
            Reject(() => generic.Checkout(4, 90, yes, now), "Invalid generic kit");
            generic.Checkout(1, 90, yes, now);
            Assert(!new LoanStore(genericPath, 3).Available(1, now), "Generic persistence");
            Assert(store.AvailableCount(now) == 4, "Generic inventory is separate from six Quest kits");
            Debug.Log("QUEST_CHECKOUT_LOGIC_PASS: checkout, blocked inputs, edit choices, persistence, expiry, scenario resets, corrupt save and write failure.");
        }
        static void Assert(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); Debug.Log("PASS: " + name); }
        static void Reject(Action action, string name)
        {
            bool rejected = false;
            try { action(); } catch { rejected = true; }
            Assert(rejected, name);
        }
    }
}

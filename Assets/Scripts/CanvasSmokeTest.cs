using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Optional developer checks. In a normal launch this component does nothing.
public class CanvasSmokeTest : MonoBehaviour
{
    public QuestCheckoutApp app;
    bool running, failed;
    float started;
    string directory;

    void Start()
    {
        if (!Environment.GetCommandLineArgs().Contains("--checkout-smoke")) return;
        running = true; started = Time.unscaledTime;
        Application.logMessageReceived += OnLog;
        directory = Path.Combine(Application.dataPath, "..", "Screenshots"); Directory.CreateDirectory(directory);
        StartCoroutine(Run());
    }

    void OnLog(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true; }
    void Update() { if (running && Time.unscaledTime - started > 65) { Debug.LogError("QUEST_CHECKOUT_SMOKE_TIMEOUT"); Application.Quit(1); } }
    void OnDestroy() => Application.logMessageReceived -= OnLog;
    void Check(bool value, string message) { if (!value) throw new Exception("Canvas smoke: " + message); }
    Button Find(string name) => app.GetComponentsInChildren<Button>(true).First(b => b.name == name && b.gameObject.activeInHierarchy);

    void Click(string name)
    {
        var b = Find(name); Check(b.IsInteractable(), name + " is disabled");
        Canvas.ForceUpdateCanvases();
        var rect = b.GetComponent<RectTransform>();
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == b, name + " is blocked by " + (hits.Count > 0 ? hits[0].gameObject.name : "no registered graphic"));
        ExecuteEvents.Execute(b.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    void Hover(string name, bool enter)
    {
        var data = new PointerEventData(EventSystem.current);
        if (enter) ExecuteEvents.Execute(Find(name).gameObject, data, ExecuteEvents.pointerEnterHandler);
        else ExecuteEvents.Execute(Find(name).gameObject, data, ExecuteEvents.pointerExitHandler);
    }

    IEnumerator Run()
    {
        int elements = app.GetComponentsInChildren<RectTransform>(true).Length;
        Check(elements > 200, "Canvas elements not serialized");
        app.ResetScenarioA(); app.ShowPage("Home"); yield return Capture("01-home");
        Hover("Meta Quest 3 Button", true); yield return Capture("10-home-hover"); Hover("Meta Quest 3 Button", false);
        Check(!Find("Meta Quest 3 Button").GetComponent<PantherHover>().selectingPanther.activeSelf, "hover stayed visible");
        Hover("Electronics Button", true); yield return Capture("11-blocked-hover"); Hover("Electronics Button", false);
        Click("Meta Quest 3 Button");
        float x = app.transition.panther.anchoredPosition.x;
        yield return Capture("12-loading");
        Check(app.transition.panther.anchoredPosition.x > x, "transition did not move right");
        while (app.Busy) yield return null;
        Check(app.Page == "QuestKits", "menu navigation failed"); yield return Capture("02-kits");
        Hover("Quest Kit 3 Button", true); yield return Capture("17-top-kit-hover"); Hover("Quest Kit 3 Button", false);
        Hover("Quest Kit 5 Button", true); yield return Capture("13-kit-hover"); Hover("Quest Kit 5 Button", false);
        Click("Quest Kit 2 Button"); Check(app.Page == "QuestKits" && !app.Busy, "unavailable Quest kit navigated");
        Click("Quest Kit 1 Button"); while (app.Busy) yield return null;
        yield return Capture("03-reserve");
        Check(!app.reservation.reviewButton.interactable, "checklist did not block review");
        foreach (var toggle in app.reservation.contents) toggle.isOn = true;
        Check(app.reservation.reviewButton.interactable, "persistent Toggle events failed");
        yield return Capture("18-verified-contents"); Click("Review Reservation Button");
        Check(app.Page == "Review", "review button failed"); yield return null;
        Click("Edit Choices Button"); yield return null; Click("60 Minutes Button");
        Check(app.Duration == 60 && app.reservation.Verified.All(v => v), "editing lost values");
        Click("90 Minutes Button"); Click("Review Reservation Button"); yield return Capture("04-review");
        Click("Confirm Reservation Button"); yield return Capture("05-success");
        Check(app.Page == "Success" && !new LoanStore(app.QuestStore.Path).Available(1, DateTime.UtcNow), "Quest persistence failed");
        Click("Test Controls Button"); yield return Capture("08-test-controls"); Click("Cancel Button");
        Check(!app.resetDialog.activeSelf, "cancel failed"); Click("Test Controls Button"); yield return null; Click("Reset Scenario B Button");
        Check(app.Page == "QuestUnavailable", "reset B did not show empty inventory"); yield return Capture("06-unavailable");
        Click("Next Availability Button"); while (app.Busy) yield return null; yield return Capture("07-next-availability");
        app.ShowPage("GenericKits"); yield return Capture("09-other-options");
        Hover("Generic VR 1 Button", true); yield return Capture("14-generic-hover"); Hover("Generic VR 1 Button", false);
        Click("Generic VR 2 Button"); Check(app.Page == "GenericKits" && !app.Busy, "unavailable generic kit navigated");
        Click("Generic VR 1 Button"); while (app.Busy) yield return null; yield return Capture("15-generic-reserve");
        Check(app.reservation.controllerLabel.text == "1 Controller", "generic contents incorrect");
        foreach (var toggle in app.reservation.contents) toggle.isOn = true;
        Click("Review Reservation Button"); yield return null; Click("Confirm Reservation Button"); yield return Capture("16-generic-success");
        Check(!new LoanStore(app.GenericStore.Path, 3).Available(1, DateTime.UtcNow), "generic persistence failed");
        Check(app.QuestStore.AvailableCount(DateTime.UtcNow) == 0, "generic checkout altered Quest inventory");
        Check(elements == app.GetComponentsInChildren<RectTransform>(true).Length, "UI objects were created/destroyed during navigation");
        Debug.Log(failed ? "QUEST_CHECKOUT_SMOKE_FAIL" : "QUEST_CHECKOUT_SMOKE_PASS: Canvas controls, persistent listeners, hover, transition, checkout and unchanged serialized hierarchy.");
        running = false; Application.Quit(failed ? 1 : 0);
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForSeconds(.45f); yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG()); Destroy(image);
        yield return new WaitForSeconds(.25f);
    }
}

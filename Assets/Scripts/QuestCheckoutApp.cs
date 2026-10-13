using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Navigation and saved data only. UI objects are authored in the scene/prefab.
public class QuestCheckoutApp : MonoBehaviour
{
    [Header("Canvas screens")]
    public CanvasScreen[] screens;
    public InventoryScreen questInventory, unavailableInventory, genericInventory;
    public ReservationForm reservation;
    public CheckoutDetailsView review, receipt, waiting;
    public PantherTransition transition;
    [Header("Shared controls")]
    public GameObject homeButton, footer, resetDialog;
    public Text alert;
    public LoanStore QuestStore { get; private set; }
    public LoanStore GenericStore { get; private set; }
    public bool Generic { get; private set; }
    public int SelectedKit { get; private set; } = 1;
    public int Duration { get; private set; } = 90;
    public DateTime ReservationDay { get; private set; } = DateTime.Today;
    public DateTime ReservationStart => ReservationDay == DateTime.Today ? DateTime.UtcNow : ReservationDay.AddHours(9).ToUniversalTime();
    public KitLoan LastReceipt { get; private set; }
    public string Page { get; private set; }
    public LoanStore CurrentStore => Generic ? GenericStore : QuestStore;
    public string KitName => Generic ? $"Generic VR {SelectedKit}" : $"Meta Quest 3 · Kit {SelectedKit}";
    public string InventoryPage => Generic ? "GenericKits" : "QuestKits";
    public bool Busy => transition.IsPlaying;
    float refreshAt;
    public static string SavePath => Path.Combine(Application.persistentDataPath, "quest-loans.json");

    void Awake()
    {
        Application.targetFrameRate = 60;
        bool smoke = Environment.GetCommandLineArgs().Contains("--checkout-smoke");
        QuestStore = new LoanStore(smoke ? Path.Combine(Application.persistentDataPath, "smoke-loans.json") : SavePath);
        GenericStore = new LoanStore(Path.Combine(Application.persistentDataPath, smoke ? "smoke-generic-loans.json" : "generic-loans.json"), 3);
        Attempt(() => { if (!File.Exists(QuestStore.Path)) QuestStore.Reset(false, DateTime.UtcNow); });
        Attempt(() => { if (!File.Exists(GenericStore.Path)) GenericStore.Reset(false, DateTime.UtcNow); });
        ShowPage("Home");
    }

    void Update()
    {
        if (Busy || resetDialog.activeSelf || Time.unscaledTime < refreshAt) return;
        refreshAt = Time.unscaledTime + 15;
        if (Page == "QuestKits" || Page == "QuestUnavailable" || Page == "GenericKits" || Page == "Waiting") ShowPage(Page);
    }

    public void Navigate(string target)
    {
        if (Busy) return;
        ShowPage("Loading");
        transition.Play(() => ShowPage(target));
    }

    public void ShowPage(string target)
    {
        if (target == "Home" || target == "QuestKits" || target == "QuestUnavailable" || target == "Waiting") Generic = false;
        if (target == "GenericKits") Generic = true;
        if (target == "QuestKits" || target == "QuestUnavailable") target = QuestStore.AvailableCount(DateTime.UtcNow) == 0 ? "QuestUnavailable" : "QuestKits";
        Page = target;
        SetNavigationEnabled(true);
        foreach (var screen in screens) screen.gameObject.SetActive(screen.key == target);
        resetDialog.SetActive(false);
        homeButton.SetActive(target != "Home" && target != "Loading");
        footer.SetActive(target != "Loading");
        ClearAlert();
        switch (target)
        {
            case "QuestKits": questInventory.Refresh(); break;
            case "QuestUnavailable": unavailableInventory.Refresh(); break;
            case "GenericKits": genericInventory.Refresh(); break;
            case "Reservation": reservation.Refresh(); break;
            case "Review": review.Refresh(); break;
            case "Success": receipt.Refresh(); break;
            case "Waiting": waiting.Refresh(); break;
        }
        if (target != "Loading" && !CurrentStore.CanWrite) ShowAlert(CurrentStore.LoadWarning);
    }

    public void SelectQuestKit(int kit) => SelectKit(kit, false);
    public void SelectGenericKit(int kit) => SelectKit(kit, true);

    void SelectKit(int kit, bool generic)
    {
        if (Busy) return;
        if (!(generic ? GenericStore : QuestStore).Available(kit, DateTime.UtcNow)) { ShowAlert("This kit is unavailable. Please choose an available kit."); return; }
        Generic = generic; SelectedKit = kit; reservation.ClearChecklist(); Navigate("Reservation");
    }

    public void SetDuration(int minutes) { Duration = minutes; reservation.Refresh(); }
    public void SetReservationDay(int index) { ReservationDay = DateTime.Today.AddDays(index); reservation.Refresh(); }
    public void ChangeKit() => Navigate(InventoryPage);

    public void ReviewChoices()
    {
        if (!reservation.Verified.All(v => v)) { ShowAlert("Verify all three kit contents before continuing."); return; }
        ShowPage("Review");
    }

    public void EditChoices() => ShowPage("Reservation");
    public void Confirm() => Attempt(() => { LastReceipt = CurrentStore.Reserve(SelectedKit, Duration, reservation.Verified, ReservationDay, DateTime.UtcNow); ShowPage("Success"); });
    public void BackToInventory() => Navigate(InventoryPage);
    public void RefreshAvailability() => ShowPage(QuestStore.AvailableCount(DateTime.UtcNow) > 0 ? "QuestKits" : "Waiting");
    public void UnavailableCategory() => ShowAlert("This category is unavailable at this kiosk. Please visit CIAC 2.");

    public void OpenTestControls()
    {
        if (Busy) return;
        ClearAlert(); resetDialog.SetActive(true); SetNavigationEnabled(false);
        EventSystem.current.SetSelectedGameObject(resetDialog.GetComponentInChildren<Button>().gameObject);
    }

    public void CloseTestControls()
    {
        resetDialog.SetActive(false); SetNavigationEnabled(true);
        EventSystem.current.SetSelectedGameObject(footer.GetComponentInChildren<Button>().gameObject);
    }

    void SetNavigationEnabled(bool enabled)
    {
        foreach (var screen in screens) screen.GetComponent<CanvasGroup>().interactable = enabled;
        homeButton.GetComponent<CanvasGroup>().interactable = enabled;
        footer.GetComponent<CanvasGroup>().interactable = enabled;
    }

    public void ResetScenarioA() => ResetScenario(false);
    public void ResetScenarioB() => ResetScenario(true);

    void ResetScenario(bool busy)
    {
        Attempt(() => { QuestStore.Reset(busy, DateTime.UtcNow); GenericStore.Reset(false, DateTime.UtcNow); SelectedKit = 1; Duration = 90; ReservationDay = DateTime.Today; LastReceipt = null; reservation.ClearChecklist(); ShowPage("QuestKits"); });
    }

    void Attempt(Action action) { try { action(); } catch (Exception ex) { ShowAlert(ex.Message); Debug.LogWarning(ex.Message); } }
    public void ShowAlert(string message) { alert.text = message; alert.gameObject.SetActive(true); alert.transform.SetAsLastSibling(); }
    void ClearAlert() { alert.text = ""; alert.gameObject.SetActive(false); }
    public static string Format(DateTime utc) => utc.ToLocalTime().ToString("dd MMM yyyy 'at' h:mm tt");
}

using System;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class KitLoan
{
    public int kit;
    public long startUtc;
    public long dueUtc;
}

//made by Pato23K
[Serializable]
public class LoanData
{
    public int version = 1;
    public KitLoan[] loans = new KitLoan[6];
}

// All mutations are validated and persisted before they are exposed to the UI.
public class LoanStore
{
    public const string Pickup = "CIAC 2";
    public readonly string Path;
    readonly int kitCount;
    public LoanData Data { get; private set; }
    public string LoadWarning { get; private set; }
    public bool CanWrite { get; private set; } = true;

    public LoanStore(string path, int kitCount = 6)
    {
        if (kitCount != 3 && kitCount != 6) throw new ArgumentOutOfRangeException(nameof(kitCount));
        this.kitCount = kitCount;
        Path = path;
        Data = Empty();
        if (!File.Exists(path)) return;
        try
        {
            var loaded = JsonUtility.FromJson<LoanData>(File.ReadAllText(path));
            Validate(loaded);
            Data = loaded;
        }
        catch (Exception ex)
        {
            // Never silently treat damaged inventory as six available kits.
            CanWrite = false;
            LoadWarning = "Saved inventory could not be read. Checkout is blocked. Use Test controls to reset the demo data. " + ex.Message;
        }
    }

    LoanData Empty() => new LoanData { loans = Enumerable.Range(1, kitCount).Select(i => new KitLoan { kit = i }).ToArray() };

    void Validate(LoanData data)
    {
        if (data == null || data.version != 1 || data.loans == null || data.loans.Length != kitCount) throw new InvalidDataException("Invalid inventory format.");
        for (int i = 0; i < kitCount; i++)
        {
            var l = data.loans[i];
            if (l == null || l.kit != i + 1 || l.startUtc < 0 || l.dueUtc < l.startUtc || l.dueUtc > DateTime.MaxValue.Ticks)
                throw new InvalidDataException("Invalid kit record.");
        }
    }

    public bool Available(int kit, DateTime now) => CanWrite && kit >= 1 && kit <= kitCount && Data.loans[kit - 1].dueUtc <= now.ToUniversalTime().Ticks;
    public int AvailableCount(DateTime now) => Enumerable.Range(1, kitCount).Count(k => Available(k, now));

    public DateTime NextAvailable(DateTime now)
    {
        long ticks = Data.loans.Min(l => l.dueUtc);
        return new DateTime(Math.Max(ticks, now.ToUniversalTime().Ticks), DateTimeKind.Utc);
    }

    public KitLoan Checkout(int kit, int minutes, bool[] verified, DateTime now)
    {
        if (!CanWrite) throw new InvalidOperationException("Reset the unreadable inventory before continuing.");
        if (kit < 1 || kit > kitCount) throw new InvalidOperationException($"Choose a valid kit from 1 to {kitCount}.");
        if (minutes != 30 && minutes != 60 && minutes != 90 && minutes != 120) throw new InvalidOperationException("Choose a supported duration.");
        if (verified == null || verified.Length != 3 || verified.Any(v => !v)) throw new InvalidOperationException("Verify the headset, controller(s) and charger first.");
        if (!Available(kit, now)) throw new InvalidOperationException("This kit is no longer available. Choose another kit.");
        var next = Clone();
        var loan = new KitLoan { kit = kit, startUtc = now.ToUniversalTime().Ticks, dueUtc = now.ToUniversalTime().AddMinutes(minutes).Ticks };
        next.loans[kit - 1] = loan;
        Save(next);
        return loan;
    }

    LoanData Clone() => JsonUtility.FromJson<LoanData>(JsonUtility.ToJson(Data));

    public KitLoan Reserve(int kit, int minutes, bool[] verified, DateTime day, DateTime now)
    {
        var today = now.ToLocalTime().Date;
        if (day.Date < today || day.Date > today.AddDays(6)) throw new InvalidOperationException("Choose today or one of the next six days.");
        // One outstanding reservation per kit; never overwrite an existing hold.
        if (!Available(kit, now)) throw new InvalidOperationException("This kit is already on loan or reserved. Choose another kit.");
        var start = day.Date == today ? now : DateTime.SpecifyKind(day.Date.AddHours(9), DateTimeKind.Local).ToUniversalTime();
        return Checkout(kit, minutes, verified, start);
    }

    public void Reset(bool allBusy, DateTime now)
    {
        var next = Empty();
        // Scenario A matches the prototype: kits 2 and 6 are on loan.
        foreach (var l in next.loans)
            if (allBusy || l.kit == 2 || l.kit == kitCount)
            {
                l.startUtc = now.ToUniversalTime().Ticks;
                l.dueUtc = now.ToUniversalTime().AddMinutes(30 + l.kit * 10).Ticks;
            }
        Save(next);
        CanWrite = true;
        LoadWarning = null;
    }

    void Save(LoanData next)
    {
        Validate(next);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(next, true));
        if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak");
        else File.Move(temporary, Path);
        Data = next;
    }
}

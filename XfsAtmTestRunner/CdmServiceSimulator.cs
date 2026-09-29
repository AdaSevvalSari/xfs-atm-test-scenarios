namespace XfsAtmTestRunner;

/// <summary>
/// CEN/XFS 3.40 Part 5 – Cash Dispenser (CDM) simülatörü.
/// Gerçek bir XFS SP implementasyonu yerine mock state-machine kullanır.
/// </summary>
public class CdmServiceSimulator
{
    public WfsCdmStatus Status { get; private set; } = new();
    public List<string> EventLog { get; } = new();

    private readonly Dictionary<int, int> _cassettes = new()
    {
        { 200, 50 },
        { 100, 80 },
        { 50,  100 }
    };

    // ── WFS_INF_CDM_STATUS ───────────────────────────────────────────────────
    public WfsCdmStatus QueryStatus()
    {
        EventLog.Add($"[CDM] WFS_INF_CDM_STATUS -> Device={Status.Device}, fwDispenser={Status.Dispenser}, fwShutter={Status.Shutter}, fwTransport={Status.Transport}");
        return Status;
    }

    // ── WFS_CMD_CDM_DENOMINATE ────────────────────────────────────────────────
    public (WfsCdmResult result, WfsCdmDenomination? denom) Denominate(long amount, string currency = "TRY")
    {
        if (Status.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
            return (WfsCdmResult.WFS_ERR_CDM_HWERROR, null);
        if (Status.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
            return (WfsCdmResult.WFS_ERR_CDM_HWERROR, null);

        var denom = new WfsCdmDenomination { Currency = currency, Amount = amount };
        long remaining = amount;

        foreach (var (note, count) in _cassettes.OrderByDescending(k => k.Key))
        {
            if (remaining <= 0) break;
            int needed = (int)(remaining / note);
            int use = Math.Min(needed, count);
            if (use > 0)
            {
                denom.Notes[note] = use;
                remaining -= (long)note * use;
            }
        }

        if (remaining != 0)
            return (WfsCdmResult.WFS_ERR_CDM_NOTDISPENSABLE, null);

        return (WfsCdmResult.WFS_SUCCESS, denom);
    }

    // ── WFS_CMD_CDM_DISPENSE ─────────────────────────────────────────────────
    public WfsCdmResult Dispense(WfsCdmDenomination denom)
    {
        if (Status.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
            return WfsCdmResult.WFS_ERR_CDM_HWERROR;
        if (Status.Shutter == WfsCdmShutterStatus.WFS_CDM_SHTOPEN)
            return WfsCdmResult.WFS_ERR_CDM_SHUTTEROPEN;

        foreach (var (note, count) in denom.Notes)
        {
            if (!_cassettes.ContainsKey(note) || _cassettes[note] < count)
            {
                Log(WfsCdmEvent.WFS_EXEE_CDM_CASHUNITERROR, $"Cassette {note} insufficient");
                return WfsCdmResult.WFS_ERR_CDM_NOITEMS;
            }
        }

        foreach (var (note, count) in denom.Notes)
            _cassettes[note] -= count;

        Status.IntermediateStackerCount = denom.Notes.Values.Sum();
        Log(WfsCdmEvent.WFS_EXEE_CDM_STARTDISPENSE, $"Dispensing {denom.Amount} {denom.Currency}");
        return WfsCdmResult.WFS_SUCCESS;
    }

    // ── WFS_CMD_CDM_PRESENT ──────────────────────────────────────────────────
    public WfsCdmResult Present()
    {
        if (Status.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
            return WfsCdmResult.WFS_ERR_CDM_HWERROR;
        if (Status.IntermediateStackerCount == 0)
            return WfsCdmResult.WFS_ERR_CDM_NOITEMS;

        Status.Shutter = WfsCdmShutterStatus.WFS_CDM_SHTOPEN;
        Log(WfsCdmEvent.WFS_SRVE_CDM_ITEMSPRESENTED, "Cash presented at outlet");
        return WfsCdmResult.WFS_SUCCESS;
    }

    // ── Simulate customer taking cash ────────────────────────────────────────
    public void SimulateItemsTaken()
    {
        Status.Shutter = WfsCdmShutterStatus.WFS_CDM_SHTCLOSED;
        Status.IntermediateStackerCount = 0;
        Status.Dispenser = WfsCdmDispenserStatus.WFS_CDM_DISPOK;
        Log(WfsCdmEvent.WFS_SRVE_CDM_ITEMSTAKEN, "Customer collected cash");
    }

    // ── Simulate items NOT taken (timeout) ───────────────────────────────────
    public WfsCdmResult SimulateRetract()
    {
        if (Status.Shutter != WfsCdmShutterStatus.WFS_CDM_SHTOPEN)
            return WfsCdmResult.WFS_ERR_CDM_NOITEMS;

        Status.Shutter = WfsCdmShutterStatus.WFS_CDM_SHTCLOSED;
        Status.IntermediateStackerCount = 0;
        Status.Dispenser = WfsCdmDispenserStatus.WFS_CDM_DISPOK;
        EventLog.Add("[CDM] WFS_CMD_CDM_RETRACT -> WFS_SUCCESS: fwShutter=WFS_CDM_SHTCLOSED, StackerCount=0, fwDispenser=WFS_CDM_DISPOK");
        return WfsCdmResult.WFS_SUCCESS;
    }

    // ── Force device offline (for CDM-02 test) ───────────────────────────────
    public void SetDeviceOffline()
    {
        Status.Device = WfsDeviceStatus.WFS_STAT_DEVHWERROR;
        Status.Dispenser = WfsCdmDispenserStatus.WFS_CDM_DISPCUSTOP;
        Log(WfsCdmEvent.WFS_SRVE_CDM_CASHUNITERROR, "Device hardware error – offline");
    }

    // ── Force dispense error mid-transaction (for CDM-03 test) ───────────────
    public void InjectDispenseError()
    {
        Status.Dispenser = WfsCdmDispenserStatus.WFS_CDM_DISPCUSTOP;
        Log(WfsCdmEvent.WFS_EXEE_CDM_NOTEERROR, "Note transport jam detected");
    }

    // ── WFS_CMD_CDM_RESET ────────────────────────────────────────────────────
    public WfsCdmResult Reset()
    {
        Status = new WfsCdmStatus();
        Log(WfsCdmEvent.WFS_SRVE_CDM_COUNTS_CHANGED, "CDM reset completed");
        return WfsCdmResult.WFS_SUCCESS;
    }

    private void Log(WfsCdmEvent evt, string detail)
        => EventLog.Add($"[CDM] {evt}: {detail}");
}

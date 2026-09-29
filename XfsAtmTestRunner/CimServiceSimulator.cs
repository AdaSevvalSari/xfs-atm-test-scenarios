namespace XfsAtmTestRunner;

/// <summary>
/// CEN/XFS 3.40 Part 15 – CIM (Cash-In Module) simülatörü.
/// </summary>
public class CimServiceSimulator
{
    public WfsCimStatus Status { get; private set; } = new();
    public List<string> EventLog { get; } = new();

    private bool _cashInSessionActive = false;
    private WfsCimCashInResult? _stagedCashIn;

    private readonly Dictionary<int, int> _cashInCassettes = new()
    {
        { 200, 0 },
        { 100, 0 },
        { 50, 0 }
    };
    private int _cassetteFreeSlots = 500;

    // ── WFS_INF_CIM_STATUS ───────────────────────────────────────────────────
    public WfsCimStatus QueryStatus()
    {
        EventLog.Add($"[CIM] WFS_INF_CIM_STATUS -> Device={Status.Device}, fwAccept={Status.Accept}, fwShutter={Status.Shutter}, fwIntermediateStacker={Status.IntermediateStacker}");
        return Status;
    }

    // ── WFS_CMD_CIM_CASH_IN_START ────────────────────────────────────────────
    public WfsCimResult CashInStart()
    {
        if (Status.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
            return WfsCimResult.WFS_ERR_CIM_HWERROR;
        if (Status.Accept != WfsCimAcceptStatus.WFS_CIM_ACCOK)
            return WfsCimResult.WFS_ERR_CIM_HWERROR;
        if (_cashInSessionActive)
            return WfsCimResult.WFS_ERR_CIM_CASHINACTIVE;

        _cashInSessionActive = true;
        _stagedCashIn = new WfsCimCashInResult();
        EventLog.Add("[CIM] WFS_CMD_CIM_CASH_IN_START -> WFS_SUCCESS");
        return WfsCimResult.WFS_SUCCESS;
    }

    // ── WFS_CMD_CIM_CASH_IN ──────────────────────────────────────────────────
    public (WfsCimResult result, WfsCimCashInResult? detail) CashIn(Dictionary<int, int> insertedNotes)
    {
        if (!_cashInSessionActive)
            return (WfsCimResult.WFS_ERR_CIM_NOTCASHINACTIVE, null);
        if (Status.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
            return (WfsCimResult.WFS_ERR_CIM_HWERROR, null);

        Status.Shutter = WfsCimShutterStatus.WFS_CIM_SHTOPEN;
        Log(WfsCimEvent.WFS_SRVE_CIM_ITEMSINSERTED, "Items inserted by customer");

        int totalNotes = insertedNotes.Values.Sum();
        if (totalNotes > _cassetteFreeSlots)
        {
            Status.Shutter = WfsCimShutterStatus.WFS_CIM_SHTCLOSED;
            Log(WfsCimEvent.WFS_EXEE_CIM_INPUTREFUSE, "Cassette full – notes refused");
            return (WfsCimResult.WFS_ERR_CIM_CASH_UNIT_ERROR, null);
        }

        long amount = 0;
        foreach (var (note, count) in insertedNotes)
        {
            if (!_cashInCassettes.ContainsKey(note))
            {
                Log(WfsCimEvent.WFS_EXEE_CIM_INPUTREFUSE, $"Unknown denomination {note} – refused");
                return (WfsCimResult.WFS_ERR_CIM_UNSUPPORTED, null);
            }
            _stagedCashIn!.NotesAccepted[note] = _stagedCashIn.NotesAccepted.GetValueOrDefault(note) + count;
            amount += (long)note * count;
        }

        _stagedCashIn!.AmountAccepted += amount;
        Status.Shutter = WfsCimShutterStatus.WFS_CIM_SHTCLOSED;
        Status.IntermediateStacker = WfsCimIntermediateStatus.WFS_CIM_ISNOTEMPTY;
        Log(WfsCimEvent.WFS_EXEE_CIM_SUBCASHIN, $"Accepted {amount} TRY in stager");

        return (WfsCimResult.WFS_SUCCESS, _stagedCashIn);
    }

    // ── WFS_CMD_CIM_CASH_IN_END ──────────────────────────────────────────────
    public (WfsCimResult result, WfsCimCashInResult? final) CashInEnd()
    {
        if (!_cashInSessionActive)
            return (WfsCimResult.WFS_ERR_CIM_NOTCASHINACTIVE, null);

        foreach (var (note, count) in _stagedCashIn!.NotesAccepted)
        {
            _cashInCassettes[note] += count;
            _cassetteFreeSlots -= count;
        }

        var result = _stagedCashIn;
        _cashInSessionActive = false;
        _stagedCashIn = null;
        Status.Accept = WfsCimAcceptStatus.WFS_CIM_ACCOK;
        Status.IntermediateStacker = WfsCimIntermediateStatus.WFS_CIM_ISEMPTY;
        Log(WfsCimEvent.WFS_SRVE_CIM_CASHUNITINFOCHANGED, $"Cash-in committed: {result.AmountAccepted} TRY");

        return (WfsCimResult.WFS_SUCCESS, result);
    }

    // ── WFS_CMD_CIM_CASH_IN_ROLLBACK ────────────────────────────────────────
    public WfsCimResult CashInRollback()
    {
        if (!_cashInSessionActive)
            return WfsCimResult.WFS_ERR_CIM_NOTCASHINACTIVE;

        _cashInSessionActive = false;
        _stagedCashIn = null;
        Status.Accept = WfsCimAcceptStatus.WFS_CIM_ACCOK;
        Status.IntermediateStacker = WfsCimIntermediateStatus.WFS_CIM_ISEMPTY;
        Status.Shutter = WfsCimShutterStatus.WFS_CIM_SHTOPEN;
        Log(WfsCimEvent.WFS_SRVE_CIM_ITEMSPRESENTED, "Notes returned to customer (rollback)");
        return WfsCimResult.WFS_SUCCESS;
    }

    // ── Simulate customer collecting returned notes ───────────────────────────
    public void SimulateRollbackItemsTaken()
    {
        Status.Shutter = WfsCimShutterStatus.WFS_CIM_SHTCLOSED;
        Log(WfsCimEvent.WFS_SRVE_CIM_ITEMSTAKEN, "Returned notes taken by customer");
    }

    // ── Force device not ready (CIM-02) ──────────────────────────────────────
    public void SetDeviceNotReady()
    {
        Status.Device = WfsDeviceStatus.WFS_STAT_DEVHWERROR;
        Status.Accept = WfsCimAcceptStatus.WFS_CIM_ACCCUSTOP;
        Log(WfsCimEvent.WFS_SRVE_CIM_CASHUNITERROR, "CIM device hardware error");
    }

    // ── Force hardware error during cash-in (CIM-03) ─────────────────────────
    public void InjectCashInError()
    {
        Status.Device = WfsDeviceStatus.WFS_STAT_DEVHWERROR;
        Log(WfsCimEvent.WFS_EXEE_CIM_NOTEERROR, "Note transport error during cash-in");
    }

    // ── WFS_CMD_CIM_RESET ────────────────────────────────────────────────────
    public WfsCimResult Reset()
    {
        Status = new WfsCimStatus();
        _cashInSessionActive = false;
        _stagedCashIn = null;
        Log(WfsCimEvent.WFS_SRVE_CIM_CASHUNITINFOCHANGED, "CIM reset completed");
        return WfsCimResult.WFS_SUCCESS;
    }

    private void Log(WfsCimEvent evt, string detail)
        => EventLog.Add($"[CIM] {evt}: {detail}");
}

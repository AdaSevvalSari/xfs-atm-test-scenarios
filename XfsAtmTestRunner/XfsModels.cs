namespace XfsAtmTestRunner;

// ─── CEN/XFS 3.40 Device Status ───────────────────────────────────────────────
public enum WfsDeviceStatus
{
    WFS_STAT_DEVONLINE = 0,
    WFS_STAT_DEVOFFLINE = 1,
    WFS_STAT_DEVPOWEROFF = 2,
    WFS_STAT_DEVNODEVICE = 3,
    WFS_STAT_DEVHWERROR = 4,
    WFS_STAT_DEVUSERERROR = 5,
    WFS_STAT_DEVFRAUDATTEMPT = 6,
    WFS_STAT_DEVPOTENTIALFRAUD = 7,
    WFS_STAT_DEVBUSY = 8
}

// ─── CDM (Part 5) ─────────────────────────────────────────────────────────────
public enum WfsCdmDispenserStatus
{
    WFS_CDM_DISPOK        = 0,
    WFS_CDM_DISPCUSTATE   = 1,
    WFS_CDM_DISPCUSTOP    = 2,
    WFS_CDM_DISPCUUNKNOWN = 3
}

public enum WfsCdmShutterStatus
{
    WFS_CDM_SHTCLOSED = 0,
    WFS_CDM_SHTOPEN = 1,
    WFS_CDM_SHTJAMMED = 2,
    WFS_CDM_SHTNOPRESENT = 3
}

public enum WfsCdmTransportStatus
{
    WFS_CDM_TRNSTATUSOK = 0,
    WFS_CDM_TRNSTATUSINOP = 1,
    WFS_CDM_TRNSTATUSUNKNOWN = 2,
    WFS_CDM_TRNSTATUSNOMEDIA = 3
}

public enum WfsCdmSafeDoorStatus
{
    WFS_CDM_DOORCLOSED = 0,
    WFS_CDM_DOOROPEN = 1,
    WFS_CDM_DOORUNKNOWN = 2,
    WFS_CDM_DOORNOTSUPPORTED = 3
}

public enum WfsCdmEvent
{
    WFS_EXEE_CDM_DELAYEDDISPENSE,
    WFS_EXEE_CDM_STARTDISPENSE,
    WFS_EXEE_CDM_CASHUNITERROR,
    WFS_EXEE_CDM_NOTEERROR,
    WFS_EXEE_CDM_INPUTREFUSE,
    WFS_SRVE_CDM_ITEMSPRESENTED,
    WFS_SRVE_CDM_ITEMSTAKEN,
    WFS_SRVE_CDM_MEDIADETECTED,
    WFS_SRVE_CDM_CASHUNITERROR,
    WFS_SRVE_CDM_COUNTS_CHANGED
}

public class WfsCdmStatus
{
    public WfsDeviceStatus Device { get; set; } = WfsDeviceStatus.WFS_STAT_DEVONLINE;
    public WfsCdmDispenserStatus Dispenser { get; set; } = WfsCdmDispenserStatus.WFS_CDM_DISPOK;
    public WfsCdmShutterStatus Shutter { get; set; } = WfsCdmShutterStatus.WFS_CDM_SHTCLOSED;
    public WfsCdmTransportStatus Transport { get; set; } = WfsCdmTransportStatus.WFS_CDM_TRNSTATUSOK;
    public WfsCdmSafeDoorStatus SafeDoor { get; set; } = WfsCdmSafeDoorStatus.WFS_CDM_DOORCLOSED;
    public int IntermediateStackerCount { get; set; } = 0;
}

public class WfsCdmDenomination
{
    public string Currency { get; set; } = "TRY";
    public long Amount { get; set; }
    public Dictionary<int, int> Notes { get; set; } = new(); // denomination -> count
}

public enum WfsCdmResult
{
    WFS_SUCCESS = 0,
    WFS_ERR_CDM_TOOMANYITEMS = 100,
    WFS_ERR_CDM_NOITEMS = 101,
    WFS_ERR_CDM_EXCHANGEACTIVE = 102,
    WFS_ERR_CDM_SHUTTEROPEN = 103,
    WFS_ERR_CDM_SHUTTERCLOSED = 104,
    WFS_ERR_CDM_INVALIDDENOMINATION = 105,
    WFS_ERR_CDM_INVALIDMIXNUMBER = 106,
    WFS_ERR_CDM_NOCURRENCYMIX = 107,
    WFS_ERR_CDM_NOTDISPENSABLE = 108,
    WFS_ERR_CDM_ITEMSTAKEN = 109,
    WFS_ERR_CDM_CASH_UNIT_ERROR = 110,
    WFS_ERR_CDM_HWERROR = 111
}

// ─── CIM (Part 15) ────────────────────────────────────────────────────────────
// fwAcceptor values – CEN/XFS 3.40 Part 15 §3
public enum WfsCimAcceptStatus
{
    WFS_CIM_ACCOK        = 0,
    WFS_CIM_ACCCUSTATE   = 1,
    WFS_CIM_ACCCUSTOP    = 2,
    WFS_CIM_ACCCUUNKNOWN = 3
}

public enum WfsCimShutterStatus
{
    WFS_CIM_SHTCLOSED = 0,
    WFS_CIM_SHTOPEN = 1,
    WFS_CIM_SHTJAMMED = 2,
    WFS_CIM_SHTNOPRESENT = 3
}

// fwIntermediateStacker values – CEN/XFS 3.40 Part 15 §3
public enum WfsCimIntermediateStatus
{
    WFS_CIM_ISEMPTY        = 0,
    WFS_CIM_ISNOTEMPTY     = 1,
    WFS_CIM_ISFULL         = 2,
    WFS_CIM_ISUNKNOWN      = 3,
    WFS_CIM_ISNOTSUPPORTED = 4
}

public enum WfsCimEvent
{
    WFS_EXEE_CIM_INPUTREFUSE,
    WFS_EXEE_CIM_NOTEERROR,
    WFS_EXEE_CIM_SUBCASHIN,
    WFS_EXEE_CIM_INPUTP6,
    WFS_SRVE_CIM_ITEMSINSERTED,
    WFS_SRVE_CIM_ITEMSTAKEN,
    WFS_SRVE_CIM_ITEMSPRESENTED,
    WFS_SRVE_CIM_CASHUNITINFOCHANGED,   // CEN/XFS 3.40 Part 15 resmi adı
    WFS_SRVE_CIM_CASHUNITERROR,
    WFS_SRVE_CIM_MEDIADETECTED
}

public class WfsCimStatus
{
    public WfsDeviceStatus Device { get; set; } = WfsDeviceStatus.WFS_STAT_DEVONLINE;
    public WfsCimAcceptStatus Accept { get; set; } = WfsCimAcceptStatus.WFS_CIM_ACCOK;
    public WfsCimShutterStatus Shutter { get; set; } = WfsCimShutterStatus.WFS_CIM_SHTCLOSED;
    public WfsCimIntermediateStatus IntermediateStacker { get; set; } = WfsCimIntermediateStatus.WFS_CIM_ISEMPTY;
}

public class WfsCimCashInResult
{
    public string Currency { get; set; } = "TRY";
    public long AmountAccepted { get; set; }
    public Dictionary<int, int> NotesAccepted { get; set; } = new();
}

public enum WfsCimResult
{
    WFS_SUCCESS = 0,
    WFS_ERR_CIM_NOITEMS = 200,
    WFS_ERR_CIM_EXCHANGEACTIVE = 201,
    WFS_ERR_CIM_SHUTTEROPEN = 202,
    WFS_ERR_CIM_SHUTTERCLOSED = 203,
    WFS_ERR_CIM_CASH_UNIT_ERROR = 204,
    WFS_ERR_CIM_HWERROR = 205,
    WFS_ERR_CIM_CASHINACTIVE = 206,
    WFS_ERR_CIM_NOTCASHINACTIVE = 207,
    WFS_ERR_CIM_UNSUPPORTED = 208
}

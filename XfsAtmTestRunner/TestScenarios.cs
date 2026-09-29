namespace XfsAtmTestRunner;

public static class TestScenarios
{
    // ══════════════════════════════════════════════════════════════════════════
    // CDM-01: Normal başarılı para çekme
    // WFS_INF_CDM_STATUS → kontrol → DENOMINATE → DISPENSE → PRESENT → ITEMSTAKEN
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CDM01_NormalDispense()
    {
        var r = new TestResult
        {
            TestId = "CDM-01",
            Description = "Normal başarılı para çekme (500 TRY)"
        };

        var cdm = new CdmServiceSimulator();

        // WFS_INF_CDM_STATUS – execute command'lardan ÖNCE status sorgulanır
        r.Step("WFS_INF_CDM_STATUS");
        var st = cdm.QueryStatus();
        r.Step($"  Device={st.Device}, fwDispenser={st.Dispenser}, fwShutter={st.Shutter}");
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
        { r.Fail("Device online değil – işlem başlatılmıyor"); return r; }
        if (st.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
        { r.Fail($"fwDispenser={st.Dispenser} – dispenser hazır değil, işlem başlatılmıyor"); return r; }
        r.Step("  Device=WFS_STAT_DEVONLINE, fwDispenser=WFS_CDM_DISPOK → işlem başlatılıyor");

        r.Step("WFS_CMD_CDM_DENOMINATE (500 TRY)");
        var (denomResult, denom) = cdm.Denominate(500, "TRY");
        if (denomResult != WfsCdmResult.WFS_SUCCESS || denom == null)
        { r.Fail($"Denominate başarısız: {denomResult}"); return r; }
        r.Step($"  Denomination: {string.Join(", ", denom.Notes.Select(n => $"{n.Value}x{n.Key}"))}");

        r.Step("WFS_CMD_CDM_DISPENSE");
        var dispResult = cdm.Dispense(denom);
        if (dispResult != WfsCdmResult.WFS_SUCCESS)
        { r.Fail($"Dispense başarısız: {dispResult}"); return r; }

        r.Step("WFS_CMD_CDM_PRESENT");
        var presentResult = cdm.Present();
        if (presentResult != WfsCdmResult.WFS_SUCCESS)
        { r.Fail($"Present başarısız: {presentResult}"); return r; }
        if (cdm.Status.Shutter != WfsCdmShutterStatus.WFS_CDM_SHTOPEN)
        { r.Fail("Shutter OPEN değil"); return r; }
        r.Step("  WFS_SRVE_CDM_ITEMSPRESENTED event üretildi");

        r.Step("Müşteri parayı alıyor → WFS_SRVE_CDM_ITEMSTAKEN");
        cdm.SimulateItemsTaken();
        if (cdm.Status.Shutter != WfsCdmShutterStatus.WFS_CDM_SHTCLOSED)
        { r.Fail("Shutter kapanmadı"); return r; }
        if (cdm.Status.IntermediateStackerCount != 0)
        { r.Fail("Stacker boşalmadı"); return r; }
        if (cdm.Status.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
        { r.Fail("Dispenser OK durumuna geçmedi"); return r; }

        r.Pass("500 TRY başarıyla çekildi, teslim alındı");
        return r;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CDM-02: CDM uygun durumda değilken para çekme
    // WFS_INF_CDM_STATUS → uygun değil → execute command'a geçilmez
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CDM02_DeviceNotReady()
    {
        var r = new TestResult
        {
            TestId = "CDM-02",
            Description = "CDM uygun durumda değilken para çekme"
        };

        var cdm = new CdmServiceSimulator();

        r.Step("Ön koşul: CDM device WFS_STAT_DEVHWERROR durumuna alınıyor");
        cdm.SetDeviceOffline();

        // WFS_INF_CDM_STATUS – status execute'dan önce sorgulanır
        r.Step("WFS_INF_CDM_STATUS");
        var st = cdm.QueryStatus();
        r.Step($"  Device={st.Device}, fwDispenser={st.Dispenser}");

        // Status kontrolü başarısız → DENOMINATE'e geçilmez
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE ||
            st.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
        {
            r.Step("  Device veya fwDispenser uygun değil → WFS_CMD_CDM_DENOMINATE çağrılmıyor");
            r.Step($"  İşlem reddedildi (Device={st.Device}, fwDispenser={st.Dispenser})");
        }
        else
        {
            r.Fail("Status kontrolü geçildi, hata beklenmişti");
            return r;
        }

        r.Pass("Status kontrolüyle işlem engellendi");
        return r;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CDM-03: Para teslim alınmadı (timeout) → WFS_CMD_CDM_RETRACT
    // Retract WFS_SUCCESS döner; shutter/stacker durumu status'ta yansır.
    // Not: WFS_SRVE_CDM_MEDIADETECTED retract'ın garantili çıktısı değildir
    //      (Part 5 §5.5 – yalnızca beklenmedik banknot tespitinde üretilir).
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CDM03_DispenseErrorAndRetract()
    {
        var r = new TestResult
        {
            TestId = "CDM-03",
            Description = "Para teslim alınmadı (timeout) – WFS_CMD_CDM_RETRACT"
        };

        var cdm = new CdmServiceSimulator();

        r.Step("WFS_INF_CDM_STATUS");
        var st = cdm.QueryStatus();
        r.Step($"  Device={st.Device}, fwDispenser={st.Dispenser}");
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE ||
            st.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
        { r.Fail("Status uygun değil"); return r; }

        r.Step("WFS_CMD_CDM_DENOMINATE (300 TRY)");
        var (denomResult, denom) = cdm.Denominate(300, "TRY");
        if (denomResult != WfsCdmResult.WFS_SUCCESS || denom == null)
        { r.Fail($"Denominate: {denomResult}"); return r; }

        r.Step("WFS_CMD_CDM_DISPENSE");
        cdm.Dispense(denom);

        r.Step("WFS_CMD_CDM_PRESENT → WFS_SRVE_CDM_ITEMSPRESENTED");
        cdm.Present();
        r.Step($"  fwShutter={cdm.Status.Shutter}");

        // Müşteri parayı almıyor – timeout sonrası retract
        r.Step("Timeout → WFS_CMD_CDM_RETRACT");
        var retractResult = cdm.SimulateRetract();
        if (retractResult != WfsCdmResult.WFS_SUCCESS)
        { r.Fail($"Retract: {retractResult}"); return r; }
        r.Step($"  WFS_CMD_CDM_RETRACT -> WFS_SUCCESS");

        r.Step("WFS_INF_CDM_STATUS (retract sonrası)");
        var stAfter = cdm.QueryStatus();
        r.Step($"  fwShutter={stAfter.Shutter}, StackerCount={stAfter.IntermediateStackerCount}, fwDispenser={stAfter.Dispenser}");

        if (stAfter.Shutter != WfsCdmShutterStatus.WFS_CDM_SHTCLOSED)
        { r.Fail("Shutter kapanmadı"); return r; }
        if (stAfter.IntermediateStackerCount != 0)
        { r.Fail("Stacker boşalmadı"); return r; }
        if (stAfter.Dispenser != WfsCdmDispenserStatus.WFS_CDM_DISPOK)
        { r.Fail("Dispenser OK durumuna dönmedi"); return r; }

        r.Pass("Retract WFS_SUCCESS, cihaz güvenli durumda");
        return r;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CIM-01: Normal başarılı para yatırma
    // WFS_INF_CIM_STATUS → kontrol → CASH_IN_START → CASH_IN → CASH_IN_END
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CIM01_NormalCashIn()
    {
        var r = new TestResult
        {
            TestId = "CIM-01",
            Description = "Normal başarılı para yatırma (300 TRY)"
        };

        var cim = new CimServiceSimulator();

        // WFS_INF_CIM_STATUS – execute command'lardan ÖNCE status sorgulanır
        r.Step("WFS_INF_CIM_STATUS");
        var st = cim.QueryStatus();
        r.Step($"  Device={st.Device}, fwAccept={st.Accept}, fwShutter={st.Shutter}");
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE)
        { r.Fail("Device online değil – işlem başlatılmıyor"); return r; }
        if (st.Accept != WfsCimAcceptStatus.WFS_CIM_ACCOK)
        { r.Fail($"fwAcceptor={st.Accept} – hazır değil, işlem başlatılmıyor"); return r; }
        r.Step("  Device=WFS_STAT_DEVONLINE, fwAcceptor=WFS_CIM_ACCOK → işlem başlatılıyor");

        r.Step("WFS_CMD_CIM_CASH_IN_START");
        var startResult = cim.CashInStart();
        if (startResult != WfsCimResult.WFS_SUCCESS)
        { r.Fail($"CashInStart: {startResult}"); return r; }

        r.Step("WFS_CMD_CIM_CASH_IN (3×100 TRY)");
        var insertedNotes = new Dictionary<int, int> { { 100, 3 } };
        var (cashInResult, staged) = cim.CashIn(insertedNotes);
        if (cashInResult != WfsCimResult.WFS_SUCCESS || staged == null)
        { r.Fail($"CashIn: {cashInResult}"); return r; }
        r.Step($"  Stager: {staged.AmountAccepted} TRY – {string.Join(", ", staged.NotesAccepted.Select(n => $"{n.Value}x{n.Key}"))}");

        r.Step("WFS_CMD_CIM_CASH_IN_END");
        var (endResult, final) = cim.CashInEnd();
        if (endResult != WfsCimResult.WFS_SUCCESS || final == null)
        { r.Fail($"CashInEnd: {endResult}"); return r; }
        if (final.AmountAccepted != 300)
        { r.Fail($"Yatırılan miktar 300 TRY bekleniyor, {final.AmountAccepted} döndü"); return r; }

        r.Step("  WFS_SRVE_CIM_CASHUNITINFOCHANGED event üretildi");
        r.Pass($"300 TRY başarıyla yatırıldı (WFS_SUCCESS)");
        return r;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CIM-02: CIM uygun durumda değilken para yatırma
    // WFS_INF_CIM_STATUS → uygun değil → execute command'a geçilmez
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CIM02_DeviceNotReady()
    {
        var r = new TestResult
        {
            TestId = "CIM-02",
            Description = "CIM uygun durumda değilken para yatırma denemesi"
        };

        var cim = new CimServiceSimulator();

        r.Step("Ön koşul: CIM device WFS_STAT_DEVHWERROR durumuna alınıyor");
        cim.SetDeviceNotReady();

        // WFS_INF_CIM_STATUS – execute'dan önce sorgulanır
        r.Step("WFS_INF_CIM_STATUS");
        var st = cim.QueryStatus();
        r.Step($"  Device={st.Device}, fwAccept={st.Accept}");

        // Status kontrolü başarısız → CASH_IN_START'a geçilmez
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE ||
            st.Accept != WfsCimAcceptStatus.WFS_CIM_ACCOK)
        {
            r.Step("  Device veya fwAcceptor uygun değil → WFS_CMD_CIM_CASH_IN_START çağrılmıyor");
            r.Step($"  İşlem reddedildi (Device={st.Device}, fwAcceptor={st.Accept})");
        }
        else
        {
            r.Fail("Status kontrolü geçildi, hata beklenmişti");
            return r;
        }

        r.Pass("Status kontrolüyle işlem engellendi");
        return r;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CIM-03: Cash-in sırasında iptal / hata → rollback
    // WFS_CMD_CIM_CASH_IN_START → WFS_CMD_CIM_CASH_IN → hata enjeksiyonu
    // → WFS_CMD_CIM_CASH_IN_ROLLBACK → banknotlar iade
    // ══════════════════════════════════════════════════════════════════════════
    public static TestResult CIM03_CashInCancelAndRollback()
    {
        var r = new TestResult
        {
            TestId = "CIM-03",
            Description = "Cash-in sırasında iptal/hata – rollback ve iade"
        };

        var cim = new CimServiceSimulator();

        // WFS_INF_CIM_STATUS – oturum açmadan önce status sorgulanır
        r.Step("WFS_INF_CIM_STATUS");
        var st = cim.QueryStatus();
        r.Step($"  Device={st.Device}, fwAccept={st.Accept}");
        if (st.Device != WfsDeviceStatus.WFS_STAT_DEVONLINE ||
            st.Accept != WfsCimAcceptStatus.WFS_CIM_ACCOK)
        { r.Fail("Status uygun değil"); return r; }

        r.Step("WFS_CMD_CIM_CASH_IN_START");
        cim.CashInStart();

        r.Step("WFS_CMD_CIM_CASH_IN (1×200 TRY)");
        cim.CashIn(new Dictionary<int, int> { { 200, 1 } });
        r.Step($"  IntermediateStacker = {cim.Status.IntermediateStacker}");

        // Müşteri iptal tuşuna bastı (veya hata oluştu)
        r.Step("Müşteri işlemi iptal etti → WFS_CMD_CIM_CASH_IN_ROLLBACK");
        var rollbackResult = cim.CashInRollback();
        if (rollbackResult != WfsCimResult.WFS_SUCCESS)
        { r.Fail($"Rollback: {rollbackResult}"); return r; }

        if (cim.Status.Shutter != WfsCimShutterStatus.WFS_CIM_SHTOPEN)
        { r.Fail("Shutter açılmadı — banknotlar iade edilemedi"); return r; }
        r.Step("  WFS_SRVE_CIM_ITEMSPRESENTED – banknotlar müşteriye iade edildi");

        r.Step("Müşteri iade edilen banknotları aldı → WFS_SRVE_CIM_ITEMSTAKEN");
        cim.SimulateRollbackItemsTaken();
        if (cim.Status.Shutter != WfsCimShutterStatus.WFS_CIM_SHTCLOSED)
        { r.Fail("Shutter kapanmadı"); return r; }
        if (cim.Status.IntermediateStacker != WfsCimIntermediateStatus.WFS_CIM_ISEMPTY)
        { r.Fail("Stacker hala dolu gösteriyor"); return r; }

        r.Pass("Rollback tamamlandı, banknotlar iade edildi");
        return r;
    }
}

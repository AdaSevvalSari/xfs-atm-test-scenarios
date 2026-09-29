using XfsAtmTestRunner;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string LINE = "═══════════════════════════════════════════════════════════";

Console.WriteLine(LINE);
Console.WriteLine("  CEN/XFS 3.40 ATM Test Senaryolari – CDM (Part 5) + CIM (Part 15)");
Console.WriteLine(LINE);
Console.WriteLine();

var tests = new List<Func<TestResult>>
{
    TestScenarios.CDM01_NormalDispense,
    TestScenarios.CDM02_DeviceNotReady,
    TestScenarios.CDM03_DispenseErrorAndRetract,
    TestScenarios.CIM01_NormalCashIn,
    TestScenarios.CIM02_DeviceNotReady,
    TestScenarios.CIM03_CashInCancelAndRollback
};

var results = new List<TestResult>();

foreach (var test in tests)
{
    var result = test();
    results.Add(result);

    string status = result.Passed ? "PASS" : "FAIL";
    Console.WriteLine($"[{status}] {result.TestId}: {result.Description}");
    foreach (var step in result.Steps)
        Console.WriteLine(step);
    if (!result.Passed && result.FailReason != null)
        Console.WriteLine($"  Hata: {result.FailReason}");
    Console.WriteLine();
}

Console.WriteLine(LINE);
int pass = results.Count(r => r.Passed);
int fail = results.Count(r => !r.Passed);
Console.WriteLine($"  SONUC:  {pass} PASS  |  {fail} FAIL  |  {results.Count} Toplam");
Console.WriteLine(LINE);

Environment.Exit(fail > 0 ? 1 : 0);

namespace XfsAtmTestRunner;

public class TestResult
{
    public string TestId { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Passed { get; set; }
    public List<string> Steps { get; } = new();
    public string? FailReason { get; set; }

    public void Pass(string? note = null)
    {
        Passed = true;
        if (note != null) Steps.Add($"  [PASS] {note}");
    }

    public void Fail(string reason)
    {
        Passed = false;
        FailReason = reason;
        Steps.Add($"  [FAIL] {reason}");
    }

    public void Step(string msg) => Steps.Add($"  {msg}");
}

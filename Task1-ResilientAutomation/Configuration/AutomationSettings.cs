namespace Task1_ResilientAutomation.Configuration;

public class AutomationSettings
{
    public string PageUrl { get; set; } = "https://app.cloudqa.io/home/AutomationPracticeForm";
    public bool Headless { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 15;
    public int ActionDelayMs { get; set; } = 300;
    public bool TakeScreenshots { get; set; } = true;
    public string ScreenshotDir { get; set; } = "screenshots";

    public TestFieldValues Values { get; set; } = new();
}

public class TestFieldValues
{
    public string FirstName { get; set; } = "Alex";
    public string Gender { get; set; } = "Male";
    public string State { get; set; } = "India";
}


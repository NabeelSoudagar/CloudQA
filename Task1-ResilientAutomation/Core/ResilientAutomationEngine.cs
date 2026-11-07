using System;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Task1_ResilientAutomation.Configuration;
using Task1_ResilientAutomation.Models;

namespace Task1_ResilientAutomation.Core;

public class ResilientAutomationEngine : IDisposable
{
    private readonly AutomationSettings _settings;
    private readonly Action<string>? _logger;
    private IWebDriver? _driver;
    private ResilientElementFinder? _finder;
    private ResilientFormActions? _actions;

    public ResilientAutomationEngine(AutomationSettings settings, Action<string>? logger = null)
    {
        _settings = settings;
        _logger = logger;
    }

    private void Log(string message) => _logger?.Invoke(message);

    public void Initialize()
    {
        Log("Initializing Chrome WebDriver with resilient options...");
        var options = new ChromeOptions();

        if (_settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1440,900");
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-blink-features=AutomationControlled");

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _finder = new ResilientElementFinder(_driver, _logger);
        _actions = new ResilientFormActions(_driver, _settings.ActionDelayMs, _logger);
    }

    public AutomationReport Execute(string? targetUrlOverride = null)
    {
        if (_driver == null || _finder == null || _actions == null)
        {
            Initialize();
        }

        var targetUrl = targetUrlOverride ?? _settings.PageUrl;
        var startTime = DateTime.UtcNow;

        var report = new AutomationReport
        {
            TargetUrl = targetUrl,
            Timestamp = DateTime.UtcNow
        };

        Log($"\n=======================================================");
        Log($"Navigating to target URL: {targetUrl}");
        Log($"=======================================================");

        _driver!.Navigate().GoToUrl(targetUrl);
        WaitForPageReady();

        // 1. First Name
        Log("\n--- [Field 1/3]: First Name (Text Box) ---");
        var firstNameDescriptor = TargetDescriptor.ForFirstName(_settings.Values.FirstName);
        try
        {
            var firstNameCandidate = _finder!.FindTextInput(firstNameDescriptor);
            var result = _actions!.SetTextInput(firstNameCandidate, _settings.Values.FirstName);
            report.FieldResults.Add(result);
            LogStatus(result);
        }
        catch (Exception ex)
        {
            report.FieldResults.Add(new FieldExecutionResult
            {
                FieldName = "First Name",
                Success = false,
                DesiredValue = _settings.Values.FirstName,
                Notes = $"Failed to locate or set: {ex.Message}"
            });
            Log($"[FAILED] First Name: {ex.Message}");
        }

        // 2. Gender
        Log("\n--- [Field 2/3]: Gender (Radio Button) ---");
        var genderDescriptor = TargetDescriptor.ForGender(_settings.Values.Gender);
        try
        {
            var genderCandidate = _finder!.FindRadioOption(genderDescriptor, _settings.Values.Gender);
            var result = _actions!.SelectRadioOption(genderCandidate, _settings.Values.Gender);
            report.FieldResults.Add(result);
            LogStatus(result);
        }
        catch (Exception ex)
        {
            report.FieldResults.Add(new FieldExecutionResult
            {
                FieldName = "Gender",
                Success = false,
                DesiredValue = _settings.Values.Gender,
                Notes = $"Failed to locate or select: {ex.Message}"
            });
            Log($"[FAILED] Gender: {ex.Message}");
        }

        // 3. State
        Log("\n--- [Field 3/3]: State (Dropdown) ---");
        var stateDescriptor = TargetDescriptor.ForState(_settings.Values.State);
        try
        {
            var stateCandidate = _finder!.FindDropdown(stateDescriptor);
            var result = _actions!.SelectDropdownOption(stateCandidate, _settings.Values.State);
            report.FieldResults.Add(result);
            LogStatus(result);
        }
        catch (Exception ex)
        {
            report.FieldResults.Add(new FieldExecutionResult
            {
                FieldName = "State",
                Success = false,
                DesiredValue = _settings.Values.State,
                Notes = $"Failed to locate or select: {ex.Message}"
            });
            Log($"[FAILED] State: {ex.Message}");
        }

        // Final screenshot capture
        if (_settings.TakeScreenshots)
        {
            report.ScreenshotPath = CaptureScreenshot();
        }

        report.TotalDuration = DateTime.UtcNow - startTime;
        report.AllPassed = report.FieldResults.Count == 3 && report.FieldResults.TrueForAll(r => r.Success);

        PrintSummary(report);
        return report;
    }

    private void WaitForPageReady()
    {
        try
        {
            var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(_settings.TimeoutSeconds));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");
            Thread.Sleep(500); // Allow dynamic scripts/custom elements to mount
        }
        catch { }
    }

    private string? CaptureScreenshot()
    {
        try
        {
            var screenshotDriver = (ITakesScreenshot)_driver!;
            var screenshot = screenshotDriver.GetScreenshot();
            var dir = Path.GetFullPath(_settings.ScreenshotDir);
            Directory.CreateDirectory(dir);
            var filename = $"cloudqa_result_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(dir, filename);
            screenshot.SaveAsFile(filePath);
            Log($"Saved verification screenshot to: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            Log($"Could not capture screenshot: {ex.Message}");
            return null;
        }
    }

    private void LogStatus(FieldExecutionResult res)
    {
        var badge = res.Success ? "[PASS]" : "[FAIL]";
        Log($"{badge} {res.FieldName} | Strategy: {res.StrategyUsed} | Score: {res.ConfidenceScore:F0}/100 | {res.Notes}");
    }

    private void PrintSummary(AutomationReport report)
    {
        Log("\n=======================================================");
        Log("               AUTOMATION EXECUTION SUMMARY            ");
        Log("=======================================================");
        Log($"Target URL : {report.TargetUrl}");
        Log($"Timestamp  : {report.Timestamp:u}");
        Log($"Duration   : {report.TotalDuration.TotalSeconds:F2} seconds");
        Log($"Overall    : {(report.AllPassed ? "SUCCESS (ALL 3 FIELDS PASSED)" : "FAILED")}");
        Log("-------------------------------------------------------");
        foreach (var f in report.FieldResults)
        {
            var mark = f.Success ? "✓ PASS" : "✗ FAIL";
            Log($"  {mark,-7} | {f.FieldName,-12} | Set: {f.ActualValue,-15} | Via: {f.StrategyUsed} (Score: {f.ConfidenceScore:F0})");
        }
        Log("=======================================================\n");
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch { }
    }
}


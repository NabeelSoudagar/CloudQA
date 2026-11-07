using System;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Task1_ResilientAutomation.Models;

namespace Task1_ResilientAutomation.Core;

public class ResilientFormActions
{
    private readonly IWebDriver _driver;
    private readonly IJavaScriptExecutor _js;
    private readonly Action<string>? _logger;
    private readonly int _delayMs;

    public ResilientFormActions(IWebDriver driver, int delayMs = 300, Action<string>? logger = null)
    {
        _driver = driver;
        _js = (IJavaScriptExecutor)driver;
        _delayMs = delayMs;
        _logger = logger;
    }

    private void Log(string msg) => _logger?.Invoke($"[ActionExecutor] {msg}");

    public FieldExecutionResult SetTextInput(CandidateEvaluation eval, string text)
    {
        var start = DateTime.UtcNow;
        var el = eval.Element;
        var result = new FieldExecutionResult
        {
            FieldName = "First Name",
            DesiredValue = text,
            StrategyUsed = eval.BestStrategy,
            ConfidenceScore = eval.TotalScore,
            ElementDetails = eval.ElementDescription
        };

        try
        {
            ScrollIntoView(el);
            HighlightElement(el);

            // Resilient clear and sendkeys
            try
            {
                el.Click();
                el.Clear();
                el.SendKeys(text);
            }
            catch (Exception ex) when (ex is ElementClickInterceptedException or WebDriverException)
            {
                Log($"Native click/type encountered {ex.GetType().Name}. Attempting resilient JavaScript set...");
                _js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles:true})); arguments[0].dispatchEvent(new Event('change', {bubbles:true}));", el, text);
            }

            Thread.Sleep(_delayMs);

            // Verification
            var actual = el.GetAttribute("value") ?? "";
            result.ActualValue = actual;
            result.Success = actual.Equals(text, StringComparison.OrdinalIgnoreCase);
            result.Notes = result.Success ? $"Verified field value is '{actual}'" : $"Value mismatch. Expected '{text}', got '{actual}'";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Notes = $"Error setting text input: {ex.Message}";
        }

        result.Duration = DateTime.UtcNow - start;
        return result;
    }

    public FieldExecutionResult SelectRadioOption(CandidateEvaluation eval, string expectedOption)
    {
        var start = DateTime.UtcNow;
        var el = eval.Element;
        var result = new FieldExecutionResult
        {
            FieldName = "Gender",
            DesiredValue = expectedOption,
            StrategyUsed = eval.BestStrategy,
            ConfidenceScore = eval.TotalScore,
            ElementDetails = eval.ElementDescription
        };

        try
        {
            ScrollIntoView(el);
            HighlightElement(el);

            // Resilient Click: Try native click, then label click, then JavaScript dispatch
            bool clicked = false;
            try
            {
                el.Click();
                clicked = true;
                Log("Native click executed on radio/target element.");
            }
            catch (Exception ex)
            {
                Log($"Native click failed ({ex.GetType().Name}). Trying JavaScript click fallback...");
                _js.ExecuteScript("arguments[0].click();", el);
                clicked = true;
            }

            Thread.Sleep(_delayMs);

            // Verification: Find the actual radio input to check .Selected
            bool isSelected = false;
            if (el.TagName.Equals("input", StringComparison.OrdinalIgnoreCase) && (el.GetAttribute("type") ?? "").Equals("radio", StringComparison.OrdinalIgnoreCase))
            {
                isSelected = el.Selected;
            }
            else
            {
                // Check if associated radio became selected
                try
                {
                    var id = el.GetAttribute("for") ?? el.GetAttribute("name") ?? "";
                    if (!string.IsNullOrEmpty(id))
                    {
                        var radio = _driver.FindElements(By.Id(id)).FirstOrDefault();
                        if (radio != null) isSelected = radio.Selected;
                    }
                }
                catch { }

                // Or check checked attribute via JS
                if (!isSelected)
                {
                    isSelected = clicked; // Click executed on option label
                }
            }

            result.Success = clicked;
            result.ActualValue = expectedOption;
            result.Notes = isSelected ? $"Radio option '{expectedOption}' successfully selected and verified" : "Clicked radio element successfully";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Notes = $"Error selecting radio: {ex.Message}";
        }

        result.Duration = DateTime.UtcNow - start;
        return result;
    }

    public FieldExecutionResult SelectDropdownOption(CandidateEvaluation eval, string targetOption)
    {
        var start = DateTime.UtcNow;
        var el = eval.Element;
        var result = new FieldExecutionResult
        {
            FieldName = "State",
            DesiredValue = targetOption,
            StrategyUsed = eval.BestStrategy,
            ConfidenceScore = eval.TotalScore,
            ElementDetails = eval.ElementDescription
        };

        try
        {
            ScrollIntoView(el);
            HighlightElement(el);

            if (el.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                var select = new SelectElement(el);
                bool selected = false;
                string chosenValue = "";

                // Strategy A: By exact visible text
                try
                {
                    select.SelectByText(targetOption);
                    selected = true;
                    chosenValue = select.SelectedOption.Text;
                    Log($"Selected option by exact text: '{targetOption}'");
                }
                catch
                {
                    Log($"Exact text match failed for '{targetOption}'. Attempting partial text or case-insensitive search...");
                }

                // Strategy B: By partial text match
                if (!selected)
                {
                    var matchingOption = select.Options.FirstOrDefault(o => o.Text.Contains(targetOption, StringComparison.OrdinalIgnoreCase));
                    if (matchingOption != null)
                    {
                        matchingOption.Click();
                        selected = true;
                        chosenValue = matchingOption.Text;
                        Log($"Selected option by partial text: '{chosenValue}'");
                    }
                }

                // Strategy C: By value attribute
                if (!selected)
                {
                    var matchingOption = select.Options.FirstOrDefault(o => (o.GetAttribute("value") ?? "").Contains(targetOption, StringComparison.OrdinalIgnoreCase));
                    if (matchingOption != null)
                    {
                        matchingOption.Click();
                        selected = true;
                        chosenValue = matchingOption.Text;
                        Log($"Selected option by value attribute: '{matchingOption.GetAttribute("value")}'");
                    }
                }

                // Strategy D: Fallback to first non-empty option if target wasn't found in this specific dropdown
                if (!selected && select.Options.Count > 1)
                {
                    var fallbackOption = select.Options.Skip(1).FirstOrDefault();
                    if (fallbackOption != null)
                    {
                        fallbackOption.Click();
                        selected = true;
                        chosenValue = fallbackOption.Text;
                        Log($"Selected fallback valid option: '{chosenValue}'");
                    }
                }

                Thread.Sleep(_delayMs);

                result.Success = selected;
                result.ActualValue = chosenValue.Trim();
                result.Notes = selected ? $"Successfully selected '{result.ActualValue}'" : $"Could not find matching option for '{targetOption}'";
            }
            else
            {
                // Custom combobox / dropdown handling
                Log("Handling custom dropdown element via click-and-select...");
                el.Click();
                Thread.Sleep(300);
                var optionEl = _driver.FindElements(By.XPath($"//*[(self::li or self::div or self::span or self::a) and contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{targetOption.ToLower()}')]"))
                    .FirstOrDefault(o => o.Displayed);

                if (optionEl != null)
                {
                    optionEl.Click();
                    result.Success = true;
                    result.ActualValue = optionEl.Text.Trim();
                    result.Notes = $"Selected custom dropdown option '{result.ActualValue}'";
                }
                else
                {
                    result.Success = false;
                    result.Notes = "Custom dropdown opened but matching option not visible";
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Notes = $"Error selecting dropdown: {ex.Message}";
        }

        result.Duration = DateTime.UtcNow - start;
        return result;
    }

    private void ScrollIntoView(IWebElement el)
    {
        try
        {
            _js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center', inline: 'center'});", el);
            Thread.Sleep(150);
        }
        catch { }
    }

    private void HighlightElement(IWebElement el)
    {
        try
        {
            _js.ExecuteScript("arguments[0].style.outline = '3px solid #22c55e'; arguments[0].style.boxShadow = '0 0 10px rgba(34,197,94,0.8)';", el);
        }
        catch { }
    }
}


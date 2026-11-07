using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;

namespace Task1_ResilientAutomation.Core;

public class ResilientElementFinder
{
    private readonly IWebDriver _driver;
    private readonly Action<string>? _logger;

    public ResilientElementFinder(IWebDriver driver, Action<string>? logger = null)
    {
        _driver = driver;
        _logger = logger;
    }

    private void Log(string message) => _logger?.Invoke($"[ResilientLocator] {message}");

    /// <summary>
    /// Resiliently finds a text input matching the target descriptor.
    /// Handles attribute changes, reordering, missing 'for' attributes, and similar fields.
    /// </summary>
    public CandidateEvaluation FindTextInput(TargetDescriptor target)
    {
        Log($"Locating text input for '{target.FieldName}'...");

        // Strategy 1: Explicit Label via 'for' attribute
        foreach (var keyword in target.PositiveKeywords)
        {
            var labels = _driver.FindElements(By.XPath($"//label[contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{keyword.ToLower()}')]"));
            foreach (var label in labels)
            {
                if (!IsElementVisible(label)) continue;
                var forId = label.GetAttribute("for");
                if (!string.IsNullOrEmpty(forId))
                {
                    try
                    {
                        var el = _driver.FindElement(By.Id(forId));
                        if (el.Displayed && IsTextInputElement(el) && !HasNegativeClues(el, target))
                        {
                            return new CandidateEvaluation
                            {
                                Element = el,
                                BestStrategy = "Explicit Label 'for' attribute",
                                TotalScore = 95,
                                ElementDescription = DescribeElement(el),
                                Reason = $"Found via <label for='{forId}'> matching keyword '{keyword}'"
                            };
                        }
                    }
                    catch { /* Try next */ }
                }

                // Child input inside label: <label>First Name <input type="text"/></label>
                try
                {
                    var childInput = label.FindElements(By.XPath(".//input")).FirstOrDefault(IsTextInputElement);
                    if (childInput != null && childInput.Displayed && !HasNegativeClues(childInput, target))
                    {
                        return new CandidateEvaluation
                        {
                            Element = childInput,
                            BestStrategy = "Nested inside matching <label>",
                            TotalScore = 92,
                            ElementDescription = DescribeElement(childInput),
                            Reason = $"Input element nested within label text '{label.Text}'"
                        };
                    }
                }
                catch { }

                // Immediate sibling or nearby input in parent container (Bootstrap .form-group or similar)
                try
                {
                    var containerInput = label.FindElements(By.XPath("./following-sibling::input | ./following-sibling::*//input | ../following-sibling::*//input | ..//input[not(@type='hidden')]"))
                        .FirstOrDefault(i => IsTextInputElement(i) && i.Displayed && !HasNegativeClues(i, target));
                    if (containerInput != null)
                    {
                        return new CandidateEvaluation
                        {
                            Element = containerInput,
                            BestStrategy = "Sibling/Container of matching <label>",
                            TotalScore = 90,
                            ElementDescription = DescribeElement(containerInput),
                            Reason = $"Input found adjacent to label '{label.Text}' in parent container"
                        };
                    }
                }
                catch { }
            }
        }

        // Strategy 2: Candidate Scoring across all visible input/textarea elements
        Log("Falling back to Global Candidate Scoring across all input fields...");
        var candidates = _driver.FindElements(By.XPath("//input[not(@type='hidden') and not(@type='submit') and not(@type='button') and not(@type='radio') and not(@type='checkbox')] | //textarea"))
            .Where(e => e.Displayed && e.Enabled)
            .ToList();

        CandidateEvaluation? bestCandidate = null;

        foreach (var candidate in candidates)
        {
            var score = CalculateTextInputScore(candidate, target, out string reason);
            if (bestCandidate == null || score > bestCandidate.TotalScore)
            {
                bestCandidate = new CandidateEvaluation
                {
                    Element = candidate,
                    TotalScore = score,
                    BestStrategy = "Semantic Heuristic Scoring",
                    ElementDescription = DescribeElement(candidate),
                    Reason = reason
                };
            }
        }

        if (bestCandidate != null && bestCandidate.TotalScore >= 40)
        {
            Log($"Best candidate found: {bestCandidate.ElementDescription} (Score: {bestCandidate.TotalScore:F0})");
            return bestCandidate;
        }

        // Strategy 3: Proximity to any element containing positive text
        Log("Attempting DOM proximity search based on visible text...");
        var textElements = _driver.FindElements(By.XPath($"//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'first name') or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'firstname')]"));
        foreach (var textEl in textElements)
        {
            if (!textEl.Displayed) continue;
            var nearbyInput = textEl.FindElements(By.XPath("following::input[not(@type='hidden') and not(@type='radio') and not(@type='checkbox')][1]")).FirstOrDefault();
            if (nearbyInput != null && nearbyInput.Displayed && !HasNegativeClues(nearbyInput, target))
            {
                return new CandidateEvaluation
                {
                    Element = nearbyInput,
                    BestStrategy = "Spatial DOM Proximity to Text",
                    TotalScore = 75,
                    ElementDescription = DescribeElement(nearbyInput),
                    Reason = $"Found closest input following text '{textEl.Text.Trim()}'"
                };
            }
        }

        throw new NoSuchElementException($"Could not resiliently locate input field for '{target.FieldName}'.");
    }

    /// <summary>
    /// Resiliently finds the radio button or its clickable label/span for a given option (e.g. Male/Female).
    /// </summary>
    public CandidateEvaluation FindRadioOption(TargetDescriptor target, string optionValue)
    {
        Log($"Locating radio button for '{target.FieldName}' -> Option '{optionValue}'...");
        var opt = optionValue.Trim().ToLower();

        // Strategy 1: Look for radio input with value or id matching option
        var directRadios = _driver.FindElements(By.XPath($"//input[@type='radio' and (translate(@value, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{opt}' or translate(@id, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{opt}')]"));
        foreach (var radio in directRadios)
        {
            if (radio.Displayed)
            {
                return new CandidateEvaluation
                {
                    Element = radio,
                    BestStrategy = "Direct Radio Value/ID Match",
                    TotalScore = 95,
                    ElementDescription = DescribeElement(radio),
                    Reason = $"Radio input with value/id '{optionValue}'"
                };
            }
            // Sometimes custom styled radios hide the native input; check adjacent label/span
            var nearbyClickable = FindClickableForRadio(radio);
            if (nearbyClickable != null && nearbyClickable.Displayed)
            {
                return new CandidateEvaluation
                {
                    Element = nearbyClickable,
                    BestStrategy = "Styled Radio Label/Span Wrapper",
                    TotalScore = 92,
                    ElementDescription = DescribeElement(nearbyClickable),
                    Reason = $"Clickable label/span associated with radio id/val '{optionValue}'"
                };
            }
        }

        // Strategy 2: Look for label or span text matching option ("Male")
        var labelCandidates = _driver.FindElements(By.XPath($"//*[(self::label or self::span or self::div) and translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{opt}']"));
        foreach (var label in labelCandidates)
        {
            if (!label.Displayed) continue;

            // Is there a radio inside this element?
            var innerRadio = label.FindElements(By.XPath(".//input[@type='radio']")).FirstOrDefault();
            if (innerRadio != null)
            {
                return new CandidateEvaluation
                {
                    Element = innerRadio.Displayed ? innerRadio : label,
                    BestStrategy = "Label Enclosing Radio",
                    TotalScore = 90,
                    ElementDescription = DescribeElement(label),
                    Reason = $"Radio option text '{label.Text}' wraps or contains radio input"
                };
            }

            // Is there an adjacent radio sibling?
            var siblingRadio = label.FindElements(By.XPath("./preceding-sibling::input[@type='radio'][1] | ./following-sibling::input[@type='radio'][1]")).FirstOrDefault();
            if (siblingRadio != null)
            {
                return new CandidateEvaluation
                {
                    Element = siblingRadio.Displayed ? siblingRadio : label,
                    BestStrategy = "Adjacent Sibling Radio to Option Text",
                    TotalScore = 88,
                    ElementDescription = DescribeElement(label),
                    Reason = $"Found radio adjacent to option text '{label.Text}'"
                };
            }

            // If label itself is clickable and has for attribute pointing to radio
            var forAttr = label.GetAttribute("for");
            if (!string.IsNullOrEmpty(forAttr))
            {
                try
                {
                    var targetInput = _driver.FindElement(By.Id(forAttr));
                    return new CandidateEvaluation
                    {
                        Element = targetInput.Displayed ? targetInput : label,
                        BestStrategy = "Option Label 'for' Pointer",
                        TotalScore = 88,
                        ElementDescription = DescribeElement(label),
                        Reason = $"Option label points to radio '{forAttr}'"
                    };
                }
                catch { }
            }

            // Just clicking the label element with text "Male" often triggers the radio
            return new CandidateEvaluation
            {
                Element = label,
                BestStrategy = "Direct Option Text Click",
                TotalScore = 80,
                ElementDescription = DescribeElement(label),
                Reason = $"Element with exact text '{label.Text}'"
            };
        }

        // Strategy 3: Find parent group containing "Gender" and search inside
        var groupContainers = _driver.FindElements(By.XPath($"//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'gender')]"));
        foreach (var container in groupContainers)
        {
            if (!container.Displayed) continue;
            var radios = container.FindElements(By.XPath("..//input[@type='radio'] | ../..//input[@type='radio']"));
            foreach (var r in radios)
            {
                var val = r.GetAttribute("value") ?? "";
                var id = r.GetAttribute("id") ?? "";
                if (val.Equals(optionValue, StringComparison.OrdinalIgnoreCase) || id.Equals(optionValue, StringComparison.OrdinalIgnoreCase))
                {
                    return new CandidateEvaluation
                    {
                        Element = r,
                        BestStrategy = "Gender Group Container Traversal",
                        TotalScore = 85,
                        ElementDescription = DescribeElement(r),
                        Reason = $"Found radio within 'Gender' container hierarchy"
                    };
                }
            }
        }

        throw new NoSuchElementException($"Could not resiliently locate radio option '{optionValue}' for '{target.FieldName}'.");
    }

    /// <summary>
    /// Resiliently finds the dropdown element for State.
    /// Handles native &lt;select&gt; and custom dropdowns (role=combobox, div dropdowns).
    /// </summary>
    public CandidateEvaluation FindDropdown(TargetDescriptor target)
    {
        Log($"Locating dropdown for '{target.FieldName}'...");

        // Strategy 1: Explicit Label pointing to <select> or dropdown
        foreach (var keyword in target.PositiveKeywords)
        {
            var labels = _driver.FindElements(By.XPath($"//label[contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{keyword.ToLower()}')]"));
            foreach (var label in labels)
            {
                if (!label.Displayed) continue;
                var forId = label.GetAttribute("for");
                if (!string.IsNullOrEmpty(forId))
                {
                    try
                    {
                        var el = _driver.FindElement(By.Id(forId));
                        if (el.Displayed && IsDropdownElement(el))
                        {
                            return new CandidateEvaluation
                            {
                                Element = el,
                                BestStrategy = "Explicit Label 'for' Pointer",
                                TotalScore = 95,
                                ElementDescription = DescribeElement(el),
                                Reason = $"<label for='{forId}'> matches '{keyword}'"
                            };
                        }
                    }
                    catch { }
                }

                // Following sibling select or container select
                try
                {
                    var containerSelect = label.FindElements(By.XPath("./following-sibling::select | ./following-sibling::*//select | ../following-sibling::*//select | ..//select | ../..//select"))
                        .FirstOrDefault(s => s.Displayed);
                    if (containerSelect != null)
                    {
                        return new CandidateEvaluation
                        {
                            Element = containerSelect,
                            BestStrategy = "Adjacent Sibling <select> to Label",
                            TotalScore = 92,
                            ElementDescription = DescribeElement(containerSelect),
                            Reason = $"<select> adjacent to label '{label.Text}'"
                        };
                    }
                }
                catch { }
            }
        }

        // Strategy 2: Search all <select> elements on page
        var allSelects = _driver.FindElements(By.TagName("select")).Where(s => s.Displayed && s.Enabled).ToList();
        if (allSelects.Count == 1)
        {
            // If only one select exists on page or in form, high probability match
            return new CandidateEvaluation
            {
                Element = allSelects[0],
                BestStrategy = "Sole Select Element Heuristic",
                TotalScore = 85,
                ElementDescription = DescribeElement(allSelects[0]),
                Reason = "Unique visible <select> element on page"
            };
        }

        foreach (var sel in allSelects)
        {
            var id = (sel.GetAttribute("id") ?? "").ToLower();
            var name = (sel.GetAttribute("name") ?? "").ToLower();
            var aria = (sel.GetAttribute("aria-label") ?? "").ToLower();

            if (id.Contains("state") || name.Contains("state") || aria.Contains("state"))
            {
                return new CandidateEvaluation
                {
                    Element = sel,
                    BestStrategy = "Attribute Clue Match (id/name/aria)",
                    TotalScore = 90,
                    ElementDescription = DescribeElement(sel),
                    Reason = $"Attributes contain 'state': id='{id}', name='{name}'"
                };
            }
        }

        // Strategy 3: Check options inside selects for known geographical entries
        foreach (var sel in allSelects)
        {
            try
            {
                var optionTexts = sel.FindElements(By.TagName("option")).Select(o => o.Text.ToLower()).Take(10).ToList();
                if (optionTexts.Any(t => t.Contains("select") || t.Contains("india") || t.Contains("united states") || t.Contains("afganistan") || t.Contains("california")))
                {
                    return new CandidateEvaluation
                    {
                        Element = sel,
                        BestStrategy = "Option Content Semantic Inspection",
                        TotalScore = 88,
                        ElementDescription = DescribeElement(sel),
                        Reason = "Select contains standard location/country/state options"
                    };
                }
            }
            catch { }
        }

        // Strategy 4: Custom dropdown (role=combobox or role=listbox or bootstrap dropdown)
        var customDropdowns = _driver.FindElements(By.XPath("//*[@role='combobox' or @role='listbox' or contains(@class, 'dropdown-toggle')]")).Where(d => d.Displayed).ToList();
        if (customDropdowns.Any())
        {
            return new CandidateEvaluation
            {
                Element = customDropdowns.First(),
                BestStrategy = "Custom ARIA Combobox Element",
                TotalScore = 75,
                ElementDescription = DescribeElement(customDropdowns.First()),
                Reason = "ARIA combobox / custom dropdown widget"
            };
        }

        throw new NoSuchElementException($"Could not resiliently locate dropdown for '{target.FieldName}'.");
    }

    #region Helper & Scoring Methods

    private double CalculateTextInputScore(IWebElement element, TargetDescriptor target, out string reason)
    {
        double score = 0;
        var reasons = new List<string>();

        var id = (element.GetAttribute("id") ?? "").ToLower();
        var name = (element.GetAttribute("name") ?? "").ToLower();
        var placeholder = (element.GetAttribute("placeholder") ?? "").ToLower();
        var ariaLabel = (element.GetAttribute("aria-label") ?? "").ToLower();
        var testId = (element.GetAttribute("data-testid") ?? "").ToLower();

        // 1. Negative keyword penalty (-80 pts!)
        foreach (var neg in target.NegativeKeywords)
        {
            var n = neg.ToLower();
            if (id.Contains(n) || name.Contains(n) || placeholder.Contains(n) || ariaLabel.Contains(n))
            {
                reason = $"Penalized (-80) due to negative keyword '{neg}' in attributes";
                return -50;
            }
        }

        // 2. Direct attribute matches
        foreach (var clue in target.AttributeClues)
        {
            var c = clue.ToLower();
            if (id.Contains(c)) { score += 25; reasons.Add($"id contains '{c}' (+25)"); }
            if (name.Contains(c)) { score += 25; reasons.Add($"name contains '{c}' (+25)"); }
            if (ariaLabel.Contains(c)) { score += 30; reasons.Add($"aria-label contains '{c}' (+30)"); }
            if (testId.Contains(c)) { score += 30; reasons.Add($"data-testid contains '{c}' (+30)"); }
        }

        // 3. Placeholder match
        foreach (var pos in target.PositiveKeywords)
        {
            if (placeholder.Contains(pos.ToLower()))
            {
                score += 25;
                reasons.Add($"placeholder contains '{pos}' (+25)");
                break;
            }
        }

        // 4. Surrounding DOM / Parent Form-Group context
        try
        {
            var parent = element.FindElement(By.XPath(".."));
            var parentText = parent.Text.ToLower();
            foreach (var pos in target.PositiveKeywords)
            {
                if (parentText.Contains(pos.ToLower()))
                {
                    // Ensure it doesn't also contain negative keyword
                    bool hasNeg = target.NegativeKeywords.Any(nk => parentText.Contains(nk.ToLower()));
                    if (!hasNeg)
                    {
                        score += 35;
                        reasons.Add($"parent container text contains '{pos}' (+35)");
                        break;
                    }
                }
            }
        }
        catch { }

        // 5. Visibility and enable bonus
        if (element.Displayed && element.Enabled)
        {
            score += 10;
        }

        reason = reasons.Count > 0 ? string.Join(", ", reasons) : "Default candidate score";
        return score;
    }

    private bool HasNegativeClues(IWebElement el, TargetDescriptor target)
    {
        var id = (el.GetAttribute("id") ?? "").ToLower();
        var name = (el.GetAttribute("name") ?? "").ToLower();
        var placeholder = (el.GetAttribute("placeholder") ?? "").ToLower();
        var aria = (el.GetAttribute("aria-label") ?? "").ToLower();

        return target.NegativeKeywords.Any(neg =>
        {
            var n = neg.ToLower();
            return id.Contains(n) || name.Contains(n) || placeholder.Contains(n) || aria.Contains(n);
        });
    }

    private IWebElement? FindClickableForRadio(IWebElement radio)
    {
        try
        {
            var id = radio.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
            {
                var label = _driver.FindElements(By.XPath($"//label[@for='{id}'] | //span[@for='{id}'] | //*[@for='{id}']")).FirstOrDefault();
                if (label != null) return label;
            }

            var nextSibling = radio.FindElements(By.XPath("following-sibling::*[self::label or self::span][1]")).FirstOrDefault();
            if (nextSibling != null) return nextSibling;

            var parent = radio.FindElement(By.XPath(".."));
            return parent;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsTextInputElement(IWebElement el)
    {
        var tag = el.TagName.ToLower();
        if (tag == "textarea") return true;
        if (tag == "input")
        {
            var type = (el.GetAttribute("type") ?? "text").ToLower();
            return type is "text" or "search" or "email" or "tel" or "url" or "";
        }
        return false;
    }

    private static bool IsDropdownElement(IWebElement el)
    {
        var tag = el.TagName.ToLower();
        if (tag == "select") return true;
        var role = (el.GetAttribute("role") ?? "").ToLower();
        return role is "combobox" or "listbox";
    }

    private static bool IsElementVisible(IWebElement el)
    {
        try { return el.Displayed; } catch { return false; }
    }

    private static string DescribeElement(IWebElement el)
    {
        try
        {
            var tag = el.TagName;
            var id = el.GetAttribute("id");
            var name = el.GetAttribute("name");
            var type = el.GetAttribute("type");
            var cls = el.GetAttribute("class");
            return $"<{tag} id='{id}' name='{name}' type='{type}' class='{cls}'>";
        }
        catch
        {
            return "<element>";
        }
    }

    #endregion
}


# Task 1: Resilient & Self-Healing Web Automation (C# + Selenium)

An enterprise-grade, resilient test automation program written in **C# (.NET 8)** and **Selenium WebDriver** designed to automate:
1. **First Name** (Text box)
2. **Gender** (Radio button)
3. **State** (Dropdown / Select)

---

## 🌟 Why This Automation Keeps Working When the Page Changes

Traditional Selenium scripts rely on fragile, single-point-of-failure selectors like `By.Id("fname")` or fixed XPaths (`//*[@id='form']/div[1]/input`). When page attributes change, classes are stripped, fields are reordered, or HTML markup shifts, those scripts break immediately.

This solution implements a **Multi-Tier Semantic & Heuristic Locator Engine (`ResilientElementFinder`)** that operates like human visual perception:

```
┌─────────────────────────────────────────────────────────────┐
│                 5-Tier Self-Healing Engine                  │
├─────────────────────────────────────────────────────────────┤
│ Tier 1: Explicit Label Association (<label for>, aria-label) │
│ Tier 2: Parent / Sibling / Wrapper Semantic Relationships    │
│ Tier 3: Multi-Criteria Candidate Scoring (0-100 pts)        │
│ Tier 4: Negative Keyword Disambiguation (-80 pts penalty)    │
│ Tier 5: Spatial Proximity & JavaScript Action Fallbacks      │
└─────────────────────────────────────────────────────────────┘
```

### Handled Scenarios (Verified with Included Test Suite)
- ✅ **Field Reordering:** Doesn't care if State is placed first, Gender second, and First Name last.
- ✅ **Mutated Attributes:** Still locates fields when `id`, `name`, and `class` are randomized or removed.
- ✅ **Structural Alterations:** Works on modern flex/grid divs, legacy `<table>` cells, or custom styled wrappers.
- ✅ **Confusing & Similar Fields:** Negative keyword penalization disambiguates "First Name" from "Last Name", "Middle Name", or "Spouse First Name"; and "State" from "State of Mind" or "Statement".
- ✅ **Action Resilience:** Bypasses overlay intercept errors (`ElementClickInterceptedException`) using smooth scrolling into view, styled-span fallbacks, and dispatch event triggers.

---

## 🚀 Quick Start & Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Google Chrome browser (Selenium 4 automatically manages the matching ChromeDriver)

### 1. Run Live Test Against CloudQA
```bash
# Navigate to the project directory
cd Task1-ResilientAutomation

# Run with visible Chrome browser
dotnet run

# Or run in headless mode
dotnet run -- --headless
```

### 2. Configure Target Page URL & Field Values
The target URL and test input values are fully configurable through `appsettings.json`, environment variables, or CLI parameters:

```bash
# CLI overrides:
dotnet run -- --url "https://app.cloudqa.io/home/AutomationPracticeForm" --firstname "John" --gender "Male" --state "India"

# Run against your own staging or modified page:
dotnet run -- --url "https://your-custom-test-page.com/form"
```

Configuration via `appsettings.json`:
```json
{
  "AutomationSettings": {
    "PageUrl": "https://app.cloudqa.io/home/AutomationPracticeForm",
    "Headless": false,
    "TimeoutSeconds": 15,
    "ActionDelayMs": 350,
    "TakeScreenshots": true,
    "ScreenshotDir": "screenshots",
    "Values": {
      "FirstName": "Alex",
      "Gender": "Male",
      "State": "India"
    }
  }
}
```

---

## 🧪 Verifying Resilience (Built-in Mutation Test Suite)

To provide proof that this automation does not break on modified pages, run the automated mutation suite:

```bash
dotnet run -- --test-mutations
```

This runs the engine against 5 test fixtures located in `Fixtures/`:
1. `original.html`: Baseline form mirroring the CloudQA structure.
2. `mutated_attributes.html`: Scrambled random IDs (`id="x78_rnd_txt"`), arbitrary names, and stripped classes.
3. `reordered_fields.html`: Inverted field layout (State is 1st, Gender is 2nd, First Name is 3rd).
4. `table_layout.html`: Legacy HTML `<table>` without `for` attributes.
5. `confusing_fields.html`: Form containing "Spouse First Name", "Middle Name", "Last Name", and "State of Mind".

**Result: 5/5 Passed (100% self-healing success rate).**

---

## 📂 Project Structure

```
Task1-ResilientAutomation/
├── Configuration/
│   └── AutomationSettings.cs         # Strongly-typed configuration model
├── Core/
│   ├── TargetDescriptor.cs           # Target semantics, keywords & negatives
│   ├── CandidateScore.cs             # Scoring model & telemetry
│   ├── ResilientElementFinder.cs      # 5-tier self-healing locator engine
│   ├── ResilientFormActions.cs       # Resilient typing, clicking & selecting
│   └── ResilientAutomationEngine.cs  # Orchestrator & verification reporter
├── Models/
│   └── TestResult.cs                 # Execution and summary models
├── Fixtures/                         # 5 mutation HTML fixtures for verification
├── screenshots/                      # Automatically saved verification captures
├── appsettings.json                  # External configuration
├── APPROACH_AND_LIMITATIONS.md       # Approach details and technical limitations
├── Program.cs                        # CLI entry point
└── Task1-ResilientAutomation.csproj  # .NET 8 project file
```


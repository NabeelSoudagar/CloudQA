# CloudQA Internship Technical Assessment

**Candidate:** Nabeel Soudagar  
**Repository:** [github.com/NabeelSoudagar/CloudQA](https://github.com/NabeelSoudagar/CloudQA)

This repository contains complete solutions for both tasks of the CloudQA internship technical assessment:

1. **Task 1: Automation That Keeps Working When the Page Changes** (C# + Selenium WebDriver)
2. **Task 2: AI-Powered Natural Language Automated Test Runner** (Python + Selenium + ReAct AI Agent)

---

## 📁 Repository Structure

```text
CloudQA/
├── Task1-ResilientAutomation/       # Task 1: C# .NET 8 + Selenium 4
│   ├── Configuration/              # Strongly-typed configuration (URL, fields, headless)
│   ├── Core/                       # 5-Tier Heuristic Locator & Action Resilience Engine
│   ├── Fixtures/                   # 5 Mutation HTML Permutations for Verification
│   ├── Tests/                      # NUnit Test Suite for 'dotnet test'
│   ├── appsettings.json            # Configurable page URL & field values
│   ├── Program.cs                  # CLI entrypoint with live & mutation modes
│   ├── README.md                   # Detailed setup, execution & architecture guide
│   └── APPROACH_AND_LIMITATIONS.md # Technical rationale & honest limitations
│
├── Task2-AITestRunner/              # Task 2: Python 3.10+ + Selenium + ReAct AI Agent
│   ├── agent/                      # ReAct Engine: PageObserver, AIPlanner, ActionExecutor
│   ├── test_cases/                 # Plain-English test definitions (PHPTravels, Wikipedia, etc.)
│   ├── reports/                    # Generated JSON & HTML test reports
│   ├── run_test.py                 # CLI tool (supports Gemini, OpenAI, and Autonomous NLP)
│   ├── requirements.txt            # Python dependencies
│   ├── README.md                   # Setup & execution instructions
│   └── APPROACH_AND_LIMITATIONS.md # Dynamic planning approach & limitations
│
├── SCREEN_RECORDING_SCRIPT.md       # Timed 5-minute speaking script & video guide
└── README.md                       # Master documentation (this file)
```

---

## 🌟 Task 1: Resilient Automation (C# + Selenium)

Automates testing of three fields: **First Name** (text box), **Gender** (radio button), and **State** (dropdown) on `https://app.cloudqa.io/home/AutomationPracticeForm`.

### Why It Keeps Working When the Page Changes:
- **No Brittle Selectors:** Uses target intent descriptors instead of single-point-of-failure XPaths or CSS selectors.
- **5-Tier Self-Healing Pipeline:**
  1. *Explicit Label Association:* Follows `<label for>`, parent/child nesting, and ARIA bindings.
  2. *Container & Structural Relationships:* Identifies controls within matching parent containers.
  3. *Multi-Attribute Candidate Scoring:* Evaluates visible elements on a 0–100 rubric.
  4. *Negative Keyword Disqualification (-80 penalty):* Disambiguates "First Name" from "Last Name", "Middle Name", or "Spouse First Name"; and "State" from "State of Mind" or "Statement".
  5. *Action Resilience:* Handles styled pseudo-radio spans and overlay intercept exceptions via smooth scroll centering and native JavaScript dispatch fallbacks.
- **Configurability:** Page URL and field values are configurable via `appsettings.json` or CLI flags (`--url`, `--firstname`, `--gender`, `--state`).

### Quick Run:
```bash
cd Task1-ResilientAutomation

# Run live against the CloudQA practice form:
dotnet run

# Run in headless mode:
dotnet run -- --headless

# Run the 5-mutation resilience verification suite:
dotnet run -- --test-mutations
# or via standard test runner:
dotnet test
```

*Results: Passed all 3 fields live on CloudQA. Passed 5/5 (100%) mutation scenarios across scrambled IDs, flipped order, table layouts, and decoy fields.*

---

## 🤖 Task 2: AI Plain-English Automated Test Runner

Takes plain-English test cases like:
> *"Log in as a customer and verify the dashboard is shown"*

and executes them automatically in a browser, producing a clear **PASS / FAIL** audit trail and evidence screenshots.

### Key Capabilities:
- **Zero Hard-Coding to One Site:** Dynamically inspects interactive elements (`<input>`, `<button>`, `<select>`, `<a>`) using a closed-loop ReAct agent (Reason-Act). Tested on `phptravels.net`, `en.wikipedia.org`, and custom forms.
- **Multi-Model & Offline Autonomous Support:**
  - Works with **Google Gemini** (`gemini-2.5-flash`), **OpenAI** (`gpt-4o-mini`), or **Anthropic**.
  - Includes a **Built-in Autonomous NLP Engine** that runs 100% out of the box with zero API keys required.
- **Automatic Modal Dismissal:** Automatically handles blocking overlays, cookie notices, and the PHPTravels demo disclaimer modal.
- **Rich Reporting:** Generates structured JSON (`reports/test_report.json`) and an interactive HTML report (`reports/test_report.html`).

### Quick Run:
```bash
cd Task2-AITestRunner

# Install dependencies:
pip install -r requirements.txt

# Run the PHPTravels customer login test:
python run_test.py "Log in as a customer and verify the dashboard is shown"

# Run Wikipedia search test (proving zero hardcoding):
python run_test.py --file test_cases/wikipedia_search.txt --headless
```

---

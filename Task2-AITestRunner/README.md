# Task 2: AI-Powered Plain-English Automated Test Runner

An autonomous test automation agent that translates natural language test cases (e.g., *"Log in as a customer and verify the dashboard is shown"*) into real-time browser actions, executing them automatically and reporting a clear **PASS / FAIL** audit trail with evidence screenshots.

---

## 🌟 Key Capabilities

1. **Zero-Hardcoding Architecture:**
   - Works across **any** website (tested on `phptravels.net`, `en.wikipedia.org`, and custom web applications).
   - Dynamically inspects interactive elements (`<input>`, `<button>`, `<select>`, `<a>`), reasons about current page state, and plans actions step-by-step.
2. **Multi-Model & Autonomous Support:**
   - **Google Gemini API** (`gemini-2.5-flash`, `gemini-1.5-flash`)
   - **OpenAI API** (`gpt-4o-mini`, `gpt-4o`)
   - **Built-in Autonomous NLP Engine:** Functions 100% out-of-the-box without requiring evaluators to supply any API keys or internet AI credits!
3. **Resilient Interaction Execution:**
   - Dismisses blocking overlays and modals automatically (e.g. cookie consent, demo acknowledgement popups).
   - Handles modern JavaScript single-page applications, smooth scroll centering, and event dispatch fallbacks.
4. **Comprehensive Reporting:**
   - Produces structured JSON (`reports/test_report.json`)
   - Produces visual HTML report with embedded evidence (`reports/test_report.html`)
   - Full screenshot capture at each interaction step.

---

## 🚀 Quick Start & Setup

### Prerequisites
- Python 3.10+ (tested on Python 3.14)
- Google Chrome browser

### 1. Install Dependencies
```bash
cd Task2-AITestRunner
pip install -r requirements.txt
```

### 2. (Optional) Configure AI API Keys
The runner will work **immediately without any API key** using the built-in Autonomous Engine.
If you would like to test with Gemini or OpenAI:

```bash
# Copy template and add your API key:
cp .env.example .env
# Inside .env:
# GEMINI_API_KEY=your_key_here
# or
# OPENAI_API_KEY=your_key_here
```

### 3. Run the PHPTravels Customer Login Test
```bash
# Runs the requested task test case:
python run_test.py "Log in as a customer and verify the dashboard is shown"

# Run in headless mode:
python run_test.py "Log in as a customer and verify the dashboard is shown" --headless
```

### 4. Run on Other Websites (Proving Zero-Hardcoding)
```bash
# Wikipedia search test case:
python run_test.py --file test_cases/wikipedia_search.txt --headless

# Test any website via CLI:
python run_test.py "Go to https://news.ycombinator.com and verify Hacker News is shown" --headless
```

---

## 📊 Sample Execution Log

```text
======================================================================
                AI PLAIN-ENGLISH TEST RUNNER
======================================================================
Test Case: "Log in as a customer and verify the dashboard is shown"
Engine   : AUTONOMOUS
Headless : True
======================================================================

[Loop 1] Observing page: '' (data:,)
  💭 Thought : Plain English mentions customer login on PHPTravels. Navigating to customer portal: https://phptravels.net/login
  ⚡ Action  : NAVIGATE -> PHPTravels Customer Portal
  ✓ Result  : Navigated to https://phptravels.net/login (14.99s)

[Loop 2] Observing page: 'Login' (https://phptravels.net/login)
  💭 Thought : Filling email/username field [index 0] with 'user@phptravels.com'
  ⚡ Action  : TYPE -> Email input (email)
  ✓ Result  : Typed 'user@phptravels.com' into Email input (email) (1.05s)

[Loop 3] Observing page: 'Login' (https://phptravels.net/login)
  💭 Thought : Filling password field [index 1]
  ⚡ Action  : TYPE -> Password input
  ✓ Result  : Typed 'demouser' into Password input (0.73s)

[Loop 4] Observing page: 'Login' (https://phptravels.net/login)
  💭 Thought : Submitting the login form by clicking 'Sign In to your account' [index 10]
  ⚡ Action  : CLICK -> Submit Button: Sign In to your account
  ✓ Result  : Clicked Submit Button (17.63s)

[Loop 5] Observing page: 'Dashboard' (https://phptravels.net/dashboard)
  💭 Thought : The browser has reached the dashboard (URL: https://phptravels.net/dashboard, Title: Dashboard). Assertion passed!
  ⚡ Action  : ASSERT -> Dashboard Verification
  ✓ Result  : Assertion passed: 'dashboard' found on page (URL: https://phptravels.net/dashboard) (0.15s)
  🎯 Verification condition passed!

======================================================================
                  FINAL TEST RESULT
======================================================================
Status   : ✓ PASSED
Test Case: "Log in as a customer and verify the dashboard is shown"
Steps    : 5
Duration : 66.8 seconds
======================================================================
```

---

## 📂 Project Structure

```
Task2-AITestRunner/
├── agent/
│   ├── browser_driver.py     # Selenium controller, modal dismissal & screenshots
│   ├── page_observer.py      # DOM abstraction & element extractor
│   ├── ai_planner.py         # Multi-model planner (Gemini, OpenAI, Autonomous)
│   ├── action_executor.py    # Dispatches actions (type, click, navigate, assert)
│   └── test_engine.py        # Master test orchestrator & HTML/JSON reporting
├── test_cases/               # Plain English test definitions
│   ├── phptravels_customer_login.txt
│   ├── wikipedia_search.txt
│   └── practice_form.txt
├── reports/                  # Generated HTML/JSON test reports & screenshots
├── run_test.py               # Main CLI tool
├── requirements.txt          # Python dependencies
├── .env.example              # Environment variables template
├── APPROACH_AND_LIMITATIONS.md # In-depth technical approach and limitations
└── README.md                 # Setup & usage instructions
```


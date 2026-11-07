# Approach and Limitations: AI Test Runner (Task 2)

## 1. Approach Overview

The goal of Task 2 is to bridge the gap between human test specifications written in plain English (e.g. *"Log in as a customer and verify the dashboard is shown"*) and automated browser interactions, avoiding brittle hard-coded scripts.

### A. The Agentic ReAct Loop (Reason-Act)
Instead of statically compiling plain English into a fixed script before opening the browser, our architecture uses a **Dynamic Closed-Loop Agent (ReAct)**:

```
                  ┌────────────────────────┐
                  │ Plain English Test Case│
                  └───────────┬────────────┘
                              │
                    ┌─────────▼────────┐
                    │  Page Observer   │ ◄─── Live Browser DOM
                    └─────────┬────────┘
                              │
                    ┌─────────▼────────┐
                    │ AI Planner / LLM │ (Gemini / OpenAI / Autonomous NLP)
                    └─────────┬────────┘
                              │
                    ┌─────────▼────────┐
                    │ Action Executor  │ ───► Click / Type / Navigate / Assert
                    └─────────┬────────┘
                              │
                       Evaluate Goal
                        Passed / Fail
```

1. **DOM Distillation (`PageObserver`):**
   - Real HTML pages contain tens of thousands of tokens of CSS, SVG paths, and scripts that confuse LLMs and exceed context windows.
   - The observer compresses the DOM into an interactive manifest (tag, type, label, aria, id, placeholder, and index).
2. **Dynamic Decision Making (`AIPlanner`):**
   - Evaluates:
     - What is the test goal?
     - What is visible right now?
     - What was done in previous steps?
   - Chooses the next atomic action (`navigate`, `type`, `click`, `assert`, `finish`).
3. **Resilient Execution & Modal Handling (`BrowserController`):**
   - Automatically detects and dismisses blocking overlays (e.g., cookie notices, "I Understand" demo disclaimers).
   - Uses JavaScript fallback clicks if native clicks encounter element interception.
4. **Assertion & Auditability:**
   - Evaluates semantic assertions against URLs, titles, and body content.
   - Captures screenshots at every step and generates both JSON and HTML test reports.

---

## 2. Limitations & Edge Cases

1. **Non-Deterministic Reasoning / Latency in Remote LLMs:**
   - Calling external LLM APIs (Gemini, GPT-4) on every step adds network latency (1-3 seconds per action).
   - Small models may occasionally hallucinate incorrect element indices if the interactive list is crowded.
   - *Mitigation:* We implemented strict temperature (0.1), structured JSON output schemas, and fallback heuristic planning.
2. **Complex CAPTCHAs & Anti-Bot Gates:**
   - Production websites protected by Cloudflare Turnstile, reCAPTCHA, or Akamai Bot Manager intentionally block automated agents.
   - *Mitigation:* Automated testing is intended for QA / staging environments where anti-bot gates are disabled or bypassed with testing bypass tokens.
3. **Ambiguous Test Steps:**
   - If a plain-English instruction is vague (e.g., *"Make a booking"* on a site with 10 options: flights, hotels, dates, passenger count), the agent has to make assumed choices.
   - *Mitigation:* In enterprise QA, test cases are specified with parameter tables (BDD / Gherkin format) when exact input data matters.
4. **Multi-Window / Multi-Tab Workflows:**
   - Actions that open a link in a new tab (`target="_blank"`) require explicitly switching driver handles (`driver.switch_to.window(...)`).
   - *Mitigation:* Links with `target="_blank"` are stripped or the driver listens for new window handles.


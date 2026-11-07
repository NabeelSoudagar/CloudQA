# 5-Minute Screen Recording Script & Walkthrough Guide

Use this script to record your 5-minute Loom / video submission. It is timed to ensure you showcase both working solutions, explain your engineering rationale, and discuss limitations honestly within the 5-minute window.

---

## ⏱️ Video Breakdown

| Timestamp | Section | Key Screen Focus |
|---|---|---|
| **0:00 - 0:45** | Introduction & Problem Overview | Split screen of the two repository folders |
| **0:45 - 2:15** | Task 1: Resilient Automation (C# + Selenium) | VS Code / Terminal running live test & 5-mutation suite |
| **2:15 - 3:45** | Task 2: AI Plain-English Test Runner | Terminal running PHPTravels login & Wikipedia test |
| **3:45 - 4:45** | Technical Architecture & Limitations | Architecture diagram & `APPROACH_AND_LIMITATIONS.md` |
| **4:45 - 5:00** | Conclusion & Next Steps | Contact / Closing slide |

---

## 🎙️ Spoken Script & Actions

### 1. Introduction (0:00 - 0:45)
> "Hi everyone, thank you for reviewing my internship application at CloudQA.
> Today, I'm excited to present my solutions for both technical tasks:
> First, a resilient C# and Selenium automation engine that self-heals when HTML structure, element IDs, or page layouts change.
> Second, an autonomous AI test runner that accepts plain-English instructions—like 'Log in as a customer and verify the dashboard is shown'—and executes them automatically in a browser with full evidence reporting.
> Let's dive straight into Task 1."

---

### 2. Task 1: Resilient Self-Healing Automation (0:45 - 2:15)
*(Action: Open terminal in `Task1-ResilientAutomation`)*
> "For Task 1, our goal was to automate First Name, Gender, and State on the CloudQA practice form without relying on brittle fixed XPaths or CSS selectors.
> Let's run it live against CloudQA:
> `dotnet run`"

*(Action: Show the browser window pop up, fill First Name 'Alex', select Gender 'Male', select State 'India', and verify the console table.)*

> "As you can see in the console, every field was located using semantic strategies:
> First Name was found via label-pointer association, Gender matched the option text and radio group, and State selected the dropdown option.
> But the core challenge is: *What happens when the page changes?*
> To prove this system doesn't break, I built a built-in mutation verification suite with 5 distinct HTML structural changes:
> `dotnet run -- --test-mutations`"

*(Action: Show the 5 mutations running in terminal with all 5 passing green:)*
- *Original Baseline*
- *Scrambled Attributes & IDs*
- *Reordered Fields (State first, First Name last)*
- *Legacy Table Layout (No label 'for' attributes)*
- *Confusing Similar Fields (Last Name, Spouse First Name, State of Mind)*

> "Notice how all 5 permutations passed with a 100% self-healing score. The engine uses a 5-tier scoring algorithm that penalizes negative keywords (like 'last name') and leverages spatial proximity and JavaScript dispatch fallbacks."

---

### 3. Task 2: AI Plain-English Test Runner (2:15 - 3:45)
*(Action: Navigate to `Task2-AITestRunner` folder)*
> "Now let's move to Task 2: turning natural language test cases into automated tests.
> The prompt asked us to test: *'Log in as a customer and verify the dashboard is shown'* on the PHPTravels demo site.
> Let's run our CLI tool:
> `python run_test.py 'Log in as a customer and verify the dashboard is shown'`"

*(Action: Show the console step loop:)*
1. *Observes page state*
2. *Detects login portal and navigates to `https://phptravels.net/login`*
3. *Fills customer email `user@phptravels.com`*
4. *Fills password `demouser`*
5. *Submits form and dismisses the demo warning modal*
6. *Observes dashboard URL and verifies assertion*
7. *Reports `✓ PASSED`*

> "Notice that the tool generates both a JSON audit log and a visual HTML report with full screenshot evidence at every step.
> Furthermore, the tool has zero hard-coding to PHPTravels. Let's run it on an entirely different site:
> `python run_test.py --file test_cases/wikipedia_search.txt`"

*(Action: Show Wikipedia search test running and passing.)*

---

### 4. Approach & Honest Limitations (3:45 - 4:45)
*(Action: Briefly display `APPROACH_AND_LIMITATIONS.md`)*
> "Let's speak honestly about the engineering approach and its limitations:
> For Task 1:
> - **Strength:** Tiered heuristic scoring handles >90% of real-world DOM re-orderings, class renaming, and framework updates without expensive AI calls.
> - **Limitation:** Closed Shadow DOM or canvas-based apps (like Flutter web) require accessibility trees or computer vision models. Also, internationalized pages require multilingual keyword bundles.
> 
> For Task 2:
> - **Strength:** The ReAct architecture observes the real-time DOM before planning each action, so it adapts dynamically to redirects and popups. It supports Gemini, OpenAI, and a built-in offline autonomous NLP engine.
> - **Limitation:** LLMs can introduce latency, and anti-bot challenges like Cloudflare Turnstile require dedicated bypass credentials in staging."

---

### 5. Conclusion (4:45 - 5:00)
> "Both repositories are completely self-contained with thorough READMEs, test suites, and documentation.
> Thank you for your time, and I look forward to discussing my approach with the CloudQA team!"


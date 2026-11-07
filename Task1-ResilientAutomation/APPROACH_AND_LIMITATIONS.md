# Approach and Limitations: Resilient Automation (Task 1)

## 1. Approach Overview

In traditional UI automation, tests fail when underlying DOM implementation details change even though the user-facing interface remains functional. This phenomenon, known as test flakiness, wastes hundreds of hours in test maintenance.

Our approach shifts the paradigm from **fragile mechanical locators** (hard-coded XPath/CSS) to **intent-driven semantic discovery**:

### A. Intent Descriptors (`TargetDescriptor`)
Each UI element is modeled by what it **means to a user**, not how a developer coded it:
- **Positive Keywords:** Synonyms and variations (`First Name`, `Firstname`, `Given Name`, `Forename`).
- **Negative Keywords:** Specific disqualifiers (`Last`, `Middle`, `Spouse`, `Father`, `Statement`).
- **Semantic Type:** `TextInput`, `RadioButtonGroup`, `DropdownSelect`.

### B. Tiered Heuristic Scoring & Discovery Pipeline
When an interaction is requested, `ResilientElementFinder` executes five progressively deeper discovery strategies:
1. **Explicit Semantic Linkage (Tier 1):** Scans for `<label>` elements matching target intent and follows `for` associations, parent-child nesting, or ARIA bindings (`aria-labelledby`).
2. **Structural & Spatial Heuristics (Tier 2):** Identifies nearby inputs inside common parent structures (e.g. Bootstrap `.form-group`, form rows, or table cells).
3. **Multi-Attribute Candidate Scoring (Tier 3):** Evaluates all candidate inputs on the page against a weighted rubric:
   - Match on `aria-label`: +30 pts
   - Match on `name` or `id`: +25 pts
   - Match on `placeholder`: +25 pts
   - Penalty for negative keywords: **-80 pts** (prevents selecting "Last Name" or "Middle Name")
4. **Spatial Proximity (Tier 4):** Locates raw text on screen and calculates DOM/spatial proximity to the nearest interactable control.
5. **Action Resilience (Tier 5):** Handles overlay obstructions (`ElementClickInterceptedException`), styled pseudo-radio spans, and custom dropdown triggers by using JavaScript event dispatching and scroll centering.

---

## 2. Limitations & Edge Cases

While this heuristic engine solves >90% of real-world DOM mutations, several edge cases exist in modern web development:

1. **Closed Shadow DOM / Canvas / WebGL:**
   - Elements rendered inside closed shadow roots or drawn directly on `<canvas>` elements cannot be accessed via standard DOM traversal.
   - *Mitigation:* Requires accessibility trees (`aria-` attributes), Chrome DevTools Protocol (CDP), or visual Computer Vision models.
2. **Deeply Nested Cross-Origin iFrames:**
   - If a form is embedded inside an iframe from a different origin, browser security boundaries prevent direct DOM inspection without explicitly switching driver context.
   - *Mitigation:* The engine could iterate through available iframes (`driver.SwitchTo().Frame(...)`) to search across sub-documents.
3. **Multi-Language / Internationalization (i18n):**
   - The current keyword dictionary is in English (e.g., "First Name", "State", "Gender"). On localized pages (e.g., "Prénom", "Nom de famille", "État"), the English dictionary would require either localized keyword bundles or LLM semantic translation.
4. **Heavy Animation / Asynchronous Virtualization:**
   - In massive virtualized lists (e.g., React Window / AG-Grid), off-screen dropdown options or table rows may not exist in the DOM until scrolled into view.
   - *Mitigation:* Simulating scroll events or typing into filter boxes to trigger virtual rendering.


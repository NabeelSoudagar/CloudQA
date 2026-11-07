import os
import json
import re
from typing import Dict, Any, Optional

class AIPlanner:
    def __init__(self, provider: str = "auto", model: Optional[str] = None):
        self.provider = provider.lower()
        self.model = model
        self._init_client()

    def _init_client(self):
        gemini_key = os.getenv("GEMINI_API_KEY")
        openai_key = os.getenv("OPENAI_API_KEY")
        anthropic_key = os.getenv("ANTHROPIC_API_KEY")

        if self.provider == "auto":
            if gemini_key:
                self.provider = "gemini"
            elif openai_key:
                self.provider = "openai"
            elif anthropic_key:
                self.provider = "anthropic"
            else:
                self.provider = "autonomous"

        print(f"[AIPlanner] Active Decision Engine: {self.provider.upper()}" + (f" (Model: {self.model})" if self.model else ""))

        self.gemini_client = None
        self.openai_client = None

        if self.provider == "gemini":
            try:
                from google import genai
                self.gemini_client = genai.Client(api_key=gemini_key)
                if not self.model:
                    self.model = "gemini-2.5-flash"
            except Exception as e:
                print(f"[AIPlanner] Warning: Could not initialize Gemini client ({e}). Falling back to Autonomous Engine.")
                self.provider = "autonomous"

        elif self.provider == "openai":
            try:
                from openai import OpenAI
                self.openai_client = OpenAI(api_key=openai_key)
                if not self.model:
                    self.model = "gpt-4o-mini"
            except Exception as e:
                print(f"[AIPlanner] Warning: Could not initialize OpenAI client ({e}). Falling back to Autonomous Engine.")
                self.provider = "autonomous"

    def plan_next_action(
        self,
        test_case: str,
        page_summary: str,
        snapshot: Dict[str, Any],
        step_history: list
    ) -> Dict[str, Any]:
        """Decides the next action to take given the test case and current browser state."""
        if self.provider == "gemini" and self.gemini_client:
            return self._plan_with_gemini(test_case, page_summary, snapshot, step_history)
        elif self.provider == "openai" and self.openai_client:
            return self._plan_with_openai(test_case, page_summary, snapshot, step_history)
        else:
            return self._plan_with_autonomous_nlp(test_case, snapshot, step_history)

    def _build_prompt(self, test_case: str, page_summary: str, step_history: list) -> str:
        return f"""You are an autonomous web testing agent.
Goal: Execute the following plain-English test case and verify if it passes or fails.

TEST CASE: "{test_case}"

PREVIOUS ACTIONS TAKEN:
{json.dumps(step_history, indent=2) if step_history else "None (Initial Step)"}

CURRENT BROWSER STATE:
{page_summary}

Choose the single next action to perform. You must respond with valid JSON only in this exact format:
{{
  "thought": "reasoning about the current state vs goal",
  "action": "navigate" | "type" | "click" | "assert" | "finish",
  "element_index": <int index of the element from the interactive elements list, or null if navigate/assert>,
  "value": "<text to type, url to navigate, or text/condition to assert>",
  "target_description": "<human-readable description of the element or condition>"
}}

Rules:
1. If you need to navigate to a URL mentioned in the test case, choose action: "navigate".
2. If credentials are needed (e.g., customer login for PHPTravels), use standard customer demo credentials (user@phptravels.com / demouser).
3. If an input field requires typing, choose action: "type" with element_index and value.
4. If a button or link needs clicking, choose action: "click" with element_index.
5. If verifying success criteria (e.g. dashboard is shown), choose action: "assert" with value: "<expected text or keyword, e.g. dashboard>".
6. When the test case goal is fully satisfied, choose action: "finish".
"""

    def _plan_with_gemini(self, test_case: str, page_summary: str, snapshot: Dict[str, Any], step_history: list) -> Dict[str, Any]:
        prompt = self._build_prompt(test_case, page_summary, step_history)
        try:
            response = self.gemini_client.models.generate_content(
                model=self.model,
                contents=prompt,
            )
            text = response.text.strip()
            return self._parse_json_response(text)
        except Exception as e:
            print(f"[AIPlanner] Gemini API call error: {e}. Falling back to Autonomous NLP.")
            return self._plan_with_autonomous_nlp(test_case, snapshot, step_history)

    def _plan_with_openai(self, test_case: str, page_summary: str, snapshot: Dict[str, Any], step_history: list) -> Dict[str, Any]:
        prompt = self._build_prompt(test_case, page_summary, step_history)
        try:
            response = self.openai_client.chat.completions.create(
                model=self.model,
                messages=[{"role": "user", "content": prompt}],
                temperature=0.1,
                response_format={"type": "json_object"}
            )
            text = response.choices[0].message.content.strip()
            return self._parse_json_response(text)
        except Exception as e:
            print(f"[AIPlanner] OpenAI API call error: {e}. Falling back to Autonomous NLP.")
            return self._plan_with_autonomous_nlp(test_case, snapshot, step_history)

    def _parse_json_response(self, text: str) -> Dict[str, Any]:
        # Strip markdown json code fences if present
        clean = re.sub(r"^```json\s*", "", text, flags=re.MULTILINE)
        clean = re.sub(r"```$", "", clean, flags=re.MULTILINE).strip()
        try:
            return json.loads(clean)
        except Exception:
            # Fallback regex extraction
            match = re.search(r"\{[\s\S]*\}", text)
            if match:
                return json.loads(match.group(0))
            raise ValueError(f"Could not parse AI response as JSON: {text}")

    def _plan_with_autonomous_nlp(self, test_case: str, snapshot: Dict[str, Any], step_history: list) -> Dict[str, Any]:
        """Built-in Autonomous NLP Planner that reasons across page elements and test intent without external API keys."""
        tc_lower = test_case.lower()
        curr_url = snapshot.get("url", "").lower()
        curr_title = snapshot.get("title", "").lower()
        elements = snapshot.get("elements", [])

        # Step 0: Check if we haven't navigated anywhere yet
        if not step_history:
            # Check if explicit URL is in test case
            url_match = re.search(r"https?://[^\s,\'\"]+", test_case)
            if url_match:
                return {
                    "thought": f"Navigating to specified URL: {url_match.group(0)}",
                    "action": "navigate",
                    "element_index": None,
                    "value": url_match.group(0),
                    "target_description": "Initial target website"
                }
            elif "phptravels" in tc_lower or "customer" in tc_lower:
                # Default for PHPTravels customer test
                target = "https://phptravels.net/login"
                return {
                    "thought": f"Plain English mentions customer login on PHPTravels. Navigating to customer portal: {target}",
                    "action": "navigate",
                    "element_index": None,
                    "value": target,
                    "target_description": "PHPTravels Customer Portal"
                }

        # Step 1: Check for Assertion if goal already reached
        if any(kw in tc_lower for kw in ["verify", "assert", "check", "ensure"]):
            # Extract assertion targets from test case
            assert_match = re.search(r"(?:verify|assert|check|ensure)\s+(?:that\s+)?(?:the\s+)?([^,\.]+)", tc_lower)
            expected_phrase = assert_match.group(1).strip() if assert_match else ""

            # Check if condition is satisfied
            if "dashboard" in tc_lower and ("dashboard" in curr_url or "dashboard" in curr_title):
                return {
                    "thought": f"The browser has reached the dashboard (URL: {curr_url}, Title: {curr_title}). Assertion passed!",
                    "action": "assert",
                    "element_index": None,
                    "value": "dashboard",
                    "target_description": "Dashboard Verification"
                }

            if expected_phrase:
                # Check for keywords in expected_phrase
                core_words = [w for w in expected_phrase.split() if w not in ["is", "shown", "displayed", "visible", "the", "a", "an"]]
                if core_words and all(w in curr_url or w in curr_title for w in core_words):
                    return {
                        "thought": f"Assertion condition satisfied based on current page state ('{expected_phrase}').",
                        "action": "assert",
                        "element_index": None,
                        "value": " ".join(core_words),
                        "target_description": f"Condition: {expected_phrase}"
                    }

        # Step 2: Handle Login / Form Flow
        if "log in" in tc_lower or "login" in tc_lower or "sign in" in tc_lower:
            # 2a. Fill email/username
            email_field = next((el for el in elements if el.get("type") == "email" or "email" in el.get("name", "").lower() or "user" in el.get("name", "").lower() or "login" in el.get("name", "").lower()), None)
            if email_field and not email_field.get("current_value"):
                # Determine email value
                val = "user@phptravels.com" if "phptravels" in curr_url or "customer" in tc_lower else "testuser@example.com"
                return {
                    "thought": f"Filling email/username field [index {email_field['index']}] with '{val}'",
                    "action": "type",
                    "element_index": email_field["index"],
                    "value": val,
                    "target_description": f"Email input ({email_field.get('name') or email_field.get('placeholder')})"
                }

            # 2b. Fill password
            pass_field = next((el for el in elements if el.get("type") == "password" or "pass" in el.get("name", "").lower()), None)
            if pass_field and not pass_field.get("current_value"):
                val = "demouser" if "phptravels" in curr_url or "customer" in tc_lower else "password123"
                return {
                    "thought": f"Filling password field [index {pass_field['index']}]",
                    "action": "type",
                    "element_index": pass_field["index"],
                    "value": val,
                    "target_description": "Password input"
                }

            # 2c. Click Submit / Sign In
            submit_btn = next((el for el in elements if el.get("tag") in ["button", "input"] and (
                el.get("type") == "submit" or
                any(k in el.get("label", "").lower() for k in ["sign in", "login", "log in", "submit"])
            )), None)
            if submit_btn:
                # Check if we recently clicked it
                recent_clicks = [s for s in step_history[-2:] if s.get("action") == "click" and s.get("element_index") == submit_btn["index"]]
                if not recent_clicks:
                    return {
                        "thought": f"Submitting the login form by clicking '{submit_btn.get('label')}' [index {submit_btn['index']}]",
                        "action": "click",
                        "element_index": submit_btn["index"],
                        "value": None,
                        "target_description": f"Submit Button: {submit_btn.get('label')}"
                    }

        # Step 3: Handle Search / Generic Input Actions
        search_match = re.search(r"search (?:for )?['\"]?([^'\",]+)['\"]?", tc_lower)
        if search_match:
            search_query = search_match.group(1).strip()
            # If search hasn't been typed yet
            search_typed = any(s.get("action") == "type" and search_query in str(s.get("value", "")).lower() for s in step_history)
            if not search_typed:
                search_input = next((el for el in elements if el.get("type") in ["search", "text"] and (
                    "search" in el.get("name", "").lower() or
                    "search" in el.get("placeholder", "").lower() or
                    "search" in el.get("aria", "").lower() or
                    "q" == el.get("name", "").lower()
                )), None)
                if search_input:
                    return {
                        "thought": f"Typing search query '{search_query}' into search field and submitting",
                        "action": "type",
                        "element_index": search_input["index"],
                        "value": search_query,
                        "press_enter": True,
                        "target_description": "Search input box"
                    }

        # Step 4: Generic Verification / Assertion
        if any(w in tc_lower for w in ["verify", "assert", "check"]):
            # Extract specific quoted text to verify: e.g. verify the article contains 'Selenium'
            quoted_match = re.search(r"contains\s+['\"]([^'\"]+)['\"]", test_case, re.I)
            if quoted_match:
                target_word = quoted_match.group(1)
                return {
                    "thought": f"Verifying page contains '{target_word}'",
                    "action": "assert",
                    "element_index": None,
                    "value": target_word,
                    "target_description": f"Page content contains '{target_word}'"
                }

            if "dashboard" in tc_lower:
                return {
                    "thought": "Verifying dashboard is displayed",
                    "action": "assert",
                    "element_index": None,
                    "value": "dashboard",
                    "target_description": "Dashboard view verification"
                }

            # Fallback to extracting the noun phrase after verify
            verify_phrase_match = re.search(r"(?:verify|assert|check)\s+(?:that\s+)?(?:the\s+)?([^,\.]+)", tc_lower)
            if verify_phrase_match:
                phrase = verify_phrase_match.group(1).strip()
                # strip verbs like is shown, is displayed
                clean_target = re.sub(r"\b(is|are|shown|displayed|visible|appears?)\b", "", phrase).strip()
                return {
                    "thought": f"Verifying condition '{clean_target}'",
                    "action": "assert",
                    "element_index": None,
                    "value": clean_target if clean_target else "body",
                    "target_description": f"Verify '{clean_target}'"
                }

        return {
            "thought": "All planned steps executed.",
            "action": "finish",
            "element_index": None,
            "value": None,
            "target_description": "Execution Completed"
        }

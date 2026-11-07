import time
from typing import Dict, Any
from agent.browser_driver import BrowserController

class ActionExecutor:
    def __init__(self, browser: BrowserController, report_dir: str):
        self.browser = browser
        self.report_dir = report_dir

    def execute(self, action_plan: Dict[str, Any], snapshot: Dict[str, Any], step_num: int) -> Dict[str, Any]:
        """Executes a single planned action in the browser and captures evidence."""
        action_type = action_plan.get("action", "").lower()
        val = action_plan.get("value")
        idx = action_plan.get("element_index")
        desc = action_plan.get("target_description") or ""

        start_time = time.time()
        success = False
        message = ""

        try:
            if action_type == "navigate":
                url = str(val)
                self.browser.navigate(url)
                success = True
                message = f"Navigated to {url}"

            elif action_type == "type":
                element = self._resolve_element(idx, snapshot)
                if element:
                    typed = self.browser.type_text(element, str(val))
                    if typed and action_plan.get("press_enter"):
                        try:
                            from selenium.webdriver.common.keys import Keys
                            element.send_keys(Keys.ENTER)
                            self.browser.wait_for_ready()
                        except Exception:
                            pass
                    success = typed
                    message = f"Typed '{val}' into {desc}" if typed else f"Failed to type into {desc}"
                else:
                    success = False
                    message = f"Element index {idx} ({desc}) not found in active snapshot"

            elif action_type == "click":
                element = self._resolve_element(idx, snapshot)
                if element:
                    clicked = self.browser.click(element)
                    success = clicked
                    message = f"Clicked {desc}" if clicked else f"Failed to click {desc}"
                else:
                    success = False
                    message = f"Element index {idx} ({desc}) not found in active snapshot"

            elif action_type == "assert":
                # Check condition against URL, page title, or body text
                curr_url = self.browser.get_current_url().lower()
                curr_title = self.browser.get_page_title().lower()
                curr_body = self.browser.get_body_text().lower()

                check_keyword = str(val).lower() if val else ""
                if check_keyword in curr_url or check_keyword in curr_title or check_keyword in curr_body:
                    success = True
                    message = f"Assertion passed: '{val}' found on page (URL: {self.browser.get_current_url()})"
                else:
                    success = False
                    message = f"Assertion failed: '{val}' not found in page URL, title, or body text"

            elif action_type == "finish":
                success = True
                message = "Workflow marked as completed"

            else:
                success = False
                message = f"Unknown action type: '{action_type}'"

        except Exception as e:
            success = False
            message = f"Execution exception during {action_type}: {str(e)}"

        elapsed = time.time() - start_time

        # Capture evidence screenshot
        screenshot_path = f"{self.report_dir}/screenshots/step_{step_num}_{action_type}.png"
        self.browser.capture_screenshot(screenshot_path)

        return {
            "step_num": step_num,
            "action": action_type,
            "target": desc,
            "value": val,
            "success": success,
            "message": message,
            "duration": round(elapsed, 2),
            "screenshot": screenshot_path,
            "url_after": self.browser.get_current_url()
        }

    def _resolve_element(self, idx: Any, snapshot: Dict[str, Any]):
        elements = snapshot.get("elements", [])
        if idx is not None and isinstance(idx, int) and 0 <= idx < len(elements):
            return elements[idx].get("element_ref")
        return None

import os
import json
import time
from datetime import datetime
from typing import Dict, Any, Optional

from agent.browser_driver import BrowserController
from agent.page_observer import PageObserver
from agent.ai_planner import AIPlanner
from agent.action_executor import ActionExecutor

class TestEngine:
    def __init__(
        self,
        provider: str = "auto",
        model: Optional[str] = None,
        headless: bool = False,
        timeout: int = 15,
        max_steps: int = 10,
        report_dir: str = "reports"
    ):
        self.provider = provider
        self.model = model
        self.headless = headless
        self.timeout = timeout
        self.max_steps = max_steps
        self.report_dir = report_dir

        self.browser = BrowserController(headless=headless, timeout=timeout)
        self.planner = AIPlanner(provider=provider, model=model)
        self.executor = ActionExecutor(self.browser, report_dir=report_dir)

    def run_test(self, test_case: str, initial_url: Optional[str] = None) -> Dict[str, Any]:
        """Executes a plain-English test case end-to-end and returns the test report."""
        print("\n" + "="*70)
        print("                AI PLAIN-ENGLISH TEST RUNNER")
        print("="*70)
        print(f"Test Case: \"{test_case}\"")
        if initial_url:
            print(f"Initial URL: {initial_url}")
        print(f"Engine   : {self.planner.provider.upper()}" + (f" ({self.planner.model})" if self.planner.model else ""))
        print(f"Headless : {self.headless}")
        print("="*70 + "\n")

        start_time = time.time()
        step_history = []
        overall_passed = False
        final_assertion_evaluated = False

        try:
            self.browser.start()

            # Navigate to initial URL if provided
            if initial_url:
                print(f"[Step 0] Navigating to starting URL: {initial_url}")
                self.browser.navigate(initial_url)

            for step_num in range(1, self.max_steps + 1):
                # 1. Observe
                snapshot = PageObserver.inspect(self.browser.driver)
                page_summary = PageObserver.format_summary_for_llm(snapshot)

                # 2. Plan
                print(f"[Loop {step_num}] Observing page: '{snapshot['title']}' ({snapshot['url']})")
                plan = self.planner.plan_next_action(
                    test_case=test_case,
                    page_summary=page_summary,
                    snapshot=snapshot,
                    step_history=step_history
                )

                thought = plan.get("thought", "")
                action = plan.get("action", "").lower()
                print(f"  💭 Thought : {thought}")
                print(f"  ⚡ Action  : {action.upper()} -> {plan.get('target_description') or plan.get('value')}")

                if action == "finish":
                    print("  🏁 Finish reached.")
                    if final_assertion_evaluated:
                        overall_passed = True
                    break

                # 3. Act
                result = self.executor.execute(plan, snapshot, step_num)
                step_history.append(result)

                status_mark = "✓" if result["success"] else "✗"
                print(f"  {status_mark} Result  : {result['message']} ({result['duration']}s)")

                if action == "assert":
                    final_assertion_evaluated = True
                    if result["success"]:
                        overall_passed = True
                        print("  🎯 Verification condition passed!")
                        break
                    else:
                        print("  ⚠️ Assertion failed on current step.")

                time.sleep(0.5)

            # If loop finished without explicit assert but actions succeeded and we arrived at target
            if not final_assertion_evaluated and len(step_history) > 0 and all(s["success"] for s in step_history):
                # Run automatic final check on current page state
                body = self.browser.get_body_text().lower()
                url = self.browser.get_current_url().lower()
                if "dashboard" in test_case.lower() and ("dashboard" in url or "dashboard" in body):
                    overall_passed = True
                else:
                    overall_passed = all(s["success"] for s in step_history)

        except Exception as ex:
            print(f"\n[FATAL ERROR] Unexpected exception during test execution: {ex}")
            step_history.append({
                "step_num": len(step_history) + 1,
                "action": "error",
                "target": "Execution Engine",
                "value": str(ex),
                "success": False,
                "message": f"Error: {ex}",
                "duration": 0,
                "screenshot": "",
                "url_after": self.browser.get_current_url() if self.browser else ""
            })
            overall_passed = False

        finally:
            self.browser.close()

        total_duration = round(time.time() - start_time, 2)

        report = {
            "test_case": test_case,
            "provider": self.planner.provider,
            "model": self.planner.model,
            "passed": overall_passed,
            "total_steps": len(step_history),
            "total_duration_sec": total_duration,
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "steps": step_history
        }

        self._save_reports(report)
        self._print_terminal_summary(report)

        return report

    def _save_reports(self, report: Dict[str, Any]):
        os.makedirs(self.report_dir, exist_ok=True)

        # 1. JSON Report
        json_path = os.path.join(self.report_dir, "test_report.json")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(report, f, indent=2)

        # 2. HTML Report
        html_path = os.path.join(self.report_dir, "test_report.html")
        html_content = self._render_html_report(report)
        with open(html_path, "w", encoding="utf-8") as f:
            f.write(html_content)

        print(f"\n[Artifacts] Reports generated:")
        print(f" - JSON Report : {json_path}")
        print(f" - HTML Report : {html_path}")

    def _render_html_report(self, report: Dict[str, Any]) -> str:
        badge_color = "#16a34a" if report["passed"] else "#dc2626"
        status_text = "PASSED" if report["passed"] else "FAILED"

        rows = []
        for s in report["steps"]:
            status_badge = "<span style='color:#16a34a; font-weight:bold;'>✓ PASS</span>" if s["success"] else "<span style='color:#dc2626; font-weight:bold;'>✗ FAIL</span>"
            screenshot_link = f"<a href='{s['screenshot']}' target='_blank'>View</a>" if s.get("screenshot") else "N/A"
            rows.append(f"""
            <tr>
                <td>{s['step_num']}</td>
                <td><code>{s['action'].upper()}</code></td>
                <td>{s.get('target', '')}</td>
                <td>{s.get('message', '')}</td>
                <td>{s.get('duration', 0)}s</td>
                <td>{status_badge}</td>
                <td>{screenshot_link}</td>
            </tr>
            """)

        return f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>AI Test Execution Report: {status_text}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif; background: #0f172a; color: #f8fafc; padding: 24px; margin: 0; }}
        .card {{ background: #1e293b; border-radius: 12px; padding: 24px; margin-bottom: 20px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); }}
        .badge {{ background: {badge_color}; color: #fff; padding: 6px 16px; border-radius: 20px; font-weight: bold; font-size: 1rem; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 16px; }}
        th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #334155; }}
        th {{ background: #0f172a; color: #94a3b8; font-size: 0.85rem; text-transform: uppercase; }}
        code {{ background: #334155; padding: 2px 6px; border-radius: 4px; color: #38bdf8; font-size: 0.9em; }}
        a {{ color: #38bdf8; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <h1 style="margin: 0; font-size: 1.5rem;">AI Test Automation Report</h1>
            <span class="badge">{status_text}</span>
        </div>
        <p style="margin: 12px 0 0; color: #94a3b8; font-size: 1.1rem;">
            Test Case: <strong style="color: #f8fafc;">"{report['test_case']}"</strong>
        </p>
        <div style="margin-top: 12px; color: #64748b; font-size: 0.9rem;">
            Provider: <strong>{report['provider']}</strong> | 
            Duration: <strong>{report['total_duration_sec']}s</strong> | 
            Steps: <strong>{report['total_steps']}</strong> | 
            Executed: <strong>{report['timestamp']}</strong>
        </div>
    </div>

    <div class="card">
        <h3 style="margin-top: 0;">Step-by-Step Execution Log</h3>
        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Action</th>
                    <th>Target</th>
                    <th>Outcome</th>
                    <th>Duration</th>
                    <th>Status</th>
                    <th>Evidence</th>
                </tr>
            </thead>
            <tbody>
                {''.join(rows)}
            </tbody>
        </table>
    </div>
</body>
</html>
"""

    def _print_terminal_summary(self, report: Dict[str, Any]):
        mark = "✓ PASSED" if report["passed"] else "✗ FAILED"
        color_start = "\033[92m" if report["passed"] else "\033[91m"
        color_reset = "\033[0m"

        print("\n" + "="*70)
        print("                  FINAL TEST RESULT")
        print("="*70)
        print(f"Status   : {color_start}{mark}{color_reset}")
        print(f"Test Case: \"{report['test_case']}\"")
        print(f"Steps    : {report['total_steps']}")
        print(f"Duration : {report['total_duration_sec']} seconds")
        print("="*70 + "\n")


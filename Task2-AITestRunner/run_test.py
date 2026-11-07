#!/usr/bin/env python3
import os
import sys
import argparse
from dotenv import load_dotenv

# Load local environment variables from .env if present
load_dotenv()

from agent.test_engine import TestEngine

def main():
    parser = argparse.ArgumentParser(
        description="Turn plain-English test cases into automated browser tests using AI."
    )
    parser.add_argument(
        "instruction",
        nargs="?",
        default="Log in as a customer and verify the dashboard is shown",
        help="Plain English test case description (e.g. 'Log in as a customer and verify the dashboard is shown')"
    )
    parser.add_argument(
        "--file", "-f",
        help="Path to a text file containing the plain English test case"
    )
    parser.add_argument(
        "--url", "-u",
        help="Optional initial starting URL to navigate to"
    )
    parser.add_argument(
        "--provider", "-p",
        default="auto",
        choices=["auto", "gemini", "openai", "anthropic", "autonomous"],
        help="AI provider to use (default: auto-detects from env or falls back to autonomous NLP)"
    )
    parser.add_argument(
        "--model", "-m",
        help="Model name (e.g. gemini-2.5-flash, gpt-4o-mini)"
    )
    parser.add_argument(
        "--headless",
        action="store_true",
        help="Run browser in headless mode"
    )
    parser.add_argument(
        "--timeout", "-t",
        type=int,
        default=15,
        help="Page load and interaction timeout in seconds (default: 15)"
    )
    parser.add_argument(
        "--max-steps", "-s",
        type=int,
        default=10,
        help="Maximum interaction steps allowed per test (default: 10)"
    )
    parser.add_argument(
        "--report-dir",
        default="reports",
        help="Directory where HTML/JSON reports and screenshots are saved (default: reports)"
    )

    args = parser.parse_args()

    # Determine test case text
    test_case_text = args.instruction
    if args.file:
        if not os.path.exists(args.file):
            print(f"[Error] Test case file not found: {args.file}")
            sys.exit(1)
        with open(args.file, "r", encoding="utf-8") as f:
            test_case_text = f.read().strip()

    # Determine headless mode from flag or env
    headless_mode = args.headless or os.getenv("HEADLESS", "false").lower() in ("true", "1")

    engine = TestEngine(
        provider=args.provider,
        model=args.model,
        headless=headless_mode,
        timeout=args.timeout,
        max_steps=args.max_steps,
        report_dir=args.report_dir
    )

    report = engine.run_test(
        test_case=test_case_text,
        initial_url=args.url
    )

    # Return exit code based on pass/fail
    sys.exit(0 if report["passed"] else 1)

if __name__ == "__main__":
    main()


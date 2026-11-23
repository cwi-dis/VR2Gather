#!/usr/bin/env python3
"""Turn a Unity (NUnit 3) test results file into a GitHub job summary, and fail on failures.

Usage: test_summary.py --label LABEL [--artifact-url URL] [--log-hint TEXT] RESULTS.xml [RESULTS.xml ...]

The first RESULTS file that exists is used (CiTestRunner's output location differs per platform).
Writes Markdown to $GITHUB_STEP_SUMMARY (or stdout when that isn't set): totals, and for every
failed test its message, the start of its stack trace and its log output, plus where to find the
rest (the job log and the results artifact). Exit status 1 if there is no results file, no tests
ran, or any test failed. See cwi-dis/cwipc_unity#63, #82.
"""
import argparse
import os
import sys
import xml.etree.ElementTree as ET

STACK_LINES = 12
OUTPUT_LINES = 40


def text(elem, path):
    found = elem.find(path)
    return (found.text or "").strip() if found is not None else ""


def first_lines(s, n):
    lines = s.splitlines()
    more = len(lines) - n
    return "\n".join(lines[:n]) + (f"\n… ({more} more lines)" if more > 0 else "")


def fenced(s):
    # A fence longer than any run of backticks in s, so the content can't end it.
    fence = "```"
    while fence in s:
        fence += "`"
    return f"{fence}\n{s}\n{fence}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--label", required=True)
    ap.add_argument("--artifact-url", default="")
    ap.add_argument("--log-hint", default="")
    ap.add_argument("results", nargs="+")
    args = ap.parse_args()

    out = []
    path = next((p for p in args.results if os.path.isfile(p)), None)
    status = 0
    if path is None:
        out.append(f"## ❌ {args.label}: no test results\n")
        out.append("None of these files exists: " + ", ".join(f"`{p}`" for p in args.results) + ".")
        out.append("Unity probably didn't run the tests at all; see the job log for why.")
        status = 1
    else:
        root = ET.parse(path).getroot()
        total = int(root.get("total", "0"))
        failed = int(root.get("failed", "0"))
        skipped = int(root.get("skipped", "0"))
        passed = int(root.get("passed", "0"))
        inconclusive = int(root.get("inconclusive", "0"))
        if total == 0:
            icon, status = "❌", 1
        elif failed or inconclusive:
            icon, status = "❌", 1
        else:
            icon = "✅"
        out.append(f"## {icon} {args.label}: {passed} passed, {failed} failed, {skipped} skipped"
                   + (f", {inconclusive} inconclusive" if inconclusive else "") + "\n")
        if total == 0:
            out.append("No tests ran.\n")
        bad = [tc for tc in root.iter("test-case") if tc.get("result") not in ("Passed", "Skipped")]
        for tc in bad:
            name = tc.get("fullname") or tc.get("name")
            out.append(f"### {tc.get('result')}: `{name}`\n")
            msg = text(tc, "failure/message") or text(tc, "reason/message")
            if msg:
                out.append(fenced(msg) + "\n")
            stack = text(tc, "failure/stack-trace")
            if stack:
                out.append("<details><summary>Stack trace</summary>\n\n" + fenced(first_lines(stack, STACK_LINES)) + "\n</details>\n")
            log = text(tc, "output")
            if log:
                out.append("<details><summary>Log output during this test</summary>\n\n" + fenced(first_lines(log, OUTPUT_LINES)) + "\n</details>\n")
            if args.log_hint:
                out.append(args.log_hint.replace("{test}", name) + "\n")
        skips = [tc for tc in root.iter("test-case") if tc.get("result") == "Skipped"]
        if skips:
            out.append("<details><summary>Skipped</summary>\n\n" + "\n".join(
                f"- `{tc.get('fullname')}`: {text(tc, 'reason/message')}" for tc in skips) + "\n</details>\n")
    if args.artifact_url:
        out.append(f"Complete results (NUnit XML): [artifact]({args.artifact_url}).")

    md = "\n".join(out) + "\n"
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as f:
            f.write(md)
    else:
        sys.stdout.write(md)
    if status:
        print(f"::error::{args.label}: tests failed or didn't run (see the job summary)")
    return status


if __name__ == "__main__":
    sys.exit(main())

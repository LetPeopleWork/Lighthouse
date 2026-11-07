"""Fails (exit 1) when the diff of test files since <before> weakens a test, drops an `expect(` that has no
moved-assertions row, or touches production code beyond what the design allows.

usage (from the repository root):
  python3 -I check_behaviour_free.py --before <sha> --moved <slice-X-moved-assertions.md> [--allow-extraction]

One counter everywhere: occurrences of the regular expression \\bexpect\\( . A removed line counts every occurrence
on it. A row of the moved-assertions file counts every occurrence in its "Assertion text" cell. Per file, the removed
occurrences and the occurrences the rows account for must be equal: a missing row hides a removal, and a surplus row
can stand in for one.

Each row's assertion text, stripped of surrounding whitespace, must be one of the removed lines of its file, and a
removed line can be cited by as many rows as it was removed. A row that fails this is a phantom.

The counter does not see `expect.soft(`, `expect.poll(` or `assert.`; none of them occurs in the three
ModifyTeamSettings files today. Add them to EXPECT before a file that uses them is edited.

Every git diff is taken with --no-renames, so a file moved to a new path is a deletion plus an addition and every
`expect(` in the old file needs a row. New test files that are not tracked yet are read as all-added lines.
"""

import argparse
import re
from collections import Counter
import subprocess
import sys

FRONTEND = "Lighthouse.Frontend"
SRC = f"{FRONTEND}/src"
SPECS = [f":(glob){SRC}/**/*.test.ts", f":(glob){SRC}/**/*.test.tsx", f"{SRC}/tests"]
EXPECT = re.compile(r"\bexpect\(")
NOT = re.compile(r"\.not\.")

# A partial list. The control that cannot be bypassed is the removed-`expect(` row check above; this list only
# catches the common ways of making an assertion say less.
WEAKER = re.compile(
    r"\.skip\b|\.only\b|\.todo\b|\bskipIf\b|\brunIf\b|it\.fails|\bxit\(|\bxdescribe\("
    r"|toMatchObject|objectContaining|toBeGreaterThan|toBeLessThan|toContain\(|toMatch\("
    r"|toBeTruthy|toBeDefined|\{ *timeout *:|testTimeout|setConfig|, *[0-9]{4,} *\)"
)
CALLED = re.compile(r"toHaveBeenCalled\(\)")
CALLED_WITH = re.compile(r"toHaveBeenCalledWith\(")

ALLOWED_PRODUCTION = re.compile(
    rf"{re.escape(SRC)}/pages/Reports/(printFailures|useReportsPdfExport)\.ts$"
)


def git(*args, ok=(0,)):
    done = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8")
    if done.returncode not in ok:
        sys.exit(f"git {' '.join(args)} failed: {done.stderr.strip()}")
    return done.stdout


def normalise(path):
    path = path.strip().strip("`")
    return path[len(FRONTEND) + 1:] if path.startswith(FRONTEND + "/") else path


def read_rows(moved_path):
    """Returns (rows_expects_by_file, rows_not_total, accepted_hits, row_texts_by_file)."""
    rows = {}
    texts = {}
    rows_not = 0
    accepted = set()
    section = ""
    with open(moved_path, encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("## "):
                section = line[3:].strip().lower()
                continue
            if section.startswith("accepted matcher hits"):
                text = line.strip().lstrip("-").strip().strip("`")
                if text:
                    accepted.add(text)
                continue
            if not section.startswith("moved assertions") or not line.lstrip().startswith("|"):
                continue
            cells = line.strip().strip("|").split("|")
            if len(cells) < 4 or " :: " not in cells[0] or cells[0].strip().startswith("Old id"):
                continue
            assertion = "|".join(cells[1:-2])
            file = normalise(cells[0].split(" :: ")[0])
            rows[file] = rows.get(file, 0) + len(EXPECT.findall(assertion))
            texts.setdefault(file, []).append(assertion.strip())
            rows_not += len(NOT.findall(assertion))
    return rows, rows_not, accepted, texts


def deleted_files_diff(before):
    """A deleted file has `+++ /dev/null`, so the file name must come from the `--- a/` line."""
    text = git("diff", "--no-renames", "-U0", before, "--", *SPECS)
    file_a = None
    for line in text.splitlines():
        if line.startswith("--- a/"):
            file_a = line[6:].strip()
        elif line.startswith("--- /dev/null"):
            file_a = None
        elif line.startswith("+++ b/"):
            file_a = line[6:].strip()
        elif line.startswith("+++ /dev/null"):
            pass
        elif line.startswith("diff --git"):
            file_a = None
        elif file_a and line.startswith("-"):
            yield file_a, "-", line[1:]
        elif file_a and line.startswith("+"):
            yield file_a, "+", line[1:]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--before", required=True)
    parser.add_argument("--moved", required=True)
    parser.add_argument("--allow-extraction", action="store_true",
                        help="permit printFailures.ts and useReportsPdfExport.ts (Q7 taken)")
    args = parser.parse_args()
    failures = []

    removed = {}
    added = []
    for file, sign, text in deleted_files_diff(args.before):
        if sign == "-":
            removed.setdefault(normalise(file), []).append(text)
        else:
            added.append((file, text))
    untracked = [f for f in git("ls-files", "-o", "--exclude-standard", "--", *SPECS).splitlines() if f]
    for file in untracked:
        with open(file, encoding="utf-8") as handle:
            added.extend((file, line.rstrip("\n")) for line in handle)

    rows, rows_not, accepted, texts = read_rows(args.moved)

    print(f"untracked test files read as added: {len(untracked)}")
    hits = [(f, t) for f, t in added if WEAKER.search(t) and t.strip() not in accepted]
    if hits:
        print("ADDED skip/only/todo/skipIf/runIf, a looser matcher or a longer timeout (answer each in the "
              "moved-assertions file under 'Accepted matcher hits', or take it out):")
        for file, text in hits:
            print(f"  {file}: {text.strip()}")
        failures.append("weaker matcher")

    removed_called_with = sum(len(CALLED_WITH.findall(t)) for ls in removed.values() for t in ls)
    added_called_with = sum(len(CALLED_WITH.findall(t)) for _, t in added)
    removed_called = sum(len(CALLED.findall(t)) for ls in removed.values() for t in ls)
    added_called = sum(len(CALLED.findall(t)) for _, t in added)
    if added_called > removed_called and removed_called_with > added_called_with:
        print(f"toHaveBeenCalled() added ({added_called} vs {removed_called} removed) while "
              f"toHaveBeenCalledWith( fell ({added_called_with} vs {removed_called_with}): a weaker matcher "
              "replaced a stronger one")
        failures.append("toHaveBeenCalled replaces toHaveBeenCalledWith")

    removed_not = sum(len(NOT.findall(t)) for ls in removed.values() for t in ls)
    added_not = sum(len(NOT.findall(t)) for _, t in added)
    unexplained_not = max(0, removed_not - added_not) - rows_not
    print(f".not. removed {removed_not}, added {added_not}, in rows {rows_not}")
    if unexplained_not > 0:
        print(f"  {unexplained_not} removed .not. assertion(s) have no row")
        failures.append(".not. removed without a row")

    print("removed expect( per file against the rows of the moved-assertions file:")
    for file in sorted(set(removed) | set(rows)):
        gone = sum(len(EXPECT.findall(t)) for t in removed.get(file, []))
        rowed = rows.get(file, 0)
        if gone == 0 and rowed == 0:
            continue
        verdict = "ok"
        if gone > rowed:
            verdict = f"MISSING {gone - rowed} row(s)"
            failures.append(f"{file}: removed expect( without a row")
        elif rowed > gone:
            verdict = f"SURPLUS {rowed - gone} occurrence(s) in rows that no removed line accounts for"
            failures.append(f"{file}: rows account for more expect( than were removed")
        print(f"  removed {gone:3d}  rows {rowed:3d}  {file}  {verdict}")

    print("every row's assertion text against the removed lines of its file:")
    for file in sorted(texts):
        available = Counter(t.strip() for t in removed.get(file, []))
        ever_removed = set(available)
        for text in texts[file]:
            if available[text] > 0:
                available[text] -= 1
            elif text in ever_removed:
                print(f"  {file}: cited by more rows than it was removed: {text}")
                failures.append(f"{file}: a removed line is cited by more rows than removals")
            else:
                print(f"  {file}: not a removed line of this file: {text}")
                failures.append(f"{file}: row whose assertion text is not a removed line")

    production = [f for f in git("diff", "--no-renames", "--name-only", args.before, "--", SRC).splitlines()
                  if f and not re.search(r"\.test\.tsx?$", f) and f"{SRC}/tests/" not in f]
    production += [f for f in git("ls-files", "-o", "--exclude-standard", "--", SRC).splitlines()
                   if f and not re.search(r"\.test\.tsx?$", f) and f"{SRC}/tests/" not in f]
    if not args.allow_extraction:
        offending = production
    else:
        offending = [f for f in production if not ALLOWED_PRODUCTION.search(f)]
    if offending:
        print("PRODUCTION FILES CHANGED beyond what the design allows:")
        for file in offending:
            print(f"  {file}")
        failures.append("production files changed")

    e2e = git("diff", "--no-renames", "--name-only", args.before, "--", "Lighthouse.EndToEndTests",
              ":(glob)**/*.screenshot*").splitlines()
    if e2e:
        print("E2E or screenshot files changed:", *e2e, sep="\n  ")
        failures.append("e2e files changed")

    if failures:
        print("FAIL: " + "; ".join(failures))
        return 1
    print("OK: behaviour-free checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())

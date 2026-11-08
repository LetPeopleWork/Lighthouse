"""Frontend audit: writes the blind input for the second rater (every 5th pick, no labels of the first rater).

usage: python3 -I make_rater2_input.py <picks.json> <out.md>
"""
import json
import sys

RULE = """## The labelling rule (the only guidance you get)

One word per test, by one rule: would the same assertion hold on the return value of a function that needs no DOM?

- **Logic** (L): arithmetic, formatting, ordering, filtering, validation, a rule's branches, a state change, building a link.
- **UI** (U): needs the DOM. Role or name, focus, click wiring, layout, copy in place, a MUI control's behaviour, a view's loading or failure look, routing.
- **Mixed** (M): one render checks both. Counted apart.

One example of each, judged from the test title:

| Class | Example | Why |
|---|---|---|
| Logic | `ForecastLevel.test.tsx`, "should set the level, IconComponent, and color for probability <= 50" | A probability goes in, a level comes out. No click, no layout |
| UI | `AppErrorBoundary.test.tsx`, "reloads the page when the reload button is pressed" | Needs a rendered button and a click |
| Mixed | `DateRangePresets.test.tsx`, "offers a team every window it was given, in the order it was given them" | The ordering is a rule; it is proved through a render |
"""


TIGHT = """
Tightened reading (applies to every file below):

- A test that mounts a component to get its output is never Logic, even when the assertion reads a prop handed to a mocked chart, a call count on a service or a colour value. Logic is a test that calls a function or a hook (with renderHook, no component rendered) and asserts its return.
- Mixed or UI is decided by what the assertion states. If it states a value a rule decided (a number, an order, a wording, a colour, a count of calls), it is Mixed. If it states only that something is present, shown, hidden, enabled, focused, dimmed or wired to a click, it is UI.
- A rule proved only through the DOM's text still counts as Mixed when the text is the rule's result ("says 'More than 5 years'"), and as UI when the text is fixed copy.
"""
def main():
    picks = json.load(open(sys.argv[1]))
    out = ["# Second rater: label the tests of ten files, blind", "", RULE + (TIGHT if len(sys.argv) > 3 else "")]
    out.append("## The ten files\n")
    out.append("Paths are under `Lighthouse.Frontend/src/` in `/storage/repos/Lighthouse`. Titles are in the order Vitest lists them "
               "(`describe > ... > test`). A title with `%s`, `$name` or similar is one test (a parametrised one counts once).\n")
    for n, p in enumerate(picks, 1):
        if n % 5:
            continue
        out.append(f"### File {n // 5}: `{p['file']}` ({len(p['tests'])} tests)\n")
        for i, t in enumerate(p["tests"], 1):
            out.append(f"{i}. {t}")
        out.append("")
    open(sys.argv[2], "w").write("\n".join(out) + "\n")


main()

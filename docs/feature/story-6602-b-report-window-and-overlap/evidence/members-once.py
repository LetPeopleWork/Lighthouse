#!/usr/bin/env python3
"""Check that the shared Refinement port test members are declared once, by their owner.

Usage: members-once.py REFINEMENT_TEST_FOLDER

Reads the Refinement port test files (the port base, the port builder and the
classes that derive from the port base) and counts the declarations of each
shared member: a line that starts with an access modifier and names the member
before "(", "=", ";" or the end of the line. Exits 0 only when every member is
declared exactly as often as expected and only in its owner file; otherwise it
prints each mismatch and exits 1. The host Refinement base is not read: it is
not a port class.
"""
import pathlib
import re
import sys

PORT_FILES = [
    "RefinementAtThePortTest.cs",
    "RefinementPortGivens.cs",
    "NeedOverOneCycleScenarios.cs",
    "NeedOverOneCycleSpecifications.cs",
    "Slice05NeedAndVerdictScenarios.cs",
    "Slice05NeedAndVerdictSpecifications.cs",
    "Slice10SleYardstickScenarios.cs",
    "Slice10SleYardstickSpecifications.cs",
    "Slice12CommentsScenarios.cs",
    "Slice12CommentsSpecifications.cs",
]

BASE = "RefinementAtThePortTest.cs"
BUILDER = "RefinementPortGivens.cs"

# member name pattern -> (owner file, expected number of declarations there)
EXPECTED = {
    r"GravityWithItsReadyRule": (BUILDER, 1),
    r"ReadyTag": (BUILDER, 1),
    r"GravitysCycleWorkingDays": (BASE, 1),
    r"Below": (BASE, 1),
    r"InRange": (BASE, 1),
    r"Above": (BASE, 1),
    r"NoCadence": (BASE, 1),
    r"InsufficientData": (BASE, 1),
    r"NoRefinementStates": (BASE, 1),
    r"GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle": (BASE, 1),
    r"GivenGravityHasTwoReadyAndRefinesOnThursday\w*": (BASE, 1),
    r"WhenTheCoachOpensTheRefinementTab": (BASE, 1),
    r"TheNeedOn": (BASE, 1),
    r"NeedFacts": (BASE, 1),
    r"TheTabAsSeenBy": (BASE, 2),
    r"TheTabAsReadBy": (None, 0),
    r"NeedIn": (None, 0),
    r"NeedReading": (None, 0),
}


def declarations(folder, name_pattern):
    declaration = re.compile(
        r"^\s*(public|protected|private|internal)\b[^=(]*?\b(" + name_pattern + r")\s*(\(|=|;|$)")
    found = []
    for file_name in PORT_FILES:
        path = folder / file_name
        if not path.exists():
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            if declaration.match(line):
                found.append((file_name, number, line.strip()))
    return found


def main(argv):
    if len(argv) != 2:
        print(__doc__)
        return 2
    folder = pathlib.Path(argv[1])
    failures = 0
    for name_pattern, (owner, count) in EXPECTED.items():
        found = declarations(folder, name_pattern)
        outside_owner = [f for f in found if f[0] != owner]
        if len(found) != count or outside_owner:
            failures += 1
            where = owner or "nowhere"
            print(f"WRONG  {name_pattern}: expected {count} in {where}, found {len(found)}")
            for file_name, number, line in found:
                print(f"         {file_name}:{number}: {line}")
    tab_overloads = declarations(folder, r"TheTabAsSeenBy")
    if not any(re.search(r"TheTabAsSeenBy\s*\(\s*string\b", line) for _, _, line in tab_overloads):
        failures += 1
        print("WRONG  TheTabAsSeenBy: no overload takes a browser key (string) first")
    print(f"{len(EXPECTED) + 1} checks, {failures} wrong")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

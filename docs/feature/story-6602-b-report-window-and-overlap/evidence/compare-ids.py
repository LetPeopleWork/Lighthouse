#!/usr/bin/env python3
"""Compare the test ids of a TRX run with an expected sorted id list.

Usage: compare-ids.py RUN.trx EXPECTED.txt [--write ACTUAL.txt]

Each id is "<ClassName>.<test name as NUnit reports it>". Exits 0 only when the
run holds exactly the expected ids and every one of them passed; otherwise it
prints the missing ids, the extra ids and the ids that did not pass, and exits 1.
"""
import sys
import xml.etree.ElementTree as ET

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def ids_with_outcomes(trx_path):
    root = ET.parse(trx_path).getroot()
    class_of = {}
    for unit_test in root.iter(NS + "UnitTest"):
        method = unit_test.find(NS + "TestMethod")
        class_of[unit_test.get("id")] = method.get("className").rsplit(".", 1)[-1]
    outcomes = {}
    for result in root.iter(NS + "UnitTestResult"):
        test_id = f"{class_of[result.get('testId')]}.{result.get('testName')}"
        outcomes[test_id] = result.get("outcome")
    return outcomes


def main(argv):
    if len(argv) not in (3, 5) or (len(argv) == 5 and argv[3] != "--write"):
        print(__doc__)
        return 2
    outcomes = ids_with_outcomes(argv[1])
    actual = sorted(outcomes)
    if len(argv) == 5:
        with open(argv[4], "w", encoding="utf-8") as out:
            out.write("".join(f"{test_id}\n" for test_id in actual))
    with open(argv[2], encoding="utf-8") as expected_file:
        expected = sorted(line.strip() for line in expected_file if line.strip())
    missing = sorted(set(expected) - set(actual))
    extra = sorted(set(actual) - set(expected))
    not_passed = sorted(i for i in actual if outcomes[i] != "Passed")
    for test_id in missing:
        print(f"MISSING  {test_id}")
    for test_id in extra:
        print(f"EXTRA    {test_id}")
    for test_id in not_passed:
        print(f"NOT PASSED ({outcomes[test_id]})  {test_id}")
    print(f"expected {len(expected)} ids, run has {len(actual)}: "
          f"{len(missing)} missing, {len(extra)} extra, {len(not_passed)} not passed")
    return 1 if missing or extra or not_passed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

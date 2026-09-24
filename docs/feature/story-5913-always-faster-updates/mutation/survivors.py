import json
import sys

report = json.load(open(sys.argv[1]))
for path, data in report["files"].items():
    tested = [m for m in data["mutants"] if m["status"] in ("Killed", "Timeout", "Survived")]
    killed = sum(1 for m in tested if m["status"] in ("Killed", "Timeout"))
    no_coverage = [m for m in data["mutants"] if m["status"] == "NoCoverage"]
    print(path.split("/")[-1], "tested", len(tested), "killed", killed, "nocov", len(no_coverage))
    for m in data["mutants"]:
        if m["status"] in ("Survived", "NoCoverage"):
            replacement = m.get("replacement", "")[:80].replace("\n", " ")
            print("   ", m["status"], "L%d" % m["location"]["start"]["line"], m["mutatorName"], "|", replacement)

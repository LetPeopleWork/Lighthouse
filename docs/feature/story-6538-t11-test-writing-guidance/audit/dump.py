"""Frontend audit: per-file digest for the rater.

usage: python3 -I dump.py <vitest-list.json> <picks.json> <first-pick> <last-pick> <src-root>
Prints, for each pick in range: the file, its local imports, and every test with its line,
size in lines and a count of render / interaction / screen / expect calls inside its body.
"""
import json
import re
import sys


def main():
    listing_path, picks_path, lo, hi, root = sys.argv[1:6]
    lo, hi = int(lo), int(hi)
    listing = json.load(open(listing_path))
    lines_by_file = {}
    for t in listing:
        f = t["file"].split("/Lighthouse.Frontend/src/")[1]
        lines_by_file.setdefault(f, []).append((t["location"]["line"], t["name"]))
    picks = json.load(open(picks_path))
    for n, p in enumerate(picks, 1):
        if n < lo or n > hi:
            continue
        f = p["file"]
        src = open(root + "/" + f).read().split("\n")
        print("=" * 100)
        print(f"#{n} {p['group']} pick {p['i']} @{p['pos']}: {f}  lines={len(src)}")
        for l in src:
            if re.match(r"\s*import .* from ['\"](\.|@/)", l) or re.match(r"\s*import ['\"]\.", l):
                print("   ", l.strip())
        for l in src:
            if "vi.mock(" in l:
                print("   MOCK", l.strip()[:110])
        tests = sorted(lines_by_file[f])
        for k, (ln, name) in enumerate(tests):
            end = tests[k + 1][0] if k + 1 < len(tests) else len(src) + 1
            body = "\n".join(src[ln - 1 : end - 1])
            r = len(re.findall(r"\brender(Hook)?\(|\brenderWith\w*\(|\brender[A-Z]\w*\(", body))
            ui = len(re.findall(r"userEvent|fireEvent|\.click\(|\.type\(", body))
            sc = len(re.findall(r"screen\.|within\(|getBy|findBy|queryBy", body))
            ex = len(re.findall(r"\bexpect\(", body))
            print(f"  L{ln:<4} sz={end-ln:<3} r={r} ui={ui} sc={sc} ex={ex} | {name}")


main()

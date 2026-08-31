"""Score a Stryker.NET report against only the lines a slice actually wrote.

Stryker's own `mutate` filter is ignored in this repository - config file and CLI flag alike - so a
run covers whole files and its headline score describes code this slice never touched. The line
numbers are in the report though, and so is the diff, so the two can be intersected afterwards.

Usage:
    python3 score_the_slice.py <mutation-report.json> <baseline-ref>
"""
import json
import subprocess
import sys
from collections import defaultdict

# Resolved rather than hardcoded so the script works from a worktree, where the checkout the report's
# absolute paths point at is not the main one.
REPO = subprocess.run(['git', 'rev-parse', '--show-toplevel'],
                      capture_output=True, text=True, check=True).stdout.strip()

# Mutants Stryker never ran say nothing about the tests: a compile error is an equivalent mutant, and
# an ignored one was excluded on purpose. The score is over the mutants that actually ran.
COUNTS_AS_KILLED = {'Killed', 'Timeout'}
COUNTS_AS_SURVIVED = {'Survived', 'NoCoverage'}


def changed_lines_by_file(baseline):
    """The line numbers this slice wrote, per file, read from the diff itself."""
    diff = subprocess.run(
        ['git', 'diff', '-U0', f'{baseline}..HEAD'],
        cwd=REPO, capture_output=True, text=True, check=True).stdout

    changed = defaultdict(set)
    current = None

    for line in diff.splitlines():
        if line.startswith('+++ b/'):
            current = line[6:]
        elif line.startswith('@@') and current:
            # @@ -old,count +new,count @@
            new_part = line.split('+')[1].split()[0]
            start, _, count = new_part.partition(',')
            count = int(count) if count else 1
            for offset in range(count):
                changed[current].add(int(start) + offset)

    return changed


def main():
    report_path, baseline = sys.argv[1], sys.argv[2]

    with open(report_path, encoding='utf-8') as handle:
        report = json.load(handle)

    changed = changed_lines_by_file(baseline)
    per_file = {}

    prefix = REPO.rstrip('/') + '/'

    for path, entry in report['files'].items():
        relative = path[len(prefix):] if path.startswith(prefix) else path
        if relative not in changed:
            continue

        touched = changed[relative]
        killed = survived = not_run = 0
        survivors = []

        for mutant in entry['mutants']:
            line = mutant.get('location', {}).get('start', {}).get('line')
            if line not in touched:
                continue

            status = mutant.get('status')
            if status in COUNTS_AS_KILLED:
                killed += 1
            elif status in COUNTS_AS_SURVIVED:
                survived += 1
                survivors.append((line, status, mutant.get('mutatorName'), mutant.get('replacement', '')[:60]))
            else:
                not_run += 1

        if killed or survived or not_run:
            per_file[relative] = (killed, survived, survivors, not_run)

    total_killed = sum(entry[0] for entry in per_file.values())
    total_survived = sum(entry[1] for entry in per_file.values())
    total_not_run = sum(entry[3] for entry in per_file.values())
    total = total_killed + total_survived

    print(f'{"file":70s} {"killed":>7s} {"survived":>9s} {"notrun":>7s} {"score":>7s}')
    for path, (killed, survived, _, not_run) in sorted(per_file.items()):
        ran = killed + survived
        score = f'{100.0 * killed / ran:.1f}%' if ran else 'n/a'
        print(f'{path[-70:]:70s} {killed:7d} {survived:9d} {not_run:7d} {score:>7s}')

    print()
    if total:
        print(f'SLICE SCORE: {100.0 * total_killed / total:.2f}%  ({total_killed}/{total} mutants on lines this slice wrote)')
        print(f'Not run (compile error or excluded by Stryker): {total_not_run}')
    else:
        print('No mutants fell on lines this slice wrote.')

    print()
    for path, (_, _, survivors, _) in sorted(per_file.items()):
        for line, status, mutator, replacement in survivors:
            print(f'SURVIVOR {path}:{line}  {status}  {mutator}  ->  {replacement}')


if __name__ == '__main__':
    main()

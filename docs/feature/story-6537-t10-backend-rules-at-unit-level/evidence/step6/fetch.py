"""Download test-timings-backend for each run id given on the command line, plus the Test Backend job outcome."""
import json, os, subprocess, sys

BASE = '/tmp/claude-1000/-storage-repos-Lighthouse/866d7411-3e78-408d-937c-b6dc9a6976a9/scratchpad/s6'
REPO = subprocess.run(['gh', 'repo', 'view', '--json', 'nameWithOwner', '-q', '.nameWithOwner'],
                      capture_output=True, text=True, check=True).stdout.strip()
for rid in sys.argv[1:]:
    d = os.path.join(BASE, rid)
    os.makedirs(d, exist_ok=True)
    if not os.path.exists(os.path.join(d, 'test-timings-backend.csv')):
        subprocess.run(['gh', 'run', 'download', rid, '-R', REPO, '-n', 'test-timings-backend', '-D', d],
                       capture_output=True, text=True)
    jobs = json.loads(subprocess.run(['gh', 'run', 'view', rid, '-R', REPO, '--json', 'jobs,headSha,attempt'],
                                     capture_output=True, text=True).stdout or '{}')
    json.dump(jobs, open(os.path.join(d, 'jobs.json'), 'w'))
    backend = [(j['name'], j['conclusion']) for j in jobs.get('jobs', []) if 'Backend' in j['name']]
    print(rid, jobs.get('headSha', '')[:9], 'attempt', jobs.get('attempt'), sorted(os.listdir(d)), backend, flush=True)

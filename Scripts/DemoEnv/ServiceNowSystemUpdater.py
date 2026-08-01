import argparse
import os
import random
from datetime import datetime, timedelta, timezone

import requests

INSTANCE_URL = "https://dev191338.service-now.com"
USERNAME = "admin"

# Everything this script creates carries the marker, and it only ever touches marked
# records — the PDI ships ~67 sample incidents that must stay untouched. correlation_id
# lives on `task`, so every descendant class inherits it.
MARKER = "LIGHTHOUSE_DEMO"

TRANSITION_PROBABILITY = 0.3

# Each class carries its own state choice list, and the same label is a different number in
# each: Closed is 7 on incident, 3 on change_request, 107 on problem; New is 1 / -5 / 101.
# The connector maps by LABEL, which is the only reason one team mapping covers all three
# (US #5611, ADR-123). Mapping by value would need three entries and would collide —
# change_request's Closed=3 against incident's On Hold=3.
#
# `walks`: whether this script moves the class through its states yet. Measured 2026-07-31
# against the PDI. incident: yes, a plain state PATCH works.
#
# change_request and problem are both on ITIL state models, and a state PATCH that the model
# disallows is refused by a "… Model: Check State Transition" business rule (403 on change,
# and on problem a bare 200 that silently changes nothing). Creating in-state does not work
# either — "Change Model: Apply Initial State" rewrites it back to New.
#
# change_request IS drivable and this script does not do it: set `assignment_group` and
# New -> Assess succeeds, then Assess -> Authorize -> Scheduled happen BY THEMSELVES as each
# round of sysapproval_approver records is approved — they cannot be PATCHed directly. So a
# walk means: assign, PATCH to Assess, approve, re-read, repeat, then Implement/Review/Closed.
# problem's Assess -> Root Cause Analysis is refused and unexplained.
#
# Both are seeded for class coverage and left where the instance parks them — decided
# 2026-08-01, incident alone carries the flow demo. Revisit only if the demo needs more than
# one kind of work in motion.
#
# `flow` is a branching walk rather than a chain, so On Hold happens to some incidents and not
# to all of them, which is what lets a demo team define a blocked rule matching a real subset.
# `state_extras` are the fields a state will not be entered without.
RECORD_TYPES = [
    {
        "table": "incident",
        "target_open": 25,
        "walks": True,
        "states": ["1", "2", "3", "6", "7"],
        "labels": {"1": "New", "2": "In Progress", "3": "On Hold", "6": "Resolved", "7": "Closed"},
        "state_metric_field": "incident_state",
        "flow": {
            "1": [("2", 1.0)],
            "2": [("6", 0.75), ("3", 0.25)],
            "3": [("2", 0.6), ("6", 0.4)],
            "6": [("7", 1.0)],
        },
        # The codes have to come from the instance: an invalid choice is dropped silently and
        # the mandatory-field policy then refuses the now-empty field.
        "state_extras": {
            "3": {
                "fields": {"hold_reason": None},
                "choice_element": "hold_reason",
                "fallback": "1",
            },
            "6": {
                "fields": {"close_code": None, "close_notes": "Seeded resolution."},
                "choice_element": "close_code",
                "fallback": "Solution provided",
            },
        },
        "creates": {
            "impact": lambda: str(random.randint(1, 3)),
            "urgency": lambda: str(random.randint(1, 3)),
        },
        "summaries": [
            "VPN drops during sync",
            "Printer queue stuck on floor 3",
            "Mailbox over quota",
            "Laptop will not dock",
            "SSO login loop after password reset",
            "Shared drive missing from explorer",
            "Conference room display flickers",
            "Expense tool rejects valid receipt",
        ],
    },
    {
        "table": "change_request",
        "target_open": 12,
        "walks": False,
        "states": ["-5", "-4", "-3", "-2", "-1", "0", "3"],
        "labels": {
            "-5": "New",
            "-4": "Assess",
            "-3": "Authorize",
            "-2": "Scheduled",
            "-1": "Implement",
            "0": "Review",
            "3": "Closed",
            "4": "Canceled",
        },
        "state_metric_field": "state",
        "flow": {},
        "state_extras": {
            "3": {
                "fields": {"close_code": "successful", "close_notes": "Seeded change closure."},
                "choice_element": None,
                "fallback": None,
            },
        },
        "creates": {"type": lambda: random.choice(["normal", "standard", "emergency"])},
        "summaries": [
            "Patch the payroll database",
            "Rotate the VPN concentrator certificates",
            "Add capacity to the print cluster",
            "Upgrade the SSO connector",
            "Retire the legacy file share",
            "Resize the reporting warehouse",
        ],
    },
    {
        "table": "problem",
        "target_open": 8,
        "walks": False,
        "states": ["101", "102", "103", "104", "106", "107"],
        "labels": {
            "101": "New",
            "102": "Assess",
            "103": "Root Cause Analysis",
            "104": "Fix in Progress",
            "106": "Resolved",
            "107": "Closed",
        },
        "state_metric_field": "problem_state",
        "flow": {},
        "state_extras": {
            "106": {
                "fields": {"resolution_code": "fix_applied", "cause_notes": "Seeded root cause."},
                "choice_element": None,
                "fallback": None,
            },
        },
        "creates": {},
        "summaries": [
            "Recurring VPN disconnects across the Zurich office",
            "Print spooler leaks handles under load",
            "SSO token refresh fails for long sessions",
            "Mailbox quota warnings fire a day late",
        ],
    },
]

PASSWORD_ENV_VAR = "ServiceNowLighthouseIntegrationTestToken"

parser = argparse.ArgumentParser()
parser.add_argument(
    "password",
    type=str,
    nargs="?",
    help=f"ServiceNow password for {USERNAME}; defaults to ${PASSWORD_ENV_VAR}",
)
parser.add_argument(
    "--only",
    type=str,
    default=None,
    help="Seed one table only (incident, change_request, problem) instead of all of them",
)
args = parser.parse_args()

# Prefer the environment over argv — argv is readable by any process via /proc.
password = args.password or os.environ.get(PASSWORD_ENV_VAR)
if not password:
    raise SystemExit(f"No password given: pass it as an argument or set ${PASSWORD_ENV_VAR}")

session = requests.Session()
session.auth = (USERNAME, password)
session.headers.update({"Accept": "application/json", "Content-Type": "application/json"})


def query(table, sysparm_query, fields, limit=100):
    response = session.get(
        f"{INSTANCE_URL}/api/now/table/{table}",
        params={"sysparm_query": sysparm_query, "sysparm_fields": fields, "sysparm_limit": limit},
        timeout=60,
    )
    if not response.ok:
        print(f"❌ GET {table} failed: {response.status_code} {response.text[:200]}")
        return []
    return response.json().get("result", [])


def create(table, payload):
    response = session.post(f"{INSTANCE_URL}/api/now/table/{table}", json=payload, timeout=60)
    if not response.ok:
        print(f"❌ POST {table} failed: {response.status_code} {response.text[:200]}")
        return None
    return response.json().get("result")


def value_of(field):
    return field.get("value", "") if isinstance(field, dict) else (field or "")


def move_state(table, sys_id, payload, target):
    """PATCH a state and confirm it landed. A state model can answer 200 and ignore the
    write — `problem` does exactly that — so trusting the status code would report
    transitions that never happened (measured 2026-07-31)."""
    response = session.patch(
        f"{INSTANCE_URL}/api/now/table/{table}/{sys_id}", json=payload, timeout=60
    )
    if not response.ok:
        return False, f"{response.status_code} {response.text[:120]}"

    landed = value_of(response.json().get("result", {}).get("state"))
    return (True, landed) if landed == target else (False, f"ignored, still {landed}")


_choice_cache = {}


def choice_from_instance(table, element, fallback):
    key = (table, element)
    if key not in _choice_cache:
        choices = query(
            "sys_choice", f"name={table}^element={element}^language=en", "value", limit=1
        )
        _choice_cache[key] = value_of(choices[0].get("value")) if choices else fallback
    return _choice_cache[key]


def extra_fields(spec, target):
    """The fields the instance will not let a record enter `target` without."""
    extra = spec["state_extras"].get(target)
    if not extra:
        return {}

    return {
        field: (
            choice_from_instance(spec["table"], extra["choice_element"], extra["fallback"])
            if value is None
            else value
        )
        for field, value in extra["fields"].items()
    }


def next_state(spec, current):
    candidates = spec["flow"].get(current)
    if not candidates:
        return None

    targets, weights = zip(*candidates)
    return random.choices(targets, weights=weights, k=1)[0]


def ensure_state_metric_definition(spec):
    """A class records state spans only where a Field value duration definition sits on its
    state field, and definitions never attach to `task` (ADR-123 D9). Stock ships one on
    incident and one on problem and NONE on change_request, so a fresh PDI silently shows no
    time in state for changes. Definitions record forward only — creating one here buys
    history from this moment, not backwards."""
    table, field = spec["table"], spec["state_metric_field"]

    if query(
        "metric_definition",
        f"table={table}^field={field}^type=field_value_duration",
        "sys_id,name",
        limit=1,
    ):
        print(f"  📏 {table}.{field} already carries a state duration definition")
        return

    created = create(
        "metric_definition",
        {
            "name": f"{table} State Duration (Lighthouse demo)",
            "table": table,
            "field": field,
            "type": "field_value_duration",
            "active": "true",
        },
    )

    if created:
        print(f"  📏 created the missing state duration definition on {table}.{field} — it")
        print("      records forward only, and a record has to move twice before a duration")
        print("      appears, so expect an empty column for a while")
    else:
        print(f"  ⚠️  {table}.{field} has no state duration definition and creating one failed")
        print("      — time in state stays empty for this kind of work")


def top_up(spec):
    """Bring the open population back to target. Closed records are left alone, so throughput
    accumulates instead of being reset every night."""
    table = spec["table"]
    open_states = ",".join(spec["states"][:-1])

    unfinished = query(
        table, f"correlation_id={MARKER}^stateIN{open_states}", "sys_id,number,state,opened_at"
    )
    print(f"📦 Open seeded {table}: {len(unfinished)}/{spec['target_open']}")

    created = 0
    for _ in range(spec["target_open"] - len(unfinished)):
        # opened_at is settable on insert, sys_created_on is not — backdating spreads arrivals
        # across the window so throughput and cycle time have something to measure.
        opened_at = datetime.now(timezone.utc) - timedelta(
            days=random.randint(0, 30), hours=random.randint(0, 23)
        )
        payload = {
            "short_description": f"{random.choice(spec['summaries'])} ({random.randint(100, 999)})",
            "description": "Seeded by ServiceNowSystemUpdater for the Lighthouse demo environment.",
            "correlation_id": MARKER,
            "state": spec["states"][0],
            "opened_at": opened_at.strftime("%Y-%m-%d %H:%M:%S"),
        }
        payload.update({field: make() for field, make in spec["creates"].items()})

        record = create(table, payload)
        if record:
            created += 1
            print(f"  🏗️  Created {value_of(record.get('number'))} opened {opened_at:%Y-%m-%d}")

    print(f"📊 Created {created} {table}" if created else "🚦 At capacity — skipping creation")


def walk(spec):
    """Move a share of the open population one step along the flow."""
    table, labels = spec["table"], spec["labels"]
    open_states = ",".join(spec["states"][:-1])

    in_flight = query(table, f"correlation_id={MARKER}^stateIN{open_states}", "sys_id,number,state")
    print(f"📋 {len(in_flight)} seeded {table} eligible to transition")

    transitioned = 0
    for item in in_flight:
        if random.random() >= TRANSITION_PROBABILITY:
            continue

        current = value_of(item.get("state"))
        target = next_state(spec, current)
        if not target:
            continue

        payload = {"state": target}
        payload.update(extra_fields(spec, target))

        moved, detail = move_state(table, value_of(item.get("sys_id")), payload, target)
        number = value_of(item.get("number"))
        if moved:
            transitioned += 1
            print(f"  ✅ {number} '{labels[current]}' → '{labels[target]}'")
        else:
            print(f"  ❌ {number} '{labels[current]}' → '{labels[target]}' refused: {detail}")

    print(f"📊 Transitioned {transitioned} {table}")


def seed(spec):
    print(f"\n=== {spec['table']} ===")

    ensure_state_metric_definition(spec)
    top_up(spec)

    if not spec["walks"]:
        print(f"⏭️  {spec['table']} states are driven by an ITIL state model the Table API cannot move")
        return

    walk(spec)


selected = [spec for spec in RECORD_TYPES if args.only in (None, spec["table"])]
if not selected:
    raise SystemExit(f"--only must name one of: {', '.join(s['table'] for s in RECORD_TYPES)}")

for spec in selected:
    seed(spec)

# What a task-rooted team filtered to these classes reads, and the labels it has to map.
# Anything left unmapped is work Lighthouse silently drops (#5611).
print("\n=== Seeded mix on the instance ===")
every_label = set()
for spec in selected:
    counts = {}
    for row in query(spec["table"], f"correlation_id={MARKER}", "state", limit=1000):
        state = value_of(row.get("state"))
        label = spec["labels"].get(state, state)
        counts[label] = counts.get(label, 0) + 1
    every_label.update(counts)
    breakdown = ", ".join(f"{label} {n}" for label, n in sorted(counts.items()))
    print(f"  {spec['table']:<16} {sum(counts.values()):>3} — {breakdown or '(none)'}")

print(f"\n🗺️  {len(every_label)} state labels to map: {', '.join(sorted(every_label))}")

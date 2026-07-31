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
# change_request IS drivable and this script does not do it yet: set `assignment_group` and
# New -> Assess succeeds, then Assess -> Authorize -> Scheduled happen BY THEMSELVES as each
# round of sysapproval_approver records is approved — they cannot be PATCHed directly. So a
# walk means: assign, PATCH to Assess, approve, re-read, repeat, then Implement/Review/Closed.
# problem's Assess -> Root Cause Analysis is still refused and unexplained.
#
# Until that lands, both are seeded for class coverage and left where the instance parks them.
RECORD_TYPES = [
    {
        "table": "incident",
        "target_open": 25,
        "walks": True,
        "states": ["1", "2", "6", "7"],
        "labels": {"1": "New", "2": "In Progress", "3": "On Hold", "6": "Resolved", "7": "Closed"},
        # Resolving needs close info, or the "Make close info mandatory" data policy rejects
        # the write. The code has to come from the instance: an invalid choice is dropped
        # silently and the policy then refuses the now-empty field.
        "closes_at": "6",
        "close_fields": {"close_code": None, "close_notes": "Seeded resolution."},
        "close_choice_element": "close_code",
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
        "closes_at": "3",
        "close_fields": {"close_code": "successful", "close_notes": "Seeded change closure."},
        "close_choice_element": None,
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
        "closes_at": "106",
        "close_fields": {"resolution_code": "fix_applied", "cause_notes": "Seeded root cause."},
        "close_choice_element": None,
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


def close_payload(spec):
    return {
        field: (
            choice_from_instance(spec["table"], spec["close_choice_element"], "Solution provided")
            if value is None
            else value
        )
        for field, value in spec["close_fields"].items()
    }


def seed(spec):
    table = spec["table"]
    states, labels = spec["states"], spec["labels"]
    next_state = dict(zip(states, states[1:]))
    open_states = ",".join(states[:-1])

    print(f"\n=== {table} ===")

    # --- Step 1: Get Environment State ---
    unfinished = query(
        table, f"correlation_id={MARKER}^stateIN{open_states}", "sys_id,number,state,opened_at"
    )
    print(f"📦 Open seeded {table}: {len(unfinished)}/{spec['target_open']}")

    # --- Step 2: Top Up To Target ---
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
            "state": states[0],
            "opened_at": opened_at.strftime("%Y-%m-%d %H:%M:%S"),
        }
        payload.update({field: make() for field, make in spec["creates"].items()})

        record = create(table, payload)
        if record:
            created += 1
            print(f"  🏗️  Created {value_of(record.get('number'))} opened {opened_at:%Y-%m-%d}")

    print(f"📊 Created {created} {table}" if created else "🚦 At capacity — skipping creation")

    # --- Step 3: Daily Transitions (Flow) ---
    if not spec["walks"]:
        print(f"⏭️  {table} states are driven by an ITIL state model the Table API cannot move")
        return None

    in_flight = query(table, f"correlation_id={MARKER}^stateIN{open_states}", "sys_id,number,state")
    print(f"📋 {len(in_flight)} seeded {table} eligible to transition")

    transitioned = 0
    witness = None
    for item in in_flight:
        if random.random() >= TRANSITION_PROBABILITY:
            continue

        current = value_of(item.get("state"))
        target = next_state.get(current)
        if not target:
            continue

        payload = {"state": target}
        if target == spec["closes_at"]:
            payload.update(close_payload(spec))

        moved, detail = move_state(table, value_of(item.get("sys_id")), payload, target)
        number = value_of(item.get("number"))
        if moved:
            transitioned += 1
            print(f"  ✅ {number} '{labels[current]}' → '{labels[target]}'")
            if witness is None and target == states[1]:
                witness = value_of(item.get("sys_id"))
        else:
            print(f"  ❌ {number} '{labels[current]}' → '{labels[target]}' refused: {detail}")

    print(f"📊 Transitioned {transitioned} {table}")
    return witness


def report_timestamp_evidence(table, witness):
    # A record that just left New is the honest test of whether ServiceNow records a
    # started-time we can trust, or whether the connector has to derive one (SPIKE Q4/Q6).
    fields = "number,state,opened_at,sys_created_on,work_start,work_end,resolved_at,closed_at,sys_updated_on"
    observed = query(table, f"sys_id={witness}", fields, limit=1)
    if observed:
        row = observed[0]
        populated = {k: value_of(v) for k, v in row.items() if value_of(v)}
        empty = sorted(set(fields.split(",")) - set(populated))
        print(f"\n🔬 After its first transition, {table} {value_of(row.get('number'))}:")
        for key in sorted(populated):
            print(f"     set   {key} = {populated[key]}")
        for key in empty:
            print(f"     EMPTY {key}")

    metrics = query(
        "metric_instance",
        f"id={witness}",
        "field,value,start,end,duration,calculation_complete",
        limit=20,
    )
    print(f"🔬 metric_instance rows for that record: {len(metrics)}")
    for metric in metrics:
        print(
            f"     {value_of(metric.get('field'))}={value_of(metric.get('value'))} "
            f"start={value_of(metric.get('start'))} duration={value_of(metric.get('duration'))}"
        )


selected = [spec for spec in RECORD_TYPES if args.only in (None, spec["table"])]
if not selected:
    raise SystemExit(f"--only must name one of: {', '.join(s['table'] for s in RECORD_TYPES)}")

witnesses = {spec["table"]: seed(spec) for spec in selected}

reported = next(((table, w) for table, w in witnesses.items() if w), None)
if reported:
    report_timestamp_evidence(*reported)
else:
    print("\n🔬 Nothing transitioned out of New this run — no timestamp evidence to report")

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

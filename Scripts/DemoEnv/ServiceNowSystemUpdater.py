import argparse
import os
import random
from datetime import datetime, timedelta, timezone

import requests

INSTANCE_URL = "https://dev191338.service-now.com"
USERNAME = "admin"
TABLE = "incident"

# Everything this script creates carries the marker, and it only ever touches marked
# records — the PDI ships ~67 sample incidents that must stay untouched.
MARKER = "LIGHTHOUSE_DEMO"

TARGET_OPEN_ITEMS = 25
TRANSITION_PROBABILITY = 0.3

# incident.state — distinct from task.state, where 3 means Closed Complete (see SPIKE Q10)
NEW = "1"
IN_PROGRESS = "2"
RESOLVED = "6"
CLOSED = "7"
NEXT_STATE = {NEW: IN_PROGRESS, IN_PROGRESS: RESOLVED, RESOLVED: CLOSED}
STATE_LABELS = {NEW: "New", IN_PROGRESS: "In Progress", RESOLVED: "Resolved", CLOSED: "Closed"}

SUMMARIES = [
    "VPN drops during sync",
    "Printer queue stuck on floor 3",
    "Mailbox over quota",
    "Laptop will not dock",
    "SSO login loop after password reset",
    "Shared drive missing from explorer",
    "Conference room display flickers",
    "Expense tool rejects valid receipt",
]

PASSWORD_ENV_VAR = "ServiceNowLighthouseIntegrationTestToken"

parser = argparse.ArgumentParser()
parser.add_argument(
    "password",
    type=str,
    nargs="?",
    help=f"ServiceNow password for {USERNAME}; defaults to ${PASSWORD_ENV_VAR}",
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


def patch(table, sys_id, payload):
    response = session.patch(
        f"{INSTANCE_URL}/api/now/table/{table}/{sys_id}", json=payload, timeout=60
    )
    if not response.ok:
        print(f"❌ PATCH {table}/{sys_id} failed: {response.status_code} {response.text[:200]}")
        return None
    return response.json().get("result")


def value_of(field):
    return field.get("value", "") if isinstance(field, dict) else (field or "")


# --- Step 1: Get Environment State ---
open_items = query(
    TABLE,
    f"correlation_id={MARKER}^stateIN{NEW},{IN_PROGRESS}",
    "sys_id,number,state,opened_at",
)
print(f"📦 Open seeded incidents: {len(open_items)}/{TARGET_OPEN_ITEMS}")

# --- Step 2: Top Up To Target ---
created = 0
for _ in range(TARGET_OPEN_ITEMS - len(open_items)):
    # opened_at is settable on insert, sys_created_on is not — backdating spreads arrivals
    # across the window so throughput and cycle time have something to measure.
    opened_at = datetime.now(timezone.utc) - timedelta(
        days=random.randint(0, 30), hours=random.randint(0, 23)
    )
    record = create(
        TABLE,
        {
            "short_description": f"{random.choice(SUMMARIES)} ({random.randint(100, 999)})",
            "description": "Seeded by ServiceNowSystemUpdater for the Lighthouse demo environment.",
            "correlation_id": MARKER,
            "state": NEW,
            "impact": str(random.randint(1, 3)),
            "urgency": str(random.randint(1, 3)),
            "opened_at": opened_at.strftime("%Y-%m-%d %H:%M:%S"),
        },
    )
    if record:
        created += 1
        print(f"  🏗️  Created {value_of(record.get('number'))} opened {opened_at:%Y-%m-%d}")

if created:
    print(f"📊 Created {created} incidents")
else:
    print("🚦 At capacity — skipping creation")

# --- Step 3: Daily Transitions (Flow) ---
in_flight = query(
    TABLE,
    f"correlation_id={MARKER}^stateIN{NEW},{IN_PROGRESS},{RESOLVED}",
    "sys_id,number,state",
)
print(f"📋 Found {len(in_flight)} seeded incidents eligible to transition")

transitioned = 0
witness = None
for item in in_flight:
    if random.random() >= TRANSITION_PROBABILITY:
        continue

    current = value_of(item.get("state"))
    target = NEXT_STATE.get(current)
    if not target:
        continue

    payload = {"state": target}
    if target == RESOLVED:
        payload["close_code"] = "Solved (Permanently)"
        payload["close_notes"] = "Seeded resolution."

    if patch(TABLE, value_of(item.get("sys_id")), payload):
        transitioned += 1
        number = value_of(item.get("number"))
        print(f"  ✅ {number} '{STATE_LABELS[current]}' → '{STATE_LABELS[target]}'")
        if witness is None and target == IN_PROGRESS:
            witness = value_of(item.get("sys_id"))

print(f"📊 Transitioned {transitioned} incidents")

# --- Step 4: Report Which Timestamps Actually Moved (SPIKE Q4/Q6 evidence) ---
# A record that just entered In Progress is the honest test of whether ServiceNow
# records a started-time we can trust, or whether slice 04 has to derive one.
if witness:
    fields = "number,state,opened_at,sys_created_on,work_start,work_end,resolved_at,closed_at,sys_updated_on"
    observed = query(TABLE, f"sys_id={witness}", fields, limit=1)
    if observed:
        row = observed[0]
        populated = {k: value_of(v) for k, v in row.items() if value_of(v)}
        empty = sorted(set(fields.split(",")) - set(populated))
        print(f"🔬 After entering In Progress, {value_of(row.get('number'))}:")
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
    print(f"🔬 metric_instance rows for that incident: {len(metrics)}")
    for metric in metrics:
        print(
            f"     {value_of(metric.get('field'))}={value_of(metric.get('value'))} "
            f"start={value_of(metric.get('start'))} duration={value_of(metric.get('duration'))}"
        )
else:
    print("🔬 No incident entered In Progress this run — no timestamp evidence to report")

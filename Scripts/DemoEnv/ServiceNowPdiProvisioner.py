"""Put everything the ServiceNow integration tests need into a fresh Personal Developer Instance.

ServiceNow reclaims a PDI after about ten days idle. When that happens the instance stops
answering, the connector suite fails against a dead host, and every pull request that touches the
ServiceNow connector is blocked until someone rebuilds the fixture by hand. Creating the
replacement instance is a manual step on developer.servicenow.com that cannot be automated —
everything that has to be true *inside* it afterwards is what this script does.

Two things it deliberately does not do. It never deletes records, because the stock sample data is
what most of the preconditions rest on. And it cannot backfill transition history: ServiceNow
writes a state span only when a record actually moves, so history exists from the moment the metric
definition does and not one second earlier. That is why seeding walks records through their states
rather than creating them already finished.

Usage:
    python ServiceNowPdiProvisioner.py --instance https://devNNNNNN.service-now.com
    python ServiceNowPdiProvisioner.py --instance ... --verify-only

The admin password comes from $ServiceNowLighthouseIntegrationTestToken unless --password says
otherwise. The probe accounts are given that same password, because the test fixture authenticates
all of them with the one secret.
"""

import argparse
import os
import subprocess
import sys
from pathlib import Path

import requests

# The privileged account this connects with. A Personal Developer Instance does not always call it
# `admin`, and authenticating as a user the instance does not have is answered with the same 401 a
# wrong password earns — so it is worth being able to say which one rather than assuming.
DEFAULT_ADMIN_USER = "admin"
PASSWORD_ENV_VAR = "ServiceNowLighthouseIntegrationTestToken"
INSTANCE_ENV_VAR = "ServiceNowLighthouseIntegrationTestInstance"

# The connector asks for this many rows per request. Several preconditions below are about whether
# a table lands either side of that line, because a fixture that proves paging needs more than one
# page in some places and less than one page in others.
SINGLE_PAGE = 100

# Recent ServiceNow releases block inbound basic authentication outright for any account without
# this role, and answer 401 before the password is ever checked. It grants no access to data — only
# the right to present a password over REST at all — so every account here holds it, including the
# one whose whole purpose is holding no other role.
BASIC_AUTH_ROLE = "snc_basic_auth_api_access"

# The probe accounts, and the single reason each one exists. Between them they cover the three ways
# ServiceNow answers a read the caller is not entitled to, which is the whole point of the
# connector's validation ladder: an honest refusal, a silent empty success, and a success whose row
# count disagrees with the count in the header.
PROBE_ACCOUNTS = [
    {
        "user_name": "lh_probe_none",
        "first_name": "Lighthouse",
        "last_name": "Probe (no roles)",
        "roles": [],
        "forbidden_roles": [],
        "why": "Authenticates, and every read comes back 200 with zero rows. The account that "
               "proves a connector cannot tell 'you may not see this' from 'there is nothing "
               "here'.",
    },
    {
        "user_name": "lh_probe_snc_read",
        "first_name": "Lighthouse",
        "last_name": "Probe (partial read)",
        "roles": ["sn_incident_read", "sn_change_read", "sn_request_read"],
        # Withholding exactly this one role is the asymmetry the fixture measures against:
        # incidents and problems come back as the same shape of response to this account, and only
        # the row count tells them apart.
        "forbidden_roles": ["sn_problem_read"],
        "why": "Reads incidents but not problems, so two otherwise identical answers can be told "
               "apart.",
    },
    {
        "user_name": "lh_probe_itil",
        "first_name": "Lighthouse",
        "last_name": "Probe (itil)",
        "roles": ["itil"],
        "forbidden_roles": [],
        "why": "Full service-desk grade. Used by the exploratory probes rather than the standing "
               "suite, and cheap enough to keep in step with them.",
    },
]

# A board has to carry both a table and a filter before Lighthouse can turn it into a team, and the
# filter has to select some of the table rather than all of it. `readable_filter` is the form the
# filter takes on ServiceNow's own screen; running that form matches every row, which is exactly
# why the fixture keeps a copy of it to prove that reading it would be wrong.
# The set of boards the picker will offer. Kept identical to the connector's own query, because the
# fixture reads whichever of them the instance happens to serve first.
USABLE_BOARDS = "active=true^tableISNOTEMPTY^filterISNOTEMPTY"

PROBE_BOARD = {
    "name": "Lighthouse integration probe board",
    "table": "incident",
    "filter": "active=true^priorityIN1,2",
    "readable_filter": "Active = true AND Priority is one of 1 - Critical, 2 - High",
    "description": "Created by ServiceNowPdiProvisioner so the board picker has something real to "
                   "read. Shared with admin only.",
}


class Instance:
    """A ServiceNow Table API caller that reports both what a read returned and what the instance
    says it holds. The gap between those two numbers is the signal the connector's validation is
    built on, so nothing here may collapse them into one."""

    def __init__(self, base_url, user, password):
        self.base_url = base_url.rstrip("/")
        self.user = user
        self.session = requests.Session()
        self.session.auth = (user, password)
        self.session.headers.update(
            {"Accept": "application/json", "Content-Type": "application/json"}
        )

    def read(self, table, query="", limit=1, fields="sys_id"):
        response = self.session.get(
            f"{self.base_url}/api/now/table/{table}",
            params={"sysparm_query": query, "sysparm_limit": limit, "sysparm_fields": fields},
            timeout=60,
        )
        records = response.json().get("result", []) if response.ok else []
        holds = int(response.headers.get("X-Total-Count", -1))
        return response.status_code, records, holds

    def count(self, table, query=""):
        _, _, holds = self.read(table, query)
        return holds

    def first(self, table, query="", fields="sys_id"):
        _, records, _ = self.read(table, query, limit=1, fields=fields)
        return records[0] if records else None

    def create(self, table, payload):
        response = self.session.post(
            f"{self.base_url}/api/now/table/{table}", json=payload, timeout=60
        )
        if not response.ok:
            print(f"  [FAIL] POST {table}: {response.status_code} {response.text[:200]}")
            return None
        return response.json().get("result")

    def update(self, table, sys_id, payload):
        response = self.session.patch(
            f"{self.base_url}/api/now/table/{table}/{sys_id}", json=payload, timeout=60
        )
        if not response.ok:
            print(f"  [FAIL] PATCH {table}/{sys_id}: {response.status_code} {response.text[:200]}")
            return None
        return response.json().get("result")

    def delete(self, table, sys_id):
        response = self.session.delete(
            f"{self.base_url}/api/now/table/{table}/{sys_id}", timeout=60
        )
        return response.ok


def value_of(field):
    return field.get("value", "") if isinstance(field, dict) else (field or "")


def ensure_account(admin, spec, password):
    """Create the probe account if it is missing, then make its roles match the spec exactly.

    Matching exactly matters in both directions. A missing role turns a test that should prove a
    refusal into one that proves nothing, and a surplus role does the same thing more quietly: an
    account that can suddenly read problems makes the asymmetry test pass for the wrong reason, and
    it will keep passing until someone reads the assertion."""
    user_name = spec["user_name"]
    existing = admin.first("sys_user", f"user_name={user_name}", fields="sys_id,user_name")

    if existing:
        sys_id = value_of(existing.get("sys_id"))
        print(f"  [ok] {user_name} already exists")
    else:
        created = admin.create(
            "sys_user",
            {
                "user_name": user_name,
                "first_name": spec["first_name"],
                "last_name": spec["last_name"],
                "email": f"{user_name}@lighthouse.invalid",
                "active": "true",
            },
        )
        if not created:
            return False
        sys_id = value_of(created.get("sys_id"))
        print(f"  [new] created {user_name}")

    # Note what is missing here: the password. Writing `user_password` over the Table API stores the
    # string exactly as given, without running it through the mechanism that turns a password into
    # the hash the platform compares against — so the account is left unable to authenticate, with a
    # copy of the secret sitting in the field in clear text. Measured: a working account holds 92
    # characters there, one written this way holds however many the password had.
    #
    # There is no Table API call that sets a usable password, so this does not try. The accounts are
    # created and roled here, and `password_script_for` prints what to paste to finish them.
    admin.update("sys_user", sys_id, {"active": "true", "locked_out": "false"})
    admin.update("sys_user", sys_id, {"password_needs_reset": "false"})

    if not reconcile_roles(admin, sys_id, user_name, spec):
        return False

    # Prove the account can actually be used, here, where the cause is still obvious. Left to the
    # verification pass this reads as one more red line among many, and the thing that broke it is
    # three steps back.
    status, _, _ = Instance(admin.base_url, user_name, password).read("incident", "active=true")
    if status == 401:
        print(f"  [todo] {user_name} still needs its password set on the instance")
        return False

    print(f"  [ok] {user_name} authenticates")
    return True


def password_script_for(accounts):
    """Print the one thing that has to be done on the instance itself.

    Setting another user's password is not something the Table API can do, so it is done server
    side, where assigning the field runs the hashing the platform compares against. This is the
    same shape of manual step as the role grant on the administrator account: small, one-off, and
    unavoidable rather than a shortcut not taken."""
    print("\n=== One step left, on the instance ===")
    print("These accounts exist and hold the right roles, but cannot authenticate until their")
    print("passwords are set server side:\n")
    for user_name in accounts:
        print(f"  - {user_name}")
    print("\nOpen All > System Definition > Scripts - Background, paste this, and run it.")
    print("Replace the placeholder with the same password the probes are meant to share —")
    print(f"the one in ${PASSWORD_ENV_VAR}.\n")
    print("  var password = 'PUT THE PASSWORD HERE';")
    print(f"  var names = {list(accounts)};")
    print("  names.forEach(function (name) {")
    print("      var user = new GlideRecord('sys_user');")
    print("      if (user.get('user_name', name)) {")
    print("          user.setDisplayValue('user_password', password);")
    print("          user.setValue('password_needs_reset', false);")
    print("          user.update();")
    print("          gs.info('password set for ' + name);")
    print("      }")
    print("  });")
    print("\nThen run this script again — it will verify the accounts rather than recreate them.")


def reconcile_roles(admin, user_sys_id, user_name, spec):
    _, held, _ = admin.read(
        "sys_user_has_role", f"user={user_sys_id}", limit=200, fields="sys_id,role.name"
    )
    held_names = {value_of(row.get("role.name")) for row in held}

    for role_name in [BASIC_AUTH_ROLE, *spec["roles"]]:
        if role_name in held_names:
            continue
        role = admin.first("sys_user_role", f"name={role_name}", fields="sys_id,name")
        if not role:
            print(f"  [warn] {user_name}: role '{role_name}' does not exist on this instance")
            continue
        granted = admin.create(
            "sys_user_has_role", {"user": user_sys_id, "role": value_of(role.get("sys_id"))}
        )
        if granted:
            print(f"  [new] {user_name} granted {role_name}")

    # Granting a role grants the roles it contains, so a surplus can arrive without anyone asking
    # for it. Only a directly assigned role can be taken back here; an inherited one is left alone
    # and reported by the verification pass, because removing it would mean unpicking whichever
    # role brought it in.
    for role_name in spec["forbidden_roles"]:
        for row in held:
            if value_of(row.get("role.name")) != role_name:
                continue
            if admin.delete("sys_user_has_role", value_of(row.get("sys_id"))):
                print(f"  [new] {user_name} stripped of {role_name}")

    return True


def ensure_board(admin):
    """Create the probe board and share it with admin.

    Boards are shared through membership rather than granted through roles, so a board nobody has
    been added to is invisible even to an account that can read everything else. The picker has
    nothing to offer until this membership exists."""
    existing = admin.first("vtb_board", f"name={PROBE_BOARD['name']}", fields="sys_id,name")

    if existing:
        board_id = value_of(existing.get("sys_id"))
        print("  [ok] probe board already exists")
    else:
        created = admin.create(
            "vtb_board",
            {
                "name": PROBE_BOARD["name"],
                "table": PROBE_BOARD["table"],
                "filter": PROBE_BOARD["filter"],
                "readable_filter": PROBE_BOARD["readable_filter"],
                "description": PROBE_BOARD["description"],
                "active": "true",
                "type": "flexible",
            },
        )
        if not created:
            return None
        board_id = value_of(created.get("sys_id"))
        print("  [new] created the probe board")

    admin_user = admin.first("sys_user", f"user_name={admin.user}", fields="sys_id")
    if not admin_user:
        print("  [warn] could not resolve the admin user, so the board was not shared")
        return board_id

    admin_id = value_of(admin_user.get("sys_id"))
    if not admin.first("vtb_board_member", f"board={board_id}^user={admin_id}", fields="sys_id"):
        if admin.create("vtb_board_member", {"board": board_id, "user": admin_id}):
            print(f"  [new] shared the probe board with {admin.user}")

    retire_boards_that_do_not_narrow(admin, board_id)

    return board_id


def retire_boards_that_do_not_narrow(admin, keep_board_id):
    """Deactivate any other offered board whose filter matches its whole table.

    The picker serves boards in no particular order and the fixture reads the first one, so a board
    in this shape sitting alongside the probe board makes the measurement a coin toss — and a filter
    that selects everything cannot demonstrate the thing a board is for. A stock instance ships
    upgrade-tracking boards exactly like this, which is how the fixture ended up measuring 61 rows
    out of a table of 61.

    An administrator sees every board regardless of who it is shared with, so removing a membership
    would not have been enough."""
    _, boards, _ = admin.read(
        "vtb_board", USABLE_BOARDS, limit=100, fields="sys_id,name,table,filter"
    )

    for board in boards:
        sys_id = value_of(board.get("sys_id"))
        if sys_id == keep_board_id:
            continue

        table = value_of(board.get("table"))
        selected = admin.count(table, value_of(board.get("filter")))
        whole = admin.count(table, "")
        if 0 < selected < whole:
            continue

        if admin.update("vtb_board", sys_id, {"active": "false"}):
            print(f"  [new] retired '{value_of(board.get('name'))}' from the picker — its filter "
                  f"selects {selected} of {whole} on {table}")


def run_seeder(instance_url, password, only=None):
    """Hand the record seeding to the demo-environment updater rather than growing a second copy of
    it. It already knows how to create records, walk them through their states, and add the metric
    definition that makes those walks leave a trace behind."""
    seeder = Path(__file__).with_name("ServiceNowSystemUpdater.py")
    if not seeder.exists():
        print(f"  [warn] {seeder.name} is missing, so no records were seeded")
        return

    command = [sys.executable, str(seeder), "--instance", instance_url]
    if only:
        command += ["--only", only]

    environment = dict(os.environ, **{PASSWORD_ENV_VAR: password})
    subprocess.run(command, check=False, env=environment)


def verify(admin, instance_url, password):
    """Measure every precondition and say what each one holds up.

    A failure here is meant to be actionable on its own, so each line names what was expected, what
    the instance actually reports, and what breaks if the two disagree."""
    print("\n=== Preconditions ===")
    results = []

    def check(ok, label, detail):
        results.append(ok)
        print(f"  [{'PASS' if ok else 'FAIL'}] {label}\n         {detail}")

    # Reported rather than checked, because either answer is workable and the failure it explains
    # looks nothing like its cause: once enforcement is live, an account missing the role is
    # refused with the same 401 a wrong password earns.
    enforced = admin.first(
        "sys_properties",
        "name=glide.authenticate.basic_auth.restriction.enforce",
        fields="value",
    )
    starts = admin.first(
        "sys_properties",
        "name=glide.authenticate.basic_auth.restriction.enforcement_date",
        fields="value",
    )
    print(
        f"  [note] inbound basic-auth restriction: enforce="
        f"{value_of(enforced.get('value')) if enforced else 'unset'}, "
        f"from {value_of(starts.get('value')) if starts else 'unset'} UTC. "
        f"Every account this script touches is granted {BASIC_AUTH_ROLE}."
    )

    active_incidents = admin.count("incident", "active=true")
    active_changes = admin.count("change_request", "active=true")
    all_changes = admin.count("change_request", "numberSTARTSWITHCHG")

    check(
        0 < active_incidents <= SINGLE_PAGE,
        "open incidents fit inside a single page",
        f"{active_incidents} open, needs 1..{SINGLE_PAGE} — the merged-read test proves a paging "
        f"bug by comparing a read that has to page against two that cannot",
    )
    check(
        0 < active_changes <= SINGLE_PAGE,
        "open changes fit inside a single page",
        f"{active_changes} open, needs 1..{SINGLE_PAGE} — same reason",
    )
    check(
        active_incidents + active_changes > SINGLE_PAGE,
        "open incidents and changes together need more than one page",
        f"{active_incidents + active_changes} together, needs > {SINGLE_PAGE}, otherwise the "
        f"merged read never pages and proves nothing",
    )
    check(
        all_changes > SINGLE_PAGE,
        "the change table spans more than one page",
        f"{all_changes} changes, needs > {SINGLE_PAGE}, otherwise a pager that stops after one "
        f"page still looks correct",
    )

    resolved_open = admin.count("incident", "state=6^closed_atISEMPTY")
    check(
        resolved_open > 0,
        "some incident is resolved without being closed",
        f"{resolved_open} found, needs > 0 — the only case where a finish date can come from "
        f"nowhere but the transition history",
    )

    definition = admin.first(
        "metric_definition",
        "table=incident^field=incident_state^type=field_value_duration",
        fields="sys_id,active",
    )
    check(
        definition is not None,
        "incidents record how long they spend in each state",
        "a state duration definition is present"
        if definition
        else "missing — without it no incident will ever carry history, and history cannot be "
             "backfilled",
    )

    spans = admin.count("metric_instance", "")
    check(
        spans > 0,
        "transition history exists",
        f"{spans} spans recorded, needs > 0 — records have to actually move after the definition "
        f"exists, so re-run to seed another pass rather than looking for a fault",
    )

    empty_descendant = admin.count("incident_task", "")
    check(
        empty_descendant == 0,
        "incident_task is still a kind of work the instance holds none of",
        f"{empty_descendant} records, needs 0 — if it has gained some, point the fixture at "
        f"another empty descendant rather than weakening the assertion",
    )

    people = admin.count("sys_user", "")
    people_as_work = admin.count("task", "sys_class_name=sys_user")
    check(
        people > 0 and people_as_work == 0,
        "a populated table that is not a kind of work",
        f"sys_user holds {people} rows and contributes {people_as_work} to the work hierarchy — "
        f"the case that separates 'this name resolves' from 'this name is work'",
    )

    problems = admin.count("problem", "")
    check(
        problems > 0,
        "problems exist for the partial-read account to be blind to",
        f"{problems} problems, needs > 0 — with an empty table the restricted account sees the "
        f"same nothing an entitled one would, and the asymmetry disappears",
    )

    # Every offered board, not just one of them. The picker serves them in no particular order and
    # the fixture reads whichever comes first, so a single board that fails this makes the result
    # depend on which one the instance happened to hand over.
    _, offered, _ = admin.read(
        "vtb_board", USABLE_BOARDS, limit=100, fields="sys_id,name,table,filter"
    )
    narrowing = []
    passengers = []
    for board in offered:
        table = value_of(board.get("table"))
        selected = admin.count(table, value_of(board.get("filter")))
        whole = admin.count(table, "")
        summary = f"'{value_of(board.get('name'))}' selects {selected} of {whole} on {table}"
        (narrowing if 0 < selected < whole else passengers).append(summary)

    check(
        bool(narrowing) and not passengers,
        "every board the picker offers selects part of its table",
        "; ".join(narrowing + [f"NOT NARROWING: {one}" for one in passengers])
        if offered
        else "no active board carries both a table and a filter",
    )

    for spec in PROBE_ACCOUNTS:
        probe = Instance(instance_url, spec["user_name"], password)
        status, _, _ = probe.read("incident", "active=true")
        check(
            status in (200, 403),
            f"{spec['user_name']} can authenticate",
            f"HTTP {status} reading incidents — a 401 here is the instance refusing basic "
            f"authentication before it looks at the password, so check the {BASIC_AUTH_ROLE} "
            f"grant before suspecting the secret",
        )

    no_roles = Instance(instance_url, "lh_probe_none", password)
    status, records, _ = no_roles.read("incident", "active=true")
    check(
        status == 200 and not records,
        "the no-roles account is answered with a silent empty success",
        f"HTTP {status} with {len(records)} rows — the headline bug the whole ladder exists to "
        f"catch",
    )

    status, _, _ = no_roles.read("metric_definition", "")
    check(
        status == 403,
        "the no-roles account is refused honestly somewhere",
        f"HTTP {status} on metric_definition, expected 403 — without one honest refusal, "
        f"'everything is a silent success' cannot be told from a bug in the ladder",
    )

    status, visible, holds = no_roles.read("vtb_board", "", limit=100)
    check(
        status == 200 and not visible and holds > 0,
        "boards are hidden from a non-member while still being counted",
        f"HTTP {status}, {len(visible)} visible, header says {holds} — the count is computed "
        f"before the access rules run, which is why a picker must never count from it",
    )

    restricted = Instance(instance_url, "lh_probe_snc_read", password)
    _, _, incident_holds = restricted.read("incident", "active=true")
    _, problem_rows, problem_holds = restricted.read("problem", "", limit=100)
    check(
        incident_holds > 0 and not problem_rows and problem_holds > 0,
        "the partial-read account reads incidents but not problems",
        f"incidents {incident_holds}, problems {len(problem_rows)} visible of {problem_holds} — "
        f"if problems have become readable the role grant drifted and the asymmetry is gone",
    )

    passed = sum(1 for ok in results if ok)
    print(f"\n{passed}/{len(results)} preconditions met")
    return passed == len(results)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--instance",
        default=os.environ.get(INSTANCE_ENV_VAR),
        help=f"Base URL of the PDI, e.g. https://dev340014.service-now.com; defaults to "
             f"${INSTANCE_ENV_VAR}",
    )
    parser.add_argument(
        "--password",
        default=None,
        help=f"Password for the privileged account, shared with the probe accounts; defaults to "
             f"${PASSWORD_ENV_VAR}",
    )
    parser.add_argument(
        "--admin-user",
        default=DEFAULT_ADMIN_USER,
        help=f"The privileged account to connect as (default {DEFAULT_ADMIN_USER}). Set it when "
             f"the instance names its administrator something else — the sign-in the developer "
             f"portal hands you is the one that works here.",
    )
    parser.add_argument(
        "--verify-only",
        action="store_true",
        help="Measure the preconditions without creating or changing anything",
    )
    parser.add_argument(
        "--skip-seed",
        action="store_true",
        help="Do not run the record seeder; accounts and board only",
    )
    parser.add_argument(
        "--seed-passes",
        type=int,
        default=2,
        # One pass cannot produce history on an instance that has none. The seeder creates the
        # metric definition and the records in the same run, and a record only leaves a trace
        # behind when it moves *after* the definition already exists — so the records the first
        # pass creates are walked by the second.
        help="How many times to run the seeder (default 2; a new instance needs at least two "
             "before any transition history exists)",
    )
    args = parser.parse_args()

    # Prefer the environment over argv: anything on a command line is readable by other processes.
    password = args.password or os.environ.get(PASSWORD_ENV_VAR)
    if not args.instance:
        raise SystemExit(f"No instance given: pass --instance or set ${INSTANCE_ENV_VAR}")
    if not password:
        raise SystemExit(f"No password given: pass --password or set ${PASSWORD_ENV_VAR}")

    instance_url = args.instance.rstrip("/")
    admin = Instance(instance_url, args.admin_user, password)

    print(f"Instance: {instance_url}")
    status, _, _ = admin.read("incident", "", limit=1)
    if status == 401:
        # Almost never the password on a newly created instance. ServiceNow now blocks inbound
        # basic authentication for accounts without a specific role, and that block answers 401
        # before the password is looked at — so a correct secret and a wrong one fail identically.
        # All three of these are answered with the same generic 401, so the message has to list
        # them rather than pick one. An unknown user name looks exactly like a wrong password.
        raise SystemExit(
            f"[FAIL] the instance refused '{args.admin_user}'. ServiceNow answers every one of "
            f"these the same way, so check them in order:\n"
            f"  1. Is '{args.admin_user}' the sign-in the developer portal gave you? An account "
            f"the instance does not have is refused\n"
            f"     identically to a wrong password. Pass --admin-user to use a different one.\n"
            f"  2. Does that account hold the '{BASIC_AUTH_ROLE}' role? Recent releases block "
            f"inbound basic auth without it, and\n"
            f"     the Table API cannot grant it — it is the thing being refused — so it has to "
            f"be done in the browser.\n"
            f"  3. Is the account locked out, or flagged to reset its password? Either refuses a "
            f"correct password."
        )
    if status != 200:
        raise SystemExit(
            f"[FAIL] the instance answered {status}. A 502 means it has been reclaimed or is "
            f"still waking up; create a new PDI and run this against that one."
        )

    unusable = []

    if not args.verify_only:
        print("\n=== Probe accounts ===")
        unusable = [
            spec["user_name"]
            for spec in PROBE_ACCOUNTS
            if not ensure_account(admin, spec, password)
        ]

        print("\n=== Board picker ===")
        ensure_board(admin)

        if not args.skip_seed:
            for seed_pass in range(1, max(args.seed_passes, 1) + 1):
                print(f"\n=== Records (pass {seed_pass} of {args.seed_passes}) ===")
                run_seeder(instance_url, password)

    if unusable:
        password_script_for(unusable)

    ok = verify(admin, instance_url, password)

    if ok:
        print("\nSet these so CI and local runs both find the new instance:")
        print(f"  gh secret set {INSTANCE_ENV_VAR.upper()} --body '{instance_url}'")
        print(f"  gh secret set {PASSWORD_ENV_VAR.upper()} --body '<the admin password>'")
        print("\nExport the same two locally before running the connector suite.")

    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())

import requests
import random
import argparse

LINEAR_API_URL = "https://api.linear.app/graphql"

parser = argparse.ArgumentParser()
parser.add_argument("api_token", type=str, help="Linear Token")
args = parser.parse_args()

headers = {"Content-Type": "application/json", "Authorization": args.api_token}

# --- Constants: Items to never touch ---
PROTECTED_ISSUE_IDENTIFIERS = {"lig-2", "lig-13"}
PROTECTED_PROJECT_NAMES = {"Integration Test Project"}

def execute(query, variables=None):
    res = requests.post(LINEAR_API_URL, headers=headers, json={"query": query, "variables": variables})
    response_json = res.json()
    if "errors" in response_json:
        print(f"❌ GraphQL Error: {response_json['errors']}")
        return None
    return response_json.get("data")

# --- Step 1: Get Environment State ---
data = execute("""
query {
  teams {
    nodes {
      id
      name
      states { nodes { id type name position } }
    }
  }
  initiatives {
    nodes { id name }
  }
  projects(filter: { state: { in: ["backlog", "planned", "started"] } }) {
    nodes { id name }
  }
}
""")

if not data:
    print("🛑 Could not fetch initial state.")
    exit(1)

teams = data["teams"]["nodes"]
initiatives = data["initiatives"]["nodes"]
active_projects = data["projects"]["nodes"]

print(f"📦 Active projects: {len(active_projects)}/8")

if not initiatives:
    print("⚠️  No initiatives found — projects will be created without one.")

# --- Step 2: Create a New Project if below threshold ---
if len(active_projects) < 8:
    project_name = f"Project {random.choice(['Alpha', 'Beta', 'Gamma', 'Delta', 'Epsilon', 'Zeta'])}-{random.randint(100,999)}"
    team_ids = [t["id"] for t in teams]

    res = execute("""
        mutation($name: String!, $tids: [String!]!) {
          projectCreate(input: { name: $name, teamIds: $tids, state: "planned" }) {
            project { id }
          }
        }
    """, {"name": project_name, "tids": team_ids})

    if res and res.get("projectCreate"):
        new_project_id = res["projectCreate"]["project"]["id"]
        print(f"🏗️  Spawned project: {project_name}")

        if initiatives:
            selected_initiative = random.choice(initiatives)
            link_res = execute("""
                mutation($iid: String!, $pid: String!) {
                  initiativeToProjectCreate(input: { initiativeId: $iid, projectId: $pid }) {
                    initiativeToProject { id }
                  }
                }
            """, {"iid": selected_initiative["id"], "pid": new_project_id})

            if link_res and link_res.get("initiativeToProjectCreate"):
                print(f"🔗 Linked to initiative: {selected_initiative['name']}")
            else:
                print(f"⚠️  Could not link to initiative: {selected_initiative['name']}")

        for i in range(random.randint(4, 10)):
            selected_team = random.choice(teams)
            sid = next(
                (s["id"] for s in selected_team["states"]["nodes"] if s["type"] == "unstarted"),
                None
            )
            if sid:
                execute("""
                    mutation($tid: String!, $pid: String!, $sid: String!, $title: String!) {
                      issueCreate(input: { title: $title, teamId: $tid, projectId: $pid, stateId: $sid }) { success }
                    }
                """, {"tid": selected_team["id"], "pid": new_project_id, "sid": sid, "title": f"Task {i+1}"})
else:
    print("🚦 At capacity (8 active projects) — skipping project creation")

# --- Step 3: Daily Transitions (Flow) ---
issue_data = execute("""
query {
  issues(filter: { project: { state: { in: ["backlog", "planned", "started"] } } }) {
    nodes {
      id
      identifier
      state { id type name position }
      team { states { nodes { id type name position } } }
    }
  }
}
""")

if issue_data:
    issues = issue_data["issues"]["nodes"]
    print(f"📋 Found {len(issues)} issues to potentially transition")

    transitioned = 0

    for issue in issues:
        if issue["identifier"].lower() in PROTECTED_ISSUE_IDENTIFIERS:
            print(f"  🔒 Skipping protected issue {issue['identifier']}")
            continue

        if random.random() < 0.3:
            if issue["state"]["type"] in ("completed", "cancelled"):
                continue

            all_states = sorted(issue["team"]["states"]["nodes"], key=lambda s: s["position"])
            active_states = [s for s in all_states if s["type"] not in ("completed", "cancelled")]

            current_ids = [s["id"] for s in active_states]
            try:
                idx = current_ids.index(issue["state"]["id"])
            except ValueError:
                print(f"  ⚠️  {issue['identifier']} state not in active states — skipping")
                continue

            if idx + 1 >= len(active_states):
                next_state = next((s for s in all_states if s["type"] == "completed"), None)
            else:
                next_state = active_states[idx + 1]

            if not next_state:
                continue

            result = execute(
                "mutation($id: String!, $sid: String!) { issueUpdate(id: $id, input: { stateId: $sid }) { success } }",
                {"id": issue["id"], "sid": next_state["id"]}
            )

            if result and result.get("issueUpdate", {}).get("success"):
                print(f"  ✅ {issue['identifier']} '{issue['state']['name']}' → '{next_state['name']}'")
                transitioned += 1
            else:
                print(f"  ❌ Failed to update {issue['identifier']}")

    print(f"📊 Transitioned {transitioned} issues")

# --- Step 4: Sync Project Status based on issue states ---
refresh_data = execute("""
query {
  projects(filter: { state: { in: ["backlog", "planned", "started"] } }) {
    nodes {
      id
      name
      issues { nodes { state { type } } }
    }
  }
}
""")

if refresh_data:
    for proj in refresh_data["projects"]["nodes"]:
        if proj["name"].strip().lower() in {p.lower() for p in PROTECTED_PROJECT_NAMES}:
            print(f"  🔒 Skipping protected project '{proj['name']}'")
            continue

        issue_types = [i["state"]["type"] for i in proj["issues"]["nodes"]]

        if not issue_types:
            continue

        all_done = all(t == "completed" for t in issue_types)
        any_in_progress = any(t in ("started", "completed") for t in issue_types)

        if all_done:
            new_p_state = "completed"
        elif any_in_progress:
            new_p_state = "started"
        else:
            new_p_state = "planned"

        result = execute(
            "mutation($id: String!, $s: String!) { projectUpdate(id: $id, input: { state: $s }) { success } }",
            {"id": proj["id"], "s": new_p_state}
        )

        if result and result.get("projectUpdate", {}).get("success"):
            status_emoji = "✅" if all_done else "🔄" if any_in_progress else "📌"
            print(f"  {status_emoji} Project '{proj['name']}' → '{new_p_state}'")
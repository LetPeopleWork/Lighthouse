import requests
import random
import argparse
from datetime import datetime

parser = argparse.ArgumentParser()
parser.add_argument("api_token", type=str, help="API Token for Jira")
args = parser.parse_args()

API_TOKEN = args.api_token
USERNAME = 'benjhuser@gmail.com'
JIRA_BASE_URL = 'https://letpeoplework.atlassian.net/rest/api/latest'

# Target throughput for each team including "Epics"
teams_targets = {
    "Lagunitas":     [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 1, 0, 2, 0, 1, 2, 0, 0],
    "Phoenix":       [1, 0, 0, 4, 2, 3, 1, 5, 0, 0, 2, 1, 3, 5, 0, 0, 0, 2, 0, 2, 3, 0, 1, 3, 0, 1, 0, 2, 3, 0, 0],
    "RebelRevolt":   [2, 1, 0, 4, 3, 2, 2, 4, 0, 0, 1, 1, 3, 4, 0, 0, 0, 2, 0, 2, 3, 0, 1, 0, 1, 0, 0, 1, 3, 0, 0],
    "Brownies":      [0, 2, 0, 4, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0],
    "Epics":         [0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0]
}

session = requests.Session()
session.auth = (USERNAME, API_TOKEN)
session.headers.update({'Content-Type': 'application/json'})

today = datetime.now()
day_index = today.day - 1
display_date = today.strftime('%Y-%m-%d')

print(f"🔄 Running Jira System Updater on {display_date} (day index: {day_index})")

# Track created issues by team
created_issues = {
    team: [] for team in teams_targets.keys()
}

# Track statistics for summary
stats = {
    'created': 0,
    'moved_to_in_progress': 0,
    'moved_to_done': 0
}

for team, throughput in teams_targets.items():
    count = throughput[day_index]
    
    print(f"\n📂 Processing team: {team}")
    print(f"📊 Target throughput for today: {count} item(s)")

    if count == 0:
        print(f"  ⏭️ No items to generate for '{team}' today")
        continue

    # Create new issues (stories or epics)
    print(f"🆕 Generating {count} item(s) for '{team}'...")
    
    for i in range(count):
        is_epic = team == "Epics"
        item_type = "Epic" if is_epic else "Story"
        summary = f"Auto-Generated {item_type} {i+1} - {team} - {display_date}"

        fields = {
            "project": {"key": "LGHTHSDMO"},
            "summary": summary,
            "issuetype": {"name": item_type},
        }

        if not is_epic:
            fields["labels"] = [team]

        # Create the issue
        create_resp = session.post(f"{JIRA_BASE_URL}/issue", json={"fields": fields})
        if create_resp.status_code != 201:
            print(f"  ❌ Failed to create {item_type}: {create_resp.status_code}, {create_resp.text}")
            continue

        issue_key = create_resp.json()["key"]
        created_issues[team].append(issue_key)
        print(f"  ✅ Created {item_type}: {issue_key} - {summary}")
        stats['created'] += 1


# Function to move issues stepwise
def move_issues_stepwise(team):
    is_epic = team == "Epics"
    issue_type = "Epic" if is_epic else "Story"
    label_filter = "" if is_epic else f'AND labels = "{team}"'
    jql_base = f'project = LGHTHSDMO AND issuetype = {issue_type} AND summary ~ "Auto-Generated" {label_filter}'

    # Move items from New to In Progress
    print(f"  🔍 Querying '{team}' items in 'To Do' state...")
    new_issues_resp = session.get(f"{JIRA_BASE_URL}/search", params={
        "jql": f"{jql_base} AND status = 'To Do'",
        "fields": "key",
        "maxResults": 100
    })
    
    if new_issues_resp.status_code != 200:
        print(f"  ❌ Failed to query To Do items: {new_issues_resp.status_code}, {new_issues_resp.text}")
        return
        
    new_issues = [issue["key"] for issue in new_issues_resp.json().get("issues", [])]
    print(f"  📊 Found {len(new_issues)} items in 'To Do' state")
    
    num_to_move = random.randint(0, len(new_issues))
    print(f"  🔄 Moving {num_to_move}/{len(new_issues)} items from 'To Do' to 'In Progress'")
    
    for issue_key in random.sample(new_issues, num_to_move):
        transition_resp = session.post(f"{JIRA_BASE_URL}/issue/{issue_key}/transitions", json={"transition": {"id": "21"}})
        if transition_resp.status_code == 204:
            print(f"    ➡️ Moved {issue_key} to In Progress")
            stats['moved_to_in_progress'] += 1
        else:
            print(f"    ❌ Failed to move {issue_key}: {transition_resp.status_code}, {transition_resp.text}")

    # Move items from In Progress to Done
    print(f"  🔍 Querying '{team}' items in 'In Progress' state...")
    in_progress_resp = session.get(f"{JIRA_BASE_URL}/search", params={
        "jql": f"{jql_base} AND status = 'In Progress'",
        "fields": "key",
        "maxResults": 100
    })
    
    if in_progress_resp.status_code != 200:
        print(f"  ❌ Failed to query In Progress items: {in_progress_resp.status_code}, {in_progress_resp.text}")
        return
        
    in_progress_issues = [issue["key"] for issue in in_progress_resp.json().get("issues", [])]
    print(f"  📊 Found {len(in_progress_issues)} items in 'In Progress' state")
    
    num_to_move_done = random.randint(0, len(in_progress_issues))
    print(f"  🔄 Moving {num_to_move_done}/{len(in_progress_issues)} items from 'In Progress' to 'Done'")
    
    for issue_key in random.sample(in_progress_issues, num_to_move_done):
        transition_resp = session.post(f"{JIRA_BASE_URL}/issue/{issue_key}/transitions", json={"transition": {"id": "31"}})
        if transition_resp.status_code == 204:
            print(f"    ✅ Moved {issue_key} to Done")
            stats['moved_to_done'] += 1
        else:
            print(f"    ❌ Failed to move {issue_key}: {transition_resp.status_code}, {transition_resp.text}")


# Process each team to move their items through the steps
print("\n🔄 Processing workflow transitions...")
for team in teams_targets.keys():
    print(f"\n📋 Processing workflow transitions for: {team}")
    move_issues_stepwise(team)

# Print summary
print("\n📊 Summary of operations:")
print(f"  ✅ Created: {stats['created']} items")
print(f"  ➡️ Moved to In Progress: {stats['moved_to_in_progress']} items")
print(f"  🏁 Moved to Done: {stats['moved_to_done']} items")

print("\n🏁 Jira System Update complete!")
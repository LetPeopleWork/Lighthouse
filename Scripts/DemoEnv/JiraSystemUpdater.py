import requests
import random
import argparse
from datetime import datetime, timedelta

parser = argparse.ArgumentParser()
parser.add_argument("api_token", type=str, help="API Token for Jira")
args = parser.parse_args()

API_TOKEN = args.api_token
USERNAME = 'atlassian.pushchair@huser-berta.com'
JIRA_BASE_URL = 'https://letpeoplework.atlassian.net/rest/api/3'
STORY_POINT_VALUES = [1, 2, 3, 5, 8, 13, 23]

# One Release on this board has to keep a date somebody could still forecast to, because that is what
# a Delivery can bind its date to. The other two Releases are deliberately left undated - a reader
# has to be able to see what an undated one looks like on a real board, not only in a test.
DATED_RELEASE_ID = '10006'
DATED_RELEASE_NAME = 'Elixir Project'
DAYS_THE_DATED_RELEASE_STAYS_AHEAD = 30

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
            "customfield_10037": random.choice(STORY_POINT_VALUES)
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
    new_issues_resp = session.get(f"{JIRA_BASE_URL}/search/jql", params={
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
    in_progress_resp = session.get(f"{JIRA_BASE_URL}/search/jql", params={
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

# The date is SET to a fixed distance from today, never advanced from whatever it currently is.
# Advancing would compound: a year of nightly runs would leave this Release due twelve months out,
# and the demo would be forecasting to a date nobody believes.
def keep_the_dated_release_ahead_of_today():
    release_date = (today + timedelta(days=DAYS_THE_DATED_RELEASE_STAYS_AHEAD)).strftime('%Y-%m-%d')

    resp = session.put(f"{JIRA_BASE_URL}/version/{DATED_RELEASE_ID}", json={"releaseDate": release_date})
    if resp.status_code == 200:
        print(f"  📅 '{DATED_RELEASE_NAME}' is now due {release_date}")
    else:
        print(f"  ❌ Failed to date '{DATED_RELEASE_NAME}': {resp.status_code}, {resp.text}")


print("\n📅 Keeping the dated Release ahead of today...")
keep_the_dated_release_ahead_of_today()

# Print summary
print("\n📊 Summary of operations:")
print(f"  ✅ Created: {stats['created']} items")
print(f"  ➡️ Moved to In Progress: {stats['moved_to_in_progress']} items")
print(f"  🏁 Moved to Done: {stats['moved_to_done']} items")

print("\n🏁 Jira System Update complete!")
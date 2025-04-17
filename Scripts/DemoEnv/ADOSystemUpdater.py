import requests
import base64
import argparse
from datetime import datetime, timezone
import random

# --- Configuration ---
organization_url = "https://dev.azure.com/LetPeopleWork"
project_name = "Lighthouse Demo"
dry_run = False  # Set to True to test without making real changes

# --- Parse token ---
parser = argparse.ArgumentParser()
parser.add_argument(
    "personal_access_token", type=str, help="Azure DevOps Personal Access Token"
)
args = parser.parse_args()

token_bytes = f":{args.personal_access_token}".encode("ascii")
base64_encoded_token = base64.b64encode(token_bytes).decode("ascii")

# Different content types for different operations
query_headers = {
    "Authorization": f"Basic {base64_encoded_token}",
}

patch_headers = {
    "Content-Type": "application/json-patch+json",
    "Authorization": f"Basic {base64_encoded_token}",
}

# --- Area Path Targets ---
area_path_targets = {
    "Lighthouse Demo\\Binary Blazers": [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 1, 0, 2, 0, 1, 2, 0, 0],
    "Lighthouse Demo\\Cyber Sultans": [1, 0, 0, 4, 2, 3, 1, 5, 0, 0, 2, 1, 3, 5, 0, 0, 0, 2, 0, 2, 3, 0, 1, 3, 0, 1, 0, 2, 3, 0, 0],
    "Lighthouse Demo\\Mavericks": [2, 1, 0, 4, 3, 2, 2, 4, 0, 0, 1, 1, 3, 4, 0, 0, 0, 2, 0, 2, 3, 0, 1, 0, 1, 0, 0, 1, 3, 0, 0],
    "Lighthouse Demo\\Tech Eagles": [0, 2, 0, 4, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0],
    "Lighthouse Demo\\Portfolio": [0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0]
}

today = datetime.now(timezone.utc)
today_index = today.day - 1
formatted_date = today.strftime("%Y-%m-%dT%H:%M:%S.%fZ")
display_date = today.strftime("%Y-%m-%d")


def get_random_count_from_throughput(target):
    return max(0, int(random.gauss(target, max(1, target * 0.3))))


def create_work_item(title, area_path, work_item_type):
    fields = [
        {"op": "add", "path": "/fields/System.Title", "value": title},
        {"op": "add", "path": "/fields/System.AreaPath", "value": area_path},
        {"op": "add", "path": "/fields/System.State", "value": "New"},
    ]
    if dry_run:
        print(f"[DRY-RUN] Would create {work_item_type} '{title}' in {area_path}")
        return None
    response = requests.post(
        f"{organization_url}/{project_name}/_apis/wit/workitems/${work_item_type}?api-version=6.0",
        headers=patch_headers,
        json=fields,
    )
    if response.status_code in [200, 201]:
        item = response.json()
        print(f"[Created] {work_item_type} {item['id']} - {title}")
        return item["id"]
    else:
        print(
            f"[Error] Failed to create {work_item_type}: {response.status_code} - {response.text}"
        )
        return None


def query_work_items(state, area_path, title_prefix, work_item_type):
    wiql = {
        "query": f"""
        SELECT [System.Id]
        FROM WorkItems
        WHERE [System.TeamProject] = '{project_name}'
          AND [System.WorkItemType] = '{work_item_type}'
          AND [System.State] = '{state}'
          AND [System.AreaPath] = '{area_path}'
          AND [System.Title] CONTAINS '{title_prefix}'
        """
    }

    response = requests.post(
        f"{organization_url}/{project_name}/_apis/wit/wiql?api-version=6.0",
        headers=query_headers,
        json=wiql,
    )
    if response.status_code != 200:
        print(f"[Error] WIQL query failed: {response.status_code} - {response.text}")
        return []
    return [item["id"] for item in response.json().get("workItems", [])]


def update_work_item_state(work_item_id, new_state):
    payload = [{"op": "add", "path": "/fields/System.State", "value": new_state}]
    if dry_run:
        print(f"[DRY-RUN] Would update work item {work_item_id} to '{new_state}'")
        return
    response = requests.patch(
        f"{organization_url}/_apis/wit/workitems/{work_item_id}?api-version=6.0",
        headers=patch_headers,
        json=payload,
    )
    if response.status_code == 200:
        print(f"[Updated] Work item {work_item_id} → {new_state}")
    else:
        print(
            f"[Error] Failed to update {work_item_id}: {response.status_code} - {response.text}"
        )


# --- Main loop ---
for area_path, throughput_pattern in area_path_targets.items():
    target = throughput_pattern[today_index]
    is_portfolio = "Portfolio" in area_path
    work_item_type = "Epic" if is_portfolio else "User Story"
    title_prefix = "Auto-Generated Epic" if is_portfolio else "Auto-Generated"

    # 1. Create items in "New"
    count_to_create = get_random_count_from_throughput(target)
    for i in range(count_to_create):
        title = f"{title_prefix} {display_date} #{i + 1}"
        create_work_item(title, area_path, work_item_type)

    # 2. Move some "New" to "Active"
    new_items = query_work_items("New", area_path, title_prefix, work_item_type)

    random_count = random.randint(0, len(new_items))
    to_activate = random.sample(new_items, random_count)

    for item_id in to_activate:
        update_work_item_state(item_id, "Active")

    # 3. Move some "Active" to "Closed"
    active_items = query_work_items("Active", area_path, title_prefix, work_item_type)
    random_count = random.randint(0, len(active_items))
    to_close = random.sample(active_items, random_count)

    for item_id in to_close:
        update_work_item_state(item_id, "Closed")

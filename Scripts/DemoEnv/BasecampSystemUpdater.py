import argparse
import random

import requests

LAUNCHPAD_TOKEN_URL = "https://launchpad.37signals.com/authorization/token"
LAUNCHPAD_AUTHORIZATION_URL = "https://launchpad.37signals.com/authorization.json"
REDIRECT_URI = "http://localhost:8765/callback"

TRIAGE_TARGET = 4
TODO_TARGET = 6
MOVE_CHANCE = 0.4
HOLD_CHANCE = 0.15
NOT_NOW_CHANCE = 0.05
TODO_COMPLETE_CHANCE = 0.3
TODO_REOPEN_CHANCE = 0.05

parser = argparse.ArgumentParser()
parser.add_argument("client_id", type=str, help="Launchpad app client id")
parser.add_argument("client_secret", type=str, help="Launchpad app client secret")
parser.add_argument("refresh_token", type=str, help="Launchpad refresh token")
parser.add_argument("--project", type=str, default=None, help="Project name (defaults to the first project)")
args = parser.parse_args()


# Launchpad access tokens expire after two weeks, so every run trades the long-lived refresh token for a fresh one.
def refresh_access_token():
    res = requests.post(LAUNCHPAD_TOKEN_URL, params={
        "type": "refresh",
        "refresh_token": args.refresh_token,
        "client_id": args.client_id,
        "client_secret": args.client_secret,
        "redirect_uri": REDIRECT_URI,
    }, timeout=30)
    res.raise_for_status()
    return res.json()["access_token"]


session = requests.Session()
session.headers.update({
    "Authorization": f"Bearer {refresh_access_token()}",
    "User-Agent": "Lighthouse Demo Environment (https://github.com/LetPeopleWork/Lighthouse)",
    "Content-Type": "application/json",
})


def get(url):
    res = session.get(url, timeout=30)
    res.raise_for_status()
    return res.json()


def get_all(url):
    items = []
    while url:
        res = session.get(url, timeout=30)
        res.raise_for_status()
        items.extend(res.json())
        url = res.links.get("next", {}).get("url")
    return items


def post(url, payload=None):
    res = session.post(url, json=payload or {}, timeout=30)
    if not res.ok:
        print(f"❌ POST {url} failed: {res.status_code} {res.text[:200]}")
    return res


def delete(url):
    res = session.delete(url, timeout=30)
    if not res.ok:
        print(f"❌ DELETE {url} failed: {res.status_code} {res.text[:200]}")
    return res


def random_title(prefix):
    return f"{prefix} {random.choice(['Alpha', 'Beta', 'Gamma', 'Delta', 'Epsilon', 'Zeta'])}-{random.randint(100, 999)}"


# --- Step 1: Get Environment State ---
account = get(LAUNCHPAD_AUTHORIZATION_URL)["accounts"][0]
base_url = account["href"]
print(f"🏢 Account: {account['name']}")

projects = get_all(f"{base_url}/projects.json")
project = next((p for p in projects if p["name"] == args.project), None) if args.project else projects[0]
if not project:
    print(f"🛑 Project '{args.project}' not found.")
    exit(1)

bucket_url = f"{base_url}/buckets/{project['id']}"
dock = {tool["name"]: tool for tool in project["dock"] if tool["enabled"]}
print(f"📦 Project: {project['name']} (tools: {', '.join(dock)})")

# --- Step 2: Card Table flow ---
if "kanban_board" in dock:
    card_table = get(dock["kanban_board"]["url"])
    columns = card_table["lists"]
    triage = next(c for c in columns if c["type"] == "Kanban::Triage")
    not_now = next((c for c in columns if c["type"] == "Kanban::NotNowColumn"), None)
    done = next(c for c in columns if c["type"] == "Kanban::DoneColumn")
    work_columns = [c for c in columns if c["type"] == "Kanban::Column"]
    flow = [triage] + work_columns + [done]

    # Read the whole board before changing anything, so each card makes at most one step per run.
    cards_by_column = [(idx, column, get_all(column["cards_url"])) for idx, column in enumerate(flow[:-1])]
    held_by_column = [(c, get_all(c["on_hold"]["cards_url"])) for c in work_columns if c.get("on_hold")]

    for _ in range(max(0, TRIAGE_TARGET - triage["cards_count"])):
        title = random_title("Card")
        if post(f"{bucket_url}/card_tables/lists/{triage['id']}/cards.json", {"title": title}).ok:
            print(f"🆕 Created card '{title}' in {triage['title']}")

    moved = 0
    for idx, column, cards in cards_by_column:
        for card in cards:
            roll = random.random()
            if column in work_columns and column.get("on_hold") and roll < HOLD_CHANCE:
                on_hold = column["on_hold"]
                if post(f"{bucket_url}/card_tables/cards/{card['id']}/moves.json", {"column_id": on_hold["id"]}).ok:
                    print(f"  ⏸️  '{card['title']}' put on hold in {column['title']}")
            elif not_now and column is triage and roll < NOT_NOW_CHANCE:
                if post(f"{bucket_url}/card_tables/cards/{card['id']}/moves.json", {"column_id": not_now["id"]}).ok:
                    print(f"  💤 '{card['title']}' {column['title']} → {not_now['title']}")
            elif roll < MOVE_CHANCE:
                target = flow[idx + 1]
                if post(f"{bucket_url}/card_tables/cards/{card['id']}/moves.json", {"column_id": target["id"]}).ok:
                    print(f"  ✅ '{card['title']}' {column['title']} → {target['title']}")
                    moved += 1

    for column, held_cards in held_by_column:
        for card in held_cards:
            if random.random() < MOVE_CHANCE:
                if post(f"{bucket_url}/card_tables/cards/{card['id']}/moves.json", {"column_id": column["id"]}).ok:
                    print(f"  ▶️  '{card['title']}' released from hold in {column['title']}")

    print(f"📊 Moved {moved} cards")

# --- Step 3: To-do flow ---
if "todoset" in dock:
    todoset = get(dock["todoset"]["url"])
    # To-dos can sit directly in the to-do set ("loose" to-dos) as well as in lists, so both are containers.
    todolists = [todoset] + get_all(todoset["todolists_url"])

    for todolist in todolists:
        open_todos = get_all(todolist["todos_url"])
        completed_todos = get_all(f"{todolist['todos_url']}?completed=true")
        replenish = 0 if todolist is todoset else max(0, TODO_TARGET - len(open_todos))
        for _ in range(replenish):
            content = random_title("To-do")
            if post(todolist["todos_url"], {"content": content}).ok:
                print(f"🆕 Created to-do '{content}' in {todolist['title']}")

        for todo in open_todos:
            if random.random() < TODO_COMPLETE_CHANCE:
                if post(f"{bucket_url}/todos/{todo['id']}/completion.json").ok:
                    print(f"  ☑️  Completed '{todo['content']}'")

        for todo in completed_todos:
            if random.random() < TODO_REOPEN_CHANCE:
                if delete(f"{bucket_url}/todos/{todo['id']}/completion.json").ok:
                    print(f"  ↩️  Reopened '{todo['content']}'")

"""Prints the query parameters of the operations in scope, so the validator can fill them."""
import json
import sys

doc = json.load(open(sys.argv[1]))
seen = {}
for path, item in doc["paths"].items():
    if "/teams/{teamId}/metrics" not in path and "Deliveries/portfolio" not in path:
        continue
    op = item.get("get")
    if not op:
        continue
    for p in op.get("parameters", []):
        if p["in"] == "query":
            key = p["name"]
            seen.setdefault(key, set()).add(json.dumps(p.get("schema")))
for k, v in seen.items():
    print(k, v)

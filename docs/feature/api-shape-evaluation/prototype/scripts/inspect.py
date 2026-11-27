"""Lists the operations in scope and the shape of their 200 answer in the OpenAPI document."""
import json
import sys

doc = json.load(open(sys.argv[1]))
print("openapi", doc.get("openapi"), "paths", len(doc["paths"]), "schemas", len(doc.get("components", {}).get("schemas", {})))
needles = [n.lower() for n in sys.argv[2:]] or ["/teams/{id}/metrics", "deliveries/portfolio"]
for path, item in doc["paths"].items():
    if not any(n in path.lower() for n in needles):
        continue
    for method, op in item.items():
        responses = op.get("responses", {})
        ok = responses.get("200", {})
        schema = None
        for _, media in ok.get("content", {}).items():
            schema = media.get("schema")
            break
        shape = json.dumps(schema)[:110] if schema else "-- NO 200 SCHEMA"
        print(method.upper(), path, "|", shape, "|", list(responses.keys()))

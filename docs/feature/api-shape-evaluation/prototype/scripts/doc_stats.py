"""Counts how the whole OpenAPI document describes enums, nullability, dates and untyped answers."""
import json
import sys

doc = json.load(open(sys.argv[1]))
schemas = doc.get("components", {}).get("schemas", {})

enums = {n: s for n, s in schemas.items() if "enum" in s}
print("enum schemas:", len(enums))
for n, s in list(enums.items())[:8]:
    print("  ", n, s.get("type"), s.get("enum")[:6])

nullable = 0
props = 0
required_lists = 0
for n, s in schemas.items():
    if s.get("required"):
        required_lists += 1
    for p, ps in s.get("properties", {}).items():
        props += 1
        if ps.get("nullable"):
            nullable += 1
print("properties:", props, "nullable:", nullable, "schemas with a required list:", required_lists, "of", len(schemas))

ops = 0
no_schema = []
for path, item in doc["paths"].items():
    for method, op in item.items():
        ops += 1
        ok = op.get("responses", {}).get("200")
        if not ok:
            continue
        if not ok.get("content"):
            no_schema.append(f"{method.upper()} {path}")
print("operations:", ops, "200 without a body schema:", len(no_schema))
gets = [x for x in no_schema if x.startswith("GET")]
print("  of which GET:", len(gets))
for g in gets:
    print("   ", g)

# schemas typed as a bare object (ActionResult<object>, IActionResult with Ok(x))
bare = []
for path, item in doc["paths"].items():
    for method, op in item.items():
        ok = op.get("responses", {}).get("200", {})
        for _, media in ok.get("content", {}).items():
            sch = media.get("schema", {})
            if sch.get("type") == "object" and "properties" not in sch and "$ref" not in sch:
                bare.append(f"{method.upper()} {path}")
print("200 typed as bare object:", len(bare))
for b in bare:
    print("   ", b)

poly = [n for n, s in schemas.items() if "oneOf" in s or "anyOf" in s or "discriminator" in s]
print("polymorphic schemas:", poly)

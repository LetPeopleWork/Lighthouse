"""PROTOTYPE: adds the three Swashbuckle settings to Program.cs of the SCRATCH worktree only."""
import sys

path = sys.argv[1]
text = open(path).read()
old = "                c.DocumentFilter<LatestRouteFilter>();\n"
new = old + (
    "                c.SupportNonNullableReferenceTypes();\n"
    "                c.UseAllOfToExtendReferenceSchemas();\n"
    "                c.SchemaFilter<API.Swagger.AlwaysWrittenIsRequiredFilter>();\n"
)
assert text.count(old) == 1
open(path, "w").write(text.replace(old, new))
print("patched", path)

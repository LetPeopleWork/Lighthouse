# Backend fix tried in a scratch worktree (NOT kept in the code)

Applied to a throwaway `git worktree` at HEAD 5c26f30ee under the scratchpad, built, run once to save
`../openapi/fixed-annotations.json`, then removed. Nothing in the real `Lighthouse.Backend` changed.

## Program.cs, inside `AddSwaggerGen` (3 added lines; applied by `../scripts/patch-program.py`)

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<LatestRouteFilter>();
    c.SupportNonNullableReferenceTypes();      // string vs string? from the C# nullable annotations
    c.UseAllOfToExtendReferenceSchemas();      // lets a nullable $ref (class or enum) say "nullable"
    c.SchemaFilter<API.Swagger.AlwaysWrittenIsRequiredFilter>();
});
```

## API/Swagger/AlwaysWrittenIsRequiredFilter.cs (new, ~45 lines)

```csharp
// System.Text.Json writes every property of an answer unless it carries a JsonIgnore, so those
// properties are always present on the wire: mark them "required". Whether a value may be null is a
// separate question, answered by SupportNonNullableReferenceTypes. Request types are left alone,
// because a client may leave their properties out.
public class AlwaysWrittenIsRequiredFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is null || context.Type is null) return;
        if (context.Type.Name.EndsWith("Request", StringComparison.Ordinal)) return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;
            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concrete.Properties.ContainsKey(name)) continue;
            concrete.Required ??= new HashSet<string>();
            concrete.Required.Add(name);
        }
    }
}
```

The `...Request` name rule is a prototype shortcut. A real version should decide "answer type or
request type" from how the type is used (Swashbuckle shares one schema for both when a DTO is used in
both directions), or use separate request DTOs, which Lighthouse mostly does already.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RepairIncompleteForecastFilterConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The forecast filter editor persisted a rule row the moment it was added, before
            // anyone typed a value into it, exactly as the blocked editor did. Those conditions are
            // now rejected when the settings are saved, which would leave a team unable to save any
            // setting at all until it cleared a row the screen shows as blank. Same repair as the
            // blocked rule sets, on the column the earlier migration did not cover.
            migrationBuilder.Sql(@"
                UPDATE ""Teams""
                SET ""ForecastFilterRuleSetJson"" = (
                    SELECT CASE
                        WHEN COUNT(*) = 0 THEN NULL
                        ELSE jsonb_build_object(
                            'version', COALESCE(""Teams"".""ForecastFilterRuleSetJson""::jsonb -> 'version', to_jsonb(1)),
                            'mode', COALESCE(""Teams"".""ForecastFilterRuleSetJson""::jsonb -> 'mode', to_jsonb('and'::text)),
                            'conditions', jsonb_agg(kept.condition))::text
                    END
                    FROM jsonb_array_elements(""Teams"".""ForecastFilterRuleSetJson""::jsonb -> 'conditions') AS kept(condition)
                    WHERE NOT (
                        COALESCE(kept.condition ->> 'value', '') = ''
                        AND lower(COALESCE(kept.condition ->> 'operator', '')) IN ('equals', 'notequals', 'contains', 'notcontains'))
                )
                WHERE ""ForecastFilterRuleSetJson"" IS NOT NULL
                  AND ""ForecastFilterRuleSetJson"" LIKE '{%'
                  AND jsonb_typeof(""ForecastFilterRuleSetJson""::jsonb -> 'conditions') = 'array'
                  AND EXISTS (
                      SELECT 1
                      FROM jsonb_array_elements(""ForecastFilterRuleSetJson""::jsonb -> 'conditions') AS stored(condition)
                      WHERE COALESCE(stored.condition ->> 'value', '') = ''
                        AND lower(COALESCE(stored.condition ->> 'operator', '')) IN ('equals', 'notequals', 'contains', 'notcontains'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the conditions removed here carried no value and therefore no
            // information, so there is nothing to put back. No schema changed in Up() either.
        }
    }
}

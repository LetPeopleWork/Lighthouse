using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                UPDATE Teams
                SET ForecastFilterRuleSetJson = (
                    SELECT CASE
                        WHEN json_array_length(kept.conditions) = 0 THEN NULL
                        ELSE json_object(
                            'version', COALESCE(json_extract(Teams.ForecastFilterRuleSetJson, '$.version'), 1),
                            'mode', COALESCE(json_extract(Teams.ForecastFilterRuleSetJson, '$.mode'), 'and'),
                            'conditions', json(kept.conditions))
                    END
                    FROM (
                        SELECT json_group_array(json(stored.value)) AS conditions
                        FROM json_each(Teams.ForecastFilterRuleSetJson, '$.conditions') stored
                        WHERE NOT (
                            COALESCE(json_extract(stored.value, '$.value'), '') = ''
                            AND lower(COALESCE(json_extract(stored.value, '$.operator'), '')) IN ('equals', 'notequals', 'contains', 'notcontains'))
                    ) kept
                )
                WHERE ForecastFilterRuleSetJson IS NOT NULL
                  AND json_valid(ForecastFilterRuleSetJson)
                  AND EXISTS (
                      SELECT 1
                      FROM json_each(ForecastFilterRuleSetJson, '$.conditions') stored
                      WHERE COALESCE(json_extract(stored.value, '$.value'), '') = ''
                        AND lower(COALESCE(json_extract(stored.value, '$.operator'), '')) IN ('equals', 'notequals', 'contains', 'notcontains'));
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

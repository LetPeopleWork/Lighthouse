using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RepairIncompleteBlockedRuleConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RepairStatementFor("Teams"));
            migrationBuilder.Sql(RepairStatementFor("Portfolios"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the conditions removed here carried no value and therefore no
            // information, so there is nothing to put back. No schema changed in Up() either.
        }

        /// <summary>
        /// Drops every blocked condition that compares against a value but was stored without one.
        /// The earlier backfill copied the legacy blocked states and tags across one-for-one, empty
        /// entries included, which left rules that read as blank on screen and — for a contains rule
        /// — marked every item as blocked. Rules whose operator needs no value (is empty, is not
        /// empty) are kept. A rule set left with no conditions becomes NULL, the single form the
        /// rest of the application already reads as "no blocked definition".
        /// </summary>
        private static string RepairStatementFor(string table)
        {
            return $@"
                UPDATE {table}
                SET BlockedRuleSetJson = (
                    SELECT CASE
                        WHEN json_array_length(kept.conditions) = 0 THEN NULL
                        ELSE json_object(
                            'version', COALESCE(json_extract({table}.BlockedRuleSetJson, '$.version'), 1),
                            'mode', COALESCE(json_extract({table}.BlockedRuleSetJson, '$.mode'), 'and'),
                            'conditions', json(kept.conditions))
                    END
                    FROM (
                        SELECT json_group_array(json(stored.value)) AS conditions
                        FROM json_each({table}.BlockedRuleSetJson, '$.conditions') stored
                        WHERE NOT (
                            COALESCE(json_extract(stored.value, '$.value'), '') = ''
                            AND lower(COALESCE(json_extract(stored.value, '$.operator'), '')) IN ('equals', 'notequals', 'contains', 'notcontains'))
                    ) kept
                )
                WHERE BlockedRuleSetJson IS NOT NULL
                  AND json_valid(BlockedRuleSetJson)
                  AND EXISTS (
                      SELECT 1
                      FROM json_each(BlockedRuleSetJson, '$.conditions') stored
                      WHERE COALESCE(json_extract(stored.value, '$.value'), '') = ''
                        AND lower(COALESCE(json_extract(stored.value, '$.operator'), '')) IN ('equals', 'notequals', 'contains', 'notcontains'));
            ";
        }
    }
}

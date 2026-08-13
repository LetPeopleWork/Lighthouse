using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                UPDATE ""{table}""
                SET ""BlockedRuleSetJson"" = (
                    SELECT CASE
                        WHEN COUNT(*) = 0 THEN NULL
                        ELSE jsonb_build_object(
                            'version', COALESCE(""{table}"".""BlockedRuleSetJson""::jsonb -> 'version', to_jsonb(1)),
                            'mode', COALESCE(""{table}"".""BlockedRuleSetJson""::jsonb -> 'mode', to_jsonb('and'::text)),
                            'conditions', jsonb_agg(kept.condition))::text
                    END
                    FROM jsonb_array_elements(""{table}"".""BlockedRuleSetJson""::jsonb -> 'conditions') AS kept(condition)
                    WHERE NOT (
                        COALESCE(kept.condition ->> 'value', '') = ''
                        AND lower(COALESCE(kept.condition ->> 'operator', '')) IN ('equals', 'notequals', 'contains', 'notcontains'))
                )
                WHERE ""BlockedRuleSetJson"" IS NOT NULL
                  AND ""BlockedRuleSetJson"" LIKE '{{%'
                  AND jsonb_typeof(""BlockedRuleSetJson""::jsonb -> 'conditions') = 'array'
                  AND EXISTS (
                      SELECT 1
                      FROM jsonb_array_elements(""BlockedRuleSetJson""::jsonb -> 'conditions') AS stored(condition)
                      WHERE COALESCE(stored.condition ->> 'value', '') = ''
                        AND lower(COALESCE(stored.condition ->> 'operator', '')) IN ('equals', 'notequals', 'contains', 'notcontains'));
            ";
        }
    }
}

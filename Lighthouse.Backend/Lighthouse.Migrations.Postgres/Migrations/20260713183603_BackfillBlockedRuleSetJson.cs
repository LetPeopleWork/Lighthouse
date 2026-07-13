using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBlockedRuleSetJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Teams"" AS t
                SET ""BlockedRuleSetJson"" = (
                    SELECT jsonb_build_object(
                        'version', 1,
                        'mode', 'or',
                        'conditions', COALESCE(jsonb_agg(cond), '[]'::jsonb)
                    )::text
                    FROM (
                        SELECT jsonb_build_object('fieldKey', 'workitem.state', 'operator', 'equals', 'value', s) AS cond
                        FROM unnest(t.""BlockedStates"") AS s
                        UNION ALL
                        SELECT jsonb_build_object('fieldKey', 'workitem.tags', 'operator', 'contains', 'value', tg) AS cond
                        FROM unnest(t.""BlockedTags"") AS tg
                    ) conds
                )
                WHERE t.""BlockedRuleSetJson"" IS NULL
                  AND (cardinality(t.""BlockedStates"") > 0 OR cardinality(t.""BlockedTags"") > 0);
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Portfolios"" AS t
                SET ""BlockedRuleSetJson"" = (
                    SELECT jsonb_build_object(
                        'version', 1,
                        'mode', 'or',
                        'conditions', COALESCE(jsonb_agg(cond), '[]'::jsonb)
                    )::text
                    FROM (
                        SELECT jsonb_build_object('fieldKey', 'feature.state', 'operator', 'equals', 'value', s) AS cond
                        FROM unnest(t.""BlockedStates"") AS s
                        UNION ALL
                        SELECT jsonb_build_object('fieldKey', 'feature.tags', 'operator', 'contains', 'value', tg) AS cond
                        FROM unnest(t.""BlockedTags"") AS tg
                    ) conds
                )
                WHERE t.""BlockedRuleSetJson"" IS NULL
                  AND (cardinality(t.""BlockedStates"") > 0 OR cardinality(t.""BlockedTags"") > 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: reversing the backfill would require distinguishing rows this
            // migration populated from rows a user (or a later feature) set BlockedRuleSetJson on
            // afterwards, which is not safely derivable from stored state. No schema changed in Up(),
            // so there is nothing else to roll back.

        }
    }
}

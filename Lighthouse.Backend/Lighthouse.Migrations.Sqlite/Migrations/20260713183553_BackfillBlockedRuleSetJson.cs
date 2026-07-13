using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBlockedRuleSetJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Teams
                SET BlockedRuleSetJson = (
                    SELECT json_object(
                        'version', 1,
                        'mode', 'or',
                        'conditions', json(
                            (SELECT json_group_array(json(cond))
                             FROM (
                                SELECT json_object('fieldKey', 'workitem.state', 'operator', 'equals', 'value', je.value) AS cond
                                FROM json_each(Teams.BlockedStates) je
                                UNION ALL
                                SELECT json_object('fieldKey', 'workitem.tags', 'operator', 'contains', 'value', je.value) AS cond
                                FROM json_each(Teams.BlockedTags) je
                             )
                            )
                        )
                    )
                )
                WHERE BlockedRuleSetJson IS NULL
                  AND (json_array_length(BlockedStates) > 0 OR json_array_length(BlockedTags) > 0);
            ");

            migrationBuilder.Sql(@"
                UPDATE Portfolios
                SET BlockedRuleSetJson = (
                    SELECT json_object(
                        'version', 1,
                        'mode', 'or',
                        'conditions', json(
                            (SELECT json_group_array(json(cond))
                             FROM (
                                SELECT json_object('fieldKey', 'feature.state', 'operator', 'equals', 'value', je.value) AS cond
                                FROM json_each(Portfolios.BlockedStates) je
                                UNION ALL
                                SELECT json_object('fieldKey', 'feature.tags', 'operator', 'contains', 'value', je.value) AS cond
                                FROM json_each(Portfolios.BlockedTags) je
                             )
                            )
                        )
                    )
                )
                WHERE BlockedRuleSetJson IS NULL
                  AND (json_array_length(BlockedStates) > 0 OR json_array_length(BlockedTags) > 0);
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

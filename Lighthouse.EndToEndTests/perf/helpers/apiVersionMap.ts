export type RefreshLogRow = {
	id: number;
	type: string | number;
	entityId: number;
	entityName: string;
	itemCount: number;
	durationMs: number;
	executedAt: string;
	success: boolean;
	cancelled: boolean;
	skipped?: boolean;
};

export type RoundVerdict = "computed" | "skipped";

// v26.10.19.10 predates rounds that skip an unchanged forecast: its Refresh History rows carry no "skipped"
// field because every round it runs computes. That field is the only shape the seeder and the rounds differ
// on between that tag and main.
export function verdictOf(row: RefreshLogRow): RoundVerdict {
	return row.skipped === true ? "skipped" : "computed";
}

export function canSkipRounds(row: RefreshLogRow): boolean {
	return "skipped" in row;
}

const refreshTypes = ["Team", "Portfolio", "Forecast"];

export function isRowOf(
	row: RefreshLogRow,
	type: "Team" | "Portfolio" | "Forecast",
	entityId: number,
): boolean {
	const rowType =
		typeof row.type === "number" ? refreshTypes[row.type] : row.type;
	return rowType === type && row.entityId === entityId;
}

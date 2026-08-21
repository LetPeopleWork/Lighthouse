export interface RefreshLog {
	id: number;
	type: "Team" | "Portfolio" | "Forecast";
	entityId: number;
	entityName: string;
	itemCount: number;
	durationMs: number;
	executedAt: string;
	success: boolean;
}

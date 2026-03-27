export interface RefreshLog {
	id: number;
	type: "Team" | "Portfolio";
	entityId: number;
	entityName: string;
	itemCount: number;
	durationMs: number;
	executedAt: string;
	success: boolean;
}

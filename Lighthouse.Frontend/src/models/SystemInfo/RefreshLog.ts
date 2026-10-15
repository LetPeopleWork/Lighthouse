export interface RefreshLog {
	id: number;
	type: "Team" | "Portfolio" | "Forecast";
	entityId: number;
	entityName: string;
	itemCount: number;
	durationMs: number;
	executedAt: string;
	success: boolean;
	cancelled: boolean;
	// Optional because a backend older than this field sends a payload without it, and a newer UI
	// still has to render that payload rather than fail on it.
	recordsWhoseLinksNamedMoreThanOneParent?: number;
	// SCAFFOLD: the backend does not send this yet, and nothing reads it yet. Optional for the same
	// reason as the field above.
	recordsPlacedFromLinksNamingMoreThanOneParent?: number;
}

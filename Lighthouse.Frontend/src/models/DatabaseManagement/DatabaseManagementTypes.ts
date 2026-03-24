export interface IDatabaseOperationStatus {
	readonly operationId: string;
	readonly operationType: "Backup" | "Restore" | "Clear";
	readonly state:
		| "Requested"
		| "PendingBehindBackup"
		| "Admitted"
		| "Executing"
		| "RestartPending"
		| "RestartComplete"
		| "Completed"
		| "Failed";
	readonly failureReason: string | null;
}

export interface IDatabaseCapabilityStatus {
	readonly provider: string;
	readonly isOperationBlocked: boolean;
	readonly blockedReason: string | null;
	readonly isToolingAvailable: boolean;
	readonly toolingGuidanceMessage: string | null;
	readonly toolingGuidanceUrl: string | null;
	readonly activeOperation: IDatabaseOperationStatus | null;
}

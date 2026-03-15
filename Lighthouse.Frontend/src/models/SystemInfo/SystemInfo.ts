export interface SystemInfo {
	os: string;
	runtime: string;
	architecture: string;
	processId: number;
	databaseProvider: string;
	databaseConnection: string | null;
	logPath: string | null;
}

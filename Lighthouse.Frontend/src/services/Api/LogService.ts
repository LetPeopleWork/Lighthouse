import { BaseApiService } from "./BaseApiService";

/**
 * One thing that has gone wrong on this instance since it started. Not an audit record: the instance
 * keeps a bounded number of these in memory, forgets the oldest to make room, loses all of them on a
 * restart, and under several replicas each one answers only for itself.
 */
export interface IRecentProblem {
	recordedAt: string;
	level: "Warning" | "Error" | "Fatal";
	source: string;
	message: string;
	/** The type of whatever threw, when something did. */
	exceptionType?: string | null;
}

export interface ILogService {
	getRecentProblems(): Promise<IRecentProblem[]>;
	getSupportedLogLevels(): Promise<string[]>;
	getLogLevel(): Promise<string>;
	setLogLevel(logLevel: string): Promise<void>;
	/**
	 * The whole log, or — given a tail — roughly that many bytes from the end of it. A follower asks
	 * for the tail: the instance re-reads the file on every ask, and at Debug level it is far too
	 * large to re-send every few seconds.
	 */
	getLogs(tailBytes?: number): Promise<string>;
	downloadLogs(): Promise<void>;
}

export class LogService extends BaseApiService implements ILogService {
	async getRecentProblems(): Promise<IRecentProblem[]> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IRecentProblem[]>("/logs/problems");

			return response.data;
		});
	}

	async getSupportedLogLevels(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>(
				"/logs/level/supported",
			);

			return response.data;
		});
	}

	async getLogLevel(): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string>("/logs/level");

			return response.data;
		});
	}

	async setLogLevel(logLevel: string): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<void>("/logs/level", { level: logLevel });
		});
	}

	async getLogs(tailBytes?: number): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string>(
				tailBytes === undefined ? "/logs" : `/logs?tailBytes=${tailBytes}`,
			);

			return response.data;
		});
	}

	async downloadLogs(): Promise<void> {
		const response = await this.apiService.get<Blob>("/logs/download", {
			responseType: "blob",
		});
		const fileUrl = URL.createObjectURL(response.data);
		const link = document.createElement("a");
		link.href = fileUrl;
		link.download = `Lighthouse_Log_${new Date().toISOString().split("T")[0]}.txt`;
		document.body.appendChild(link);
		link.click();
		link.remove();
		URL.revokeObjectURL(fileUrl);
	}
}

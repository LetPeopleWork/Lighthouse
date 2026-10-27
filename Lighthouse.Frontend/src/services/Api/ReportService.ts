import {
	type INewReport,
	type IReport,
	type IReportEdit,
	type IReportList,
	type IReportOwner,
	type IReportTemplates,
	ReportListSchema,
	ReportSchema,
	ReportTemplatesSchema,
} from "../../models/Reports/Report";
import {
	type INewReportsForOwners,
	type IReportCreationOptions,
	type IReportCreationOutcome,
	type IReportRemovalOutcome,
	type IReportsAcrossOwners,
	ReportsAcrossOwnersSchema,
} from "../../models/Reports/ReportsAcrossOwners";
import { BaseApiService } from "./BaseApiService";

/** The reports of one Team or one Portfolio. Refusals arrive as an ApiError carrying the reason's code. */
export interface IReportService {
	getTemplates(owner: IReportOwner): Promise<IReportTemplates>;
	getReports(owner: IReportOwner): Promise<IReportList>;
	getReport(owner: IReportOwner, reportId: number): Promise<IReport>;
	/** Answers the new report's id. */
	createReport(owner: IReportOwner, report: INewReport): Promise<number>;
	editReport(
		owner: IReportOwner,
		reportId: number,
		edit: IReportEdit,
	): Promise<IReport>;
	deleteReport(owner: IReportOwner, reportId: number): Promise<void>;
	/** Every report the caller can read, on every Team and Portfolio. */
	getAllReports(): Promise<IReportsAcrossOwners>;
	getCreationOptions(): Promise<IReportCreationOptions>;
	/** Answers one outcome per owner, in the order they were sent. */
	createReportsInBulk(
		request: INewReportsForOwners,
	): Promise<IReportCreationOutcome[]>;
	/** Answers one outcome per report, in the order they were sent. */
	removeReportsInBulk(reportIds: number[]): Promise<IReportRemovalOutcome[]>;
}

const reportsOf = (owner: IReportOwner): string =>
	`/${owner.kind === "team" ? "teams" : "portfolios"}/${owner.id}/reports`;

export class ReportService extends BaseApiService implements IReportService {
	public getTemplates(owner: IReportOwner): Promise<IReportTemplates> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`${reportsOf(owner)}/templates`,
			);
			return BaseApiService.parse(ReportTemplatesSchema, response.data);
		});
	}

	public getReports(owner: IReportOwner): Promise<IReportList> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(reportsOf(owner));
			return BaseApiService.parse(ReportListSchema, response.data);
		});
	}

	public getReport(owner: IReportOwner, reportId: number): Promise<IReport> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`${reportsOf(owner)}/${reportId}`,
			);
			return BaseApiService.parse(ReportSchema, response.data);
		});
	}

	public createReport(
		owner: IReportOwner,
		report: INewReport,
	): Promise<number> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<unknown>(
				reportsOf(owner),
				report,
			);
			return BaseApiService.parse(ReportSchema, response.data).id;
		});
	}

	public editReport(
		owner: IReportOwner,
		reportId: number,
		edit: IReportEdit,
	): Promise<IReport> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<unknown>(
				`${reportsOf(owner)}/${reportId}`,
				edit,
			);
			return BaseApiService.parse(ReportSchema, response.data);
		});
	}

	public deleteReport(owner: IReportOwner, reportId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`${reportsOf(owner)}/${reportId}`);
		});
	}

	public getAllReports(): Promise<IReportsAcrossOwners> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/reports");
			return BaseApiService.parse(ReportsAcrossOwnersSchema, response.data);
		});
	}

	public getCreationOptions(): Promise<IReportCreationOptions> {
		return this.withErrorHandling(() =>
			Promise.reject(
				new Error("SCAFFOLD: creating for several owners is not built yet"),
			),
		);
	}

	public createReportsInBulk(
		_request: INewReportsForOwners,
	): Promise<IReportCreationOutcome[]> {
		return this.withErrorHandling(() =>
			Promise.reject(
				new Error("SCAFFOLD: creating for several owners is not built yet"),
			),
		);
	}

	public removeReportsInBulk(
		_reportIds: number[],
	): Promise<IReportRemovalOutcome[]> {
		return this.withErrorHandling(() =>
			Promise.reject(
				new Error("SCAFFOLD: removing reports in bulk is not built yet"),
			),
		);
	}
}

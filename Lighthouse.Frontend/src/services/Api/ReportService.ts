import type {
	INewReport,
	IReport,
	IReportEdit,
	IReportList,
	IReportOwner,
	IReportTemplates,
} from "../../models/Reports/Report";
import { BaseApiService } from "./BaseApiService";

// Placeholder until the Reports endpoints are wired; every method is replaced with the real call.
export const __SCAFFOLD__ = true;

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
}

const notYet = (what: string): Promise<never> =>
	Promise.reject(new Error(`Not yet implemented -- RED scaffold: ${what}`));

export class ReportService extends BaseApiService implements IReportService {
	public getTemplates(owner: IReportOwner): Promise<IReportTemplates> {
		return notYet(`the report types of ${owner.kind} ${owner.id}`);
	}

	public getReports(owner: IReportOwner): Promise<IReportList> {
		return notYet(`the reports of ${owner.kind} ${owner.id}`);
	}

	public getReport(owner: IReportOwner, reportId: number): Promise<IReport> {
		return notYet(`report ${reportId} of ${owner.kind} ${owner.id}`);
	}

	public createReport(
		owner: IReportOwner,
		report: INewReport,
	): Promise<number> {
		return notYet(`creating "${report.name}" on ${owner.kind} ${owner.id}`);
	}

	public editReport(
		owner: IReportOwner,
		reportId: number,
		edit: IReportEdit,
	): Promise<IReport> {
		return notYet(
			`saving "${edit.name}" over report ${reportId} of ${owner.kind} ${owner.id}`,
		);
	}

	public deleteReport(owner: IReportOwner, reportId: number): Promise<void> {
		return notYet(`deleting report ${reportId} of ${owner.kind} ${owner.id}`);
	}
}

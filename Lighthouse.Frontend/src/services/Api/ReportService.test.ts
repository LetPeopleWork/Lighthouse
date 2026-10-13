import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	INewReport,
	IReport,
	IReportEdit,
	IReportList,
	IReportOwner,
	IReportTemplates,
	IReportValue,
} from "../../models/Reports/Report";
import { ApiError } from "./ApiError";
import { ReportService } from "./ReportService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

const LIGHTSPEED: IReportOwner = { kind: "team", id: 1 };
const APOLLO: IReportOwner = { kind: "portfolio", id: 3 };
const ENGAGEMENT_START_ID = 42;
const ENGAGEMENT_START = "Then & Now — engagement start";

const shown = (value: number, sampleSize: number): IReportValue => ({
	value,
	sampleSize,
	absentReason: null,
});

const aDash = (absentReason: IReportValue["absentReason"]): IReportValue => ({
	value: null,
	sampleSize: null,
	absentReason,
});

const aListedReport = () => ({
	id: ENGAGEMENT_START_ID,
	name: ENGAGEMENT_START,
	templateKey: "then-and-now",
	ownerKind: "team" as const,
	ownerId: 1,
	createdOn: "2026-08-01",
	thenAndNow: { thenRebuildable: true },
});

/** A later version's report: listed with no Then & Now part at all. */
const aListedReportOfALaterType = () => ({
	id: 44,
	name: "Signals — flow alerts",
	templateKey: "signals",
	ownerKind: "team" as const,
	ownerId: 1,
	createdOn: "2026-09-20",
});

const aReportList = (reports: IReportList["reports"]): IReportList => ({
	reports,
	creationBlockedByCap: false,
	communityCap: 2,
});

const aTemplateList = (earliestStart: string | null): IReportTemplates => ({
	today: "2026-10-02",
	earliestStart,
	minimumLengthDays: 14,
	templates: [
		{
			templateKey: "then-and-now",
			metrics: [{ key: "cycle-time" }, { key: "wip" }],
		},
	],
});

const theEngagementStartReport = (
	overrides: Partial<IReport> = {},
): IReport => ({
	id: ENGAGEMENT_START_ID,
	name: ENGAGEMENT_START,
	templateKey: "then-and-now",
	ownerKind: "team",
	ownerId: 1,
	createdOn: "2026-08-01",
	concurrencyToken: "token-1",
	shownMetricKeys: ["cycle-time", "wip"],
	thenAndNow: {
		thenWindow: { start: "2026-05-03", end: "2026-07-31", lengthDays: 90 },
		nowWindow: { start: "2026-07-05", end: "2026-10-02", lengthDays: 90 },
		daysSinceThenEnded: 63,
		thenRebuildable: true,
		metrics: {
			"cycle-time": {
				atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
				atNow: { p85: shown(12, 41), p50: shown(4, 41) },
				beyondThenLimits: {
					above: shown(1, 41),
					below: aDash("lower-limit-zero"),
				},
			},
			wip: {
				atThen: { average: shown(9, 90) },
				atNow: { average: aDash("no-wip") },
			},
		},
	},
	...overrides,
});

const aNewReport = (name: string): INewReport => ({
	templateKey: "then-and-now",
	name,
	shownMetricKeys: ["cycle-time", "wip"],
	thenAndNow: {
		thenEndDate: "2026-07-31",
		thenLengthDays: 90,
		nowLengthDays: null,
	},
});

describe("ReportService", () => {
	let subject: ReportService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		subject = new ReportService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it.each([
		[LIGHTSPEED, "/teams/1/reports"],
		[APOLLO, "/portfolios/3/reports"],
	])(
		"lists the reports of %o from %s, a report of a later type included",
		async (owner, path) => {
			const list = aReportList([aListedReport(), aListedReportOfALaterType()]);
			mockedAxios.get.mockResolvedValueOnce({ data: list });

			const result = await subject.getReports(owner);

			expect(mockedAxios.get).toHaveBeenCalledWith(path);
			expect(result).toEqual(list);
		},
	);

	it("reads the report types with no earliest start when every finished item is kept", async () => {
		const templates = aTemplateList(null);
		mockedAxios.get.mockResolvedValueOnce({ data: templates });

		const result = await subject.getTemplates(LIGHTSPEED);

		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/1/reports/templates");
		expect(result).toEqual(templates);
	});

	it("reads a report with its sides named atThen and atNow, as the server sends them, dashes included", async () => {
		const report = theEngagementStartReport();
		mockedAxios.get.mockResolvedValueOnce({ data: report });

		const result = await subject.getReport(LIGHTSPEED, ENGAGEMENT_START_ID);

		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/1/reports/42");
		expect(result).toEqual(report);
		expect(
			result.thenAndNow.metrics["cycle-time"]?.beyondThenLimits?.below,
		).toEqual(aDash("lower-limit-zero"));
	});

	it("reads a report with a metric that has no value and one without limits", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.throughput = null;
		report.thenAndNow.metrics.wip = {
			atThen: {},
			atNow: {},
			beyondThenLimits: null,
		};
		mockedAxios.get.mockResolvedValueOnce({ data: report });

		const result = await subject.getReport(APOLLO, ENGAGEMENT_START_ID);

		expect(mockedAxios.get).toHaveBeenCalledWith("/portfolios/3/reports/42");
		expect(result).toEqual(report);
	});

	it("reads the owner kind as the server sends it, by its capitalised name", async () => {
		const listed = { ...aListedReport(), ownerKind: "Team" };
		const portfolioReport = {
			...theEngagementStartReport({ ownerId: 3 }),
			ownerKind: "Portfolio",
		};
		mockedAxios.get
			.mockResolvedValueOnce({
				data: { ...aReportList([]), reports: [listed] },
			})
			.mockResolvedValueOnce({ data: portfolioReport });

		const list = await subject.getReports(LIGHTSPEED);
		const report = await subject.getReport(APOLLO, ENGAGEMENT_START_ID);

		expect(list.reports[0].ownerKind).toBe("team");
		expect(report.ownerKind).toBe("portfolio");
	});

	it("refuses a list that is not the shape the server promises", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { reports: [{ id: 1, name: null }] },
		});

		await expect(subject.getReports(LIGHTSPEED)).rejects.toBeInstanceOf(
			ApiError,
		);
	});

	it("creates a report and answers its id", async () => {
		const newReport = aNewReport(ENGAGEMENT_START);
		mockedAxios.post.mockResolvedValueOnce({
			data: theEngagementStartReport(),
		});

		const id = await subject.createReport(LIGHTSPEED, newReport);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/teams/1/reports",
			newReport,
		);
		expect(id).toBe(ENGAGEMENT_START_ID);
	});

	it("saves an edit over a report and answers the report as saved", async () => {
		const edit: IReportEdit = {
			name: "Renamed",
			shownMetricKeys: ["wip"],
			concurrencyToken: "token-1",
			thenAndNow: { nowLengthDays: 30 },
		};
		const saved = theEngagementStartReport({ name: "Renamed" });
		mockedAxios.put.mockResolvedValueOnce({ data: saved });

		const result = await subject.editReport(APOLLO, ENGAGEMENT_START_ID, edit);

		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/portfolios/3/reports/42",
			edit,
		);
		expect(result).toEqual(saved);
	});

	it("deletes a report", async () => {
		mockedAxios.delete.mockResolvedValueOnce({ status: 204 });

		await subject.deleteReport(LIGHTSPEED, ENGAGEMENT_START_ID);

		expect(mockedAxios.delete).toHaveBeenCalledWith("/teams/1/reports/42");
	});

	it("hands a refusal on as an ApiError carrying the reason's code", async () => {
		mockedAxios.isAxiosError.mockReturnValue(true);
		mockedAxios.post.mockRejectedValueOnce({
			message: "Forbidden",
			response: { status: 403, data: { code: "report-cap-reached" } },
		});

		await expect(
			subject.createReport(LIGHTSPEED, aNewReport("One too many")),
		).rejects.toMatchObject({ code: 403, problemCode: "report-cap-reached" });
	});
});

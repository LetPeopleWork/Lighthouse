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
				atNow: {
					p85: shown(12, 41),
					p70: aDash("no-finished-items"),
					p50: shown(4, 41),
				},
			},
			wip: {
				atThen: {
					average: shown(8.62, 90),
					median: shown(9, 90),
					min: shown(6, 90),
					max: shown(12, 90),
				},
				atNow: {
					average: shown(7, 90),
					median: shown(7, 90),
					min: shown(5, 90),
					max: shown(9, 90),
				},
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

	it("lists every report the caller can read from one call, each with its owner's name", async () => {
		const everyReport = {
			reports: [
				{ ...aListedReport(), ownerName: "Lightspeed" },
				{
					...aListedReportOfALaterType(),
					ownerKind: "Portfolio",
					ownerId: 3,
					ownerName: "Apollo",
				},
			],
		};
		mockedAxios.get.mockResolvedValueOnce({ data: everyReport });

		const result = await subject.getAllReports();

		expect(mockedAxios.get).toHaveBeenCalledWith("/reports");
		expect(result.reports.map((r) => [r.ownerKind, r.ownerName])).toEqual([
			["team", "Lightspeed"],
			["portfolio", "Apollo"],
		]);
	});

	it("refuses a cross-owner list in which a report has no owner name", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { reports: [aListedReport()] },
		});

		await expect(subject.getAllReports()).rejects.toBeInstanceOf(ApiError);
	});

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
		expect(result.thenAndNow.metrics["cycle-time"]?.atNow.p70).toEqual(
			aDash("no-finished-items"),
		);
	});

	it("reads a report with a metric that has no value and one with nothing on either side", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.throughput = null;
		report.thenAndNow.metrics.wip = { atThen: {}, atNow: {} };
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

	it("reads what creating a report needs, owner kinds lower-cased as the browser names them", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: {
				today: "2026-10-27",
				minimumLengthDays: 14,
				templates: [
					{
						templateKey: "then-and-now",
						metricKeysByOwnerKind: {
							Team: ["cycle-time", "throughput"],
							Portfolio: ["wip"],
						},
					},
				],
				owners: [
					{
						ownerKind: "Team",
						ownerId: 21,
						ownerName: "Zenith",
						earliestStart: null,
					},
					{
						ownerKind: "Portfolio",
						ownerId: 31,
						ownerName: "Ocean Explorer",
						earliestStart: "2026-07-30",
					},
				],
			},
		});

		const options = await subject.getCreationOptions();

		expect(mockedAxios.get).toHaveBeenCalledWith("/reports/creation-options");
		expect(options).toEqual({
			today: "2026-10-27",
			minimumLengthDays: 14,
			templates: [
				{
					templateKey: "then-and-now",
					metricKeysByOwnerKind: {
						team: ["cycle-time", "throughput"],
						portfolio: ["wip"],
					},
				},
			],
			owners: [
				{
					ownerKind: "team",
					ownerId: 21,
					ownerName: "Zenith",
					earliestStart: null,
				},
				{
					ownerKind: "portfolio",
					ownerId: 31,
					ownerName: "Ocean Explorer",
					earliestStart: "2026-07-30",
				},
			],
		});
	});

	it("creates one report for several owners in one request and answers each owner's outcome in order", async () => {
		const report: INewReport = {
			templateKey: "then-and-now",
			name: ENGAGEMENT_START,
			shownMetricKeys: ["cycle-time"],
			thenAndNow: {
				thenEndDate: "2026-07-29",
				thenLengthDays: 90,
				nowLengthDays: null,
			},
		};
		mockedAxios.post.mockResolvedValueOnce({
			data: {
				outcomes: [
					{
						ownerKind: "Team",
						ownerId: 21,
						ownerName: "Zenith",
						status: "created",
						reportId: 501,
						code: null,
						earliestStart: null,
					},
					{
						ownerKind: "Portfolio",
						ownerId: 31,
						ownerName: null,
						status: "notCreated",
						reportId: null,
						code: "not-admin",
						earliestStart: null,
					},
				],
			},
		});

		const outcomes = await subject.createReportsInBulk({
			...report,
			owners: [
				{ ownerKind: "team", ownerId: 21 },
				{ ownerKind: "portfolio", ownerId: 31 },
			],
		});

		expect(mockedAxios.post).toHaveBeenCalledWith("/reports/bulk", {
			...report,
			owners: [
				{ ownerKind: "Team", ownerId: 21 },
				{ ownerKind: "Portfolio", ownerId: 31 },
			],
		});
		expect(outcomes).toEqual([
			{
				ownerKind: "team",
				ownerId: 21,
				ownerName: "Zenith",
				status: "created",
				reportId: 501,
				code: undefined,
				earliestStart: null,
			},
			{
				ownerKind: "portfolio",
				ownerId: 31,
				ownerName: undefined,
				status: "notCreated",
				reportId: undefined,
				code: "not-admin",
				earliestStart: null,
			},
		]);
	});

	it("refuses an answer to a bulk run that names an outcome it does not know", async () => {
		mockedAxios.post.mockResolvedValueOnce({
			data: {
				outcomes: [{ ownerKind: "Team", ownerId: 21, status: "maybe" }],
			},
		});

		await expect(
			subject.createReportsInBulk({
				templateKey: "then-and-now",
				name: ENGAGEMENT_START,
				shownMetricKeys: ["cycle-time"],
				thenAndNow: {
					thenEndDate: "2026-07-29",
					thenLengthDays: 90,
					nowLengthDays: null,
				},
				owners: [{ ownerKind: "team", ownerId: 21 }],
			}),
		).rejects.toBeInstanceOf(ApiError);
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

	it.each([
		[
			"names the oldest kept day",
			{ earliestStart: "2025-10-03" },
			"2025-10-03",
		],
		["says there is no oldest kept day", { earliestStart: null }, null],
		["says nothing of an oldest kept day", {}, undefined],
		[
			"names something that is not a day",
			{ earliestStart: 20251003 },
			undefined,
		],
	])(
		"hands on the oldest kept day a refusal %s exactly as it was given",
		async (_, body, earliestStart) => {
			mockedAxios.isAxiosError.mockReturnValue(true);
			mockedAxios.post.mockRejectedValueOnce({
				message: "Bad Request",
				response: {
					status: 400,
					data: { code: "then-before-cutoff", ...body },
				},
			});

			const refusal = await subject
				.createReport(LIGHTSPEED, aNewReport("Too far back"))
				.catch((error: unknown) => error);

			expect(refusal).toBeInstanceOf(ApiError);
			expect((refusal as ApiError).earliestStart).toBe(earliestStart);
		},
	);
});

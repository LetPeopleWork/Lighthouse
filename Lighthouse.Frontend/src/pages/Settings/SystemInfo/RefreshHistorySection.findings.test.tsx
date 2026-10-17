import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type {
	RefreshFinding,
	RefreshLog,
} from "../../../models/SystemInfo/RefreshLog";

/**
 * Priya names "depends on" as Payments Platform's Dependency Field, its update finishes, and Dependencies
 * stays empty. Refresh History, on Payments Platform, now says why, as of the latest full download: no
 * Feature carries that link, and these are the links that were seen. Each finding reads in the words this
 * instance uses.
 */

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(() => <div data-testid="mock-bar-chart" />),
	};
});

const termsThisInstanceUses: Record<string, string> = {
	feature: "Epic",
	features: "Epics",
	workItems: "Tickets",
};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => termsThisInstanceUses[key] ?? key,
	}),
}));

import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISystemInfoService } from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import RefreshHistorySection from "./RefreshHistorySection";

const TheLatestFullDownload = "2026-10-16T09:12:00";

const TheAsOfLabel = "as of the full download on 16 Oct, 09:12";

const NothingMatched: RefreshFinding = {
	kind: "NamedLinkMatchedNothing",
	setting: "DependencyField",
	named: "depends on",
	seen: ["is blocked by", "relates to"],
};

const PointsBothWays: RefreshFinding = {
	kind: "NamedLinkPointsBothWays",
	setting: "DependencyField",
	named: "Blocks",
	seen: ["blocks", "is blocked by"],
};

const ARelationTypeAsParentOverride: RefreshFinding = {
	kind: "RelationTypeCannotBeReadHere",
	setting: "ParentOverrideField",
	named: "Contoso.DependsOn-Forward",
	seen: [],
};

const paymentsPlatformRows = (
	findings: RefreshFinding[] | undefined,
): RefreshLog[] =>
	[1, 2].map((id) => ({
		id,
		type: "Portfolio",
		entityId: 5,
		entityName: "Payments Platform",
		itemCount: 3,
		durationMs: 400,
		executedAt: `2026-10-16T1${id}:00:00`,
		success: true,
		cancelled: false,
		...(findings === undefined
			? {}
			: {
					latestFullRunFindings: findings,
					latestFullRunAt: TheLatestFullDownload,
				}),
	}));

const givenRefreshHistoryHolds = async (rows: RefreshLog[]) => {
	const systemInfo: ISystemInfoService = createMockSystemInfoService();
	systemInfo.getSystemInfo = vi.fn();
	systemInfo.getRefreshLogs = vi.fn().mockResolvedValue(rows);

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ systemInfoService: systemInfo })}
		>
			<RefreshHistorySection />
		</ApiServiceContext.Provider>,
	);

	await waitFor(() =>
		expect(screen.getByText("All (Aggregate)")).toBeInTheDocument(),
	);
	await userEvent.click(screen.getByRole("combobox"));
	await userEvent.click(
		await screen.findByText("Portfolio: Payments Platform"),
	);
	await waitFor(() =>
		expect(screen.getByText("Total Runs")).toBeInTheDocument(),
	);
};

describe("Refresh History says what a Portfolio's settings named that its latest full download did not find", () => {
	// @walking_skeleton @driving_port @us-02 @ac-2.2 @kpi @contract-shape:pure-function
	it("a named link nothing carries is shown with the links that were seen and when that was found", async () => {
		await givenRefreshHistoryHolds(paymentsPlatformRows([NothingMatched]));

		expect(
			screen.getByText(
				'The Dependency Field names "depends on", and no Epic carries that link. Links seen: is blocked by, relates to.',
			),
		).toBeInTheDocument();
		expect(screen.getByText(TheAsOfLabel)).toBeInTheDocument();
	});

	// @error @us-01 @ac-1.3 @contract-shape:pure-function
	it("a link type named by its name rather than one end asks which end is meant", async () => {
		await givenRefreshHistoryHolds(paymentsPlatformRows([PointsBothWays]));

		expect(
			screen.getByText(
				'The Dependency Field names "Blocks", which points both ways. Name one end: "blocks" or "is blocked by".',
			),
		).toBeInTheDocument();
	});

	// @error @us-03 @ac-3.10 @contract-shape:pure-function
	it("a relation type named as the Parent Override Field says only the Dependency Field reads one", async () => {
		await givenRefreshHistoryHolds(
			paymentsPlatformRows([ARelationTypeAsParentOverride]),
		);

		expect(
			screen.getByText(
				"The Parent Override Field points at an Azure DevOps relation type, which only the Dependency Field reads.",
			),
		).toBeInTheDocument();
	});

	// @edge @active-guard @us-02 @ac-2.1 @contract-shape:unbounded-preservation
	it("a full download that found nothing to say shows no finding and no label", async () => {
		await givenRefreshHistoryHolds(paymentsPlatformRows([]));

		expect(
			screen.queryByText(/^as of the full download/),
		).not.toBeInTheDocument();
		expect(
			screen.queryByText(/^The Dependency Field names/),
		).not.toBeInTheDocument();
	});

	// @error @active-guard @us-02 @contract-shape:unbounded-preservation
	it("rows from a backend that sends no findings still show their history", async () => {
		await givenRefreshHistoryHolds(paymentsPlatformRows(undefined));

		expect(
			screen.queryByText(/^as of the full download/),
		).not.toBeInTheDocument();
		expect(screen.getByText("Total Runs")).toBeInTheDocument();
	});
});

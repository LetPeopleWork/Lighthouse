import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RefreshLog } from "../../../models/SystemInfo/RefreshLog";

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(() => <div data-testid="mock-bar-chart" />),
	};
});

// Words no tracker seeds, so a caption can only show them by reading Terminology.
const termsThisInstanceUses: Record<string, string> = {
	workItems: "Tickets",
	features: "Epics",
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

const mockGetRefreshLogs = vi.fn();
const mockSystemInfoService: ISystemInfoService = createMockSystemInfoService();
mockSystemInfoService.getSystemInfo = vi.fn();
mockSystemInfoService.getRefreshLogs = mockGetRefreshLogs;

const MockProvider = ({ children }: { children: React.ReactNode }) => (
	<ApiServiceContext.Provider
		value={createMockApiServiceContext({
			systemInfoService: mockSystemInfoService,
		})}
	>
		{children}
	</ApiServiceContext.Provider>
);

const mockLogs: RefreshLog[] = [
	{
		id: 1,
		type: "Team",
		entityId: 1,
		entityName: "Team Alpha",
		itemCount: 10,
		durationMs: 300,
		executedAt: "2026-03-01T10:00:00Z",
		success: true,
		cancelled: false,
	},
	{
		id: 2,
		type: "Team",
		entityId: 1,
		entityName: "Team Alpha",
		itemCount: 12,
		durationMs: 400,
		executedAt: "2026-03-02T10:00:00Z",
		success: true,
		cancelled: false,
	},
	{
		id: 3,
		type: "Portfolio",
		entityId: 5,
		entityName: "My Portfolio",
		itemCount: 20,
		durationMs: 800,
		executedAt: "2026-03-01T11:00:00Z",
		success: true,
		cancelled: false,
	},
];

const selectEntity = async (entityLabel: string) => {
	await waitFor(() => {
		expect(screen.getByText("All (Aggregate)")).toBeInTheDocument();
	});

	await userEvent.click(screen.getByRole("combobox"));
	await userEvent.click(await screen.findByText(entityLabel));

	await waitFor(() => {
		expect(screen.getByText("Total Runs")).toBeInTheDocument();
	});
};

const theFigureCaptioned = (caption: string) =>
	screen.getByText(caption).parentElement as HTMLElement;

const fetchedCaption = (records: string) =>
	`Max ${records} Fetched With Links Naming More Than One Parent`;

const placedCaption = (records: string) =>
	`Max ${records} Placed From Links Naming More Than One Parent`;

const notPlacedCaption = (records: string) =>
	`Max ${records} Not Placed: Links Name More Than One Parent`;

const expectNoParentFigureFor = (records: string) => {
	for (const caption of [
		fetchedCaption(records),
		placedCaption(records),
		notPlacedCaption(records),
	]) {
		expect(screen.queryByText(caption)).not.toBeInTheDocument();
	}
};

const selectTeamAlpha = async () => {
	await waitFor(() => {
		expect(screen.getByText("All (Aggregate)")).toBeInTheDocument();
	});

	await userEvent.click(screen.getByRole("combobox"));

	await waitFor(() => {
		expect(screen.getByText("Team: Team Alpha")).toBeInTheDocument();
	});

	await userEvent.click(screen.getByText("Team: Team Alpha"));

	await waitFor(() => {
		expect(screen.getByText("Total Runs")).toBeInTheDocument();
	});
};

describe("RefreshHistorySection", () => {
	it("shows empty state message when no logs available", async () => {
		mockGetRefreshLogs.mockResolvedValue([]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(
				screen.getByText("No refresh history available yet."),
			).toBeInTheDocument();
		});
	});

	it("shows aggregate stats in All view by default", async () => {
		mockGetRefreshLogs.mockResolvedValue(mockLogs);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Total Items Fetched")).toBeInTheDocument();
		});

		expect(screen.getByText("Avg Duration")).toBeInTheDocument();
		expect(screen.getByText("Max Duration")).toBeInTheDocument();

		// Total items: 10 + 12 + 20 = 42
		expect(screen.getByText("42")).toBeInTheDocument();

		// Aggregate comparison chart should be rendered
		expect(screen.getByTestId("mock-bar-chart")).toBeInTheDocument();
	});

	it("shows entity options in dropdown", async () => {
		mockGetRefreshLogs.mockResolvedValue(mockLogs);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("All (Aggregate)")).toBeInTheDocument();
		});
	});

	it("renders chart and stats when a specific entity is selected", async () => {
		mockGetRefreshLogs.mockResolvedValue(mockLogs);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		expect(screen.getByText("Success Rate")).toBeInTheDocument();
		expect(screen.getByText("Avg Duration")).toBeInTheDocument();
	});

	it("does not count a cancelled refresh against the success rate", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			...mockLogs,
			{
				id: 4,
				type: "Team",
				entityId: 1,
				entityName: "Team Alpha",
				itemCount: 0,
				durationMs: 120,
				executedAt: "2026-03-03T10:00:00Z",
				success: false,
				cancelled: true,
			},
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		// Two refreshes ran and both worked; the third was stopped by an operator. Counting that as a
		// failure reports the connection as broken to whoever stopped it.
		expect(screen.getByText("100%")).toBeInTheDocument();
		expect(screen.getByText("Cancelled")).toBeInTheDocument();
	});

	it("says nothing about cancellations when there were none", async () => {
		mockGetRefreshLogs.mockResolvedValue(mockLogs);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		// A row reading "Cancelled: 0" on every healthy entity is noise that teaches people to skip the
		// panel, which is where the number that does matter lives.
		expect(screen.queryByText("Cancelled")).not.toBeInTheDocument();
	});

	it("reports no success rate rather than a broken one when every run was cancelled", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{
				id: 10,
				type: "Team",
				entityId: 1,
				entityName: "Team Alpha",
				itemCount: 0,
				durationMs: 90,
				executedAt: "2026-03-04T10:00:00Z",
				success: false,
				cancelled: true,
			},
			{
				id: 11,
				type: "Team",
				entityId: 1,
				entityName: "Team Alpha",
				itemCount: 0,
				durationMs: 110,
				executedAt: "2026-03-05T10:00:00Z",
				success: false,
				cancelled: true,
			},
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		// Nothing was left to finish, so there is no rate to report. Dividing anyway puts NaN% on the
		// screen, which reads as a bug in Lighthouse rather than as an operator stopping every run.
		expect(screen.getByText("0%")).toBeInTheDocument();
	});

	it("says nothing about parents when every refresh could place everything it fetched", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{ ...mockLogs[0], recordsWhoseLinksNamedMoreThanOneParent: 0 },
			{ ...mockLogs[1], recordsWhoseLinksNamedMoreThanOneParent: 0 },
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		expectNoParentFigureFor("Tickets");
	});

	it("renders a refresh from a backend too old to send the count at all", async () => {
		mockGetRefreshLogs.mockResolvedValue(mockLogs);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectTeamAlpha();

		expect(screen.getByText("Total Runs")).toBeInTheDocument();
		expectNoParentFigureFor("Tickets");
	});
});

describe("RefreshHistorySection: placed and not placed", () => {
	it("shows the worst single refresh for each figure, placed apart from not placed, in the instance's Work Item term", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{
				...mockLogs[0],
				recordsPlacedFromLinksNamingMoreThanOneParent: 41,
				recordsWhoseLinksNamedMoreThanOneParent: 1,
			},
			{
				...mockLogs[1],
				recordsPlacedFromLinksNamingMoreThanOneParent: 30,
				recordsWhoseLinksNamedMoreThanOneParent: 2,
			},
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectEntity("Team: Team Alpha");

		// The two refreshes are almost certainly the same untidy records read twice, so 71 and 3 would be
		// made-up totals. Each figure is the worst a single refresh saw.
		expect(
			within(theFigureCaptioned(placedCaption("Tickets"))).getByText("41"),
		).toBeInTheDocument();
		expect(
			within(theFigureCaptioned(notPlacedCaption("Tickets"))).getByText("2"),
		).toBeInTheDocument();
		expectNoParentFigureFor("Epics");
		expect(
			screen.queryByText(fetchedCaption("Tickets")),
		).not.toBeInTheDocument();
	});

	it("shows only the figure that is above zero across the window", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{
				...mockLogs[0],
				recordsPlacedFromLinksNamingMoreThanOneParent: 3,
				recordsWhoseLinksNamedMoreThanOneParent: 0,
			},
			{
				...mockLogs[1],
				recordsPlacedFromLinksNamingMoreThanOneParent: 0,
				recordsWhoseLinksNamedMoreThanOneParent: 0,
			},
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectEntity("Team: Team Alpha");

		// A figure reading zero on every healthy entity teaches the reader to skip the panel.
		expect(screen.getByText(placedCaption("Tickets"))).toBeInTheDocument();
		expect(
			screen.queryByText(notPlacedCaption("Tickets")),
		).not.toBeInTheDocument();
	});

	it("reads a refresh recorded before the split as not placed, which is what happened to it", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{ ...mockLogs[0], recordsWhoseLinksNamedMoreThanOneParent: 4 },
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectEntity("Team: Team Alpha");

		expect(
			within(theFigureCaptioned(notPlacedCaption("Tickets"))).getByText("4"),
		).toBeInTheDocument();
		expect(
			screen.queryByText(placedCaption("Tickets")),
		).not.toBeInTheDocument();
	});

	it("uses the instance's Feature term on a Portfolio, whose records are Features", async () => {
		mockGetRefreshLogs.mockResolvedValue([
			{
				...mockLogs[2],
				recordsPlacedFromLinksNamingMoreThanOneParent: 6,
				recordsWhoseLinksNamedMoreThanOneParent: 1,
			},
		]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		await selectEntity("Portfolio: My Portfolio");

		expect(
			within(theFigureCaptioned(placedCaption("Epics"))).getByText("6"),
		).toBeInTheDocument();
		expect(
			within(theFigureCaptioned(notPlacedCaption("Epics"))).getByText("1"),
		).toBeInTheDocument();
		expectNoParentFigureFor("Tickets");
	});

	it("shows a spinner while the history is on its way, and neither the empty message nor the entity picker", () => {
		mockGetRefreshLogs.mockReturnValue(new Promise<RefreshLog[]>(() => {}));

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		// An answer still on its way is not an empty history, and a picker over nothing invites a choice
		// that cannot be made yet.
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.queryByText("No refresh history available yet."),
		).not.toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("says it could not load the history when the request fails, not that there is none", async () => {
		const quietConsole = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		mockGetRefreshLogs.mockRejectedValue(new Error("The connection dropped"));

		try {
			render(
				<MockProvider>
					<RefreshHistorySection />
				</MockProvider>,
			);

			expect(await screen.findByRole("alert")).toHaveTextContent(
				/^Could not load refresh history\.$/,
			);
			expect(
				screen.queryByText("No refresh history available yet."),
			).not.toBeInTheDocument();
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		} finally {
			quietConsole.mockRestore();
		}
	});

	it("says there is no history yet only when the history came back empty", async () => {
		mockGetRefreshLogs.mockResolvedValue([]);

		render(
			<MockProvider>
				<RefreshHistorySection />
			</MockProvider>,
		);

		expect(
			await screen.findByText("No refresh history available yet."),
		).toBeInTheDocument();
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});
});

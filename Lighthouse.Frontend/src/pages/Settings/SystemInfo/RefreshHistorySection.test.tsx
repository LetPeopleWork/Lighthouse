import { render, screen, waitFor } from "@testing-library/react";
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

		// Nothing was left to finish, so there is no rate to report. Dividing anyway puts NaN% on the
		// screen, which reads as a bug in Lighthouse rather than as an operator stopping every run.
		expect(screen.getByText("0%")).toBeInTheDocument();
	});
});

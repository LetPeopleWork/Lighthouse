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
});

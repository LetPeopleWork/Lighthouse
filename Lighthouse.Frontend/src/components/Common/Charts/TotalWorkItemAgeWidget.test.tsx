import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import TotalWorkItemAgeWidget from "./TotalWorkItemAgeWidget";

const theme = createTheme();

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
	<ThemeProvider theme={theme}>{children}</ThemeProvider>
);

describe("TotalWorkItemAgeWidget", () => {
	const createMockMetricsService = (
		totalAge: number | null,
		shouldError = false,
	): IMetricsService<IWorkItem | IFeature> => ({
		getTotalWorkItemAge: vi.fn().mockImplementation(() => {
			if (shouldError) {
				return Promise.reject(new Error("API Error"));
			}
			return Promise.resolve(totalAge);
		}),
		getThroughput: vi.fn(),
		getStartedItems: vi.fn(),
		getWorkInProgressOverTime: vi.fn(),
		getInProgressItems: vi.fn(),
		getCycleTimeData: vi.fn(),
		getCycleTimePercentiles: vi.fn(),
		getMultiItemForecastPredictabilityScore: vi.fn(),
		getThroughputPbc: vi.fn(),
		getWipPbc: vi.fn(),
		getTotalWorkItemAgePbc: vi.fn(),
		getCycleTimePbc: vi.fn(),
	});

	it("renders loading state initially", () => {
		const mockService = createMockMetricsService(150);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("displays total work item age after loading", async () => {
		const mockService = createMockMetricsService(250);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(screen.getByText("250")).toBeInTheDocument();
			expect(screen.getByText("days")).toBeInTheDocument();
		});
	});

	it("calls getTotalWorkItemAge with correct entity ID", async () => {
		const mockService = createMockMetricsService(100);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={42} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(mockService.getTotalWorkItemAge).toHaveBeenCalledWith(42);
		});
	});

	it("displays error message when API call fails", async () => {
		const mockService = createMockMetricsService(null, true);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(screen.getByText("Failed to load data")).toBeInTheDocument();
		});
	});

	it("does not show loading indicator after data is loaded", async () => {
		const mockService = createMockMetricsService(75);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(screen.getByText("75")).toBeInTheDocument();
		});

		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("renders title with terminology", async () => {
		const mockService = createMockMetricsService(100);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(
				screen.getByRole("heading", { name: /Total.*Work Item Age/i }),
			).toBeInTheDocument();
		});
	});

	it("handles zero age correctly", async () => {
		const mockService = createMockMetricsService(0);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(screen.getByText("0")).toBeInTheDocument();
			expect(screen.getByText("days")).toBeInTheDocument();
		});
	});

	it("refetches data when entity ID changes", async () => {
		const mockService = createMockMetricsService(100);
		const { rerender } = render(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={1} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(mockService.getTotalWorkItemAge).toHaveBeenCalledWith(1);
		});

		rerender(
			<TestWrapper>
				<TotalWorkItemAgeWidget entityId={2} metricsService={mockService} />
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(mockService.getTotalWorkItemAge).toHaveBeenCalledWith(2);
		});
	});
});

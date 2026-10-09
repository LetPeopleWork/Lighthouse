import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	render as renderOnItsOwn,
	screen,
	waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import ThroughputRunChartCard from "./ThroughputRunChartCard";

vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		title,
		chartData,
		displayTotal,
		filterToggle,
	}: {
		title: string;
		chartData: RunChartData;
		displayTotal?: boolean;
		filterToggle?: React.ReactNode;
	}) => (
		<div data-testid={`bar-run-chart-${title}`}>
			<div data-testid="chart-history">{chartData.history}</div>
			<div data-testid="chart-total">{chartData.total}</div>
			<div data-testid="chart-shows-total">{String(displayTotal)}</div>
			{filterToggle}
		</div>
	),
}));

const mockCanUsePremiumFeatures = vi.fn(() => true);
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: mockCanUsePremiumFeatures() },
	}),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "throughput" ? "Throughput" : key),
	}),
}));

const rawData = new RunChartData({ 0: [], 1: [] }, 2, 5, []);
const filteredData = new RunChartData({ 0: [], 1: [] }, 2, 3, []);

const buildMockMetricsService = (): IMetricsService<IWorkItem> =>
	({
		getThroughput: vi.fn().mockResolvedValue(filteredData),
	}) as unknown as IMetricsService<IWorkItem>;

let queryClient: QueryClient;

const render = (ui: React.ReactElement) =>
	renderOnItsOwn(
		<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
	);

const buildProps = (
	overrides: Partial<
		React.ComponentProps<typeof ThroughputRunChartCard<IWorkItem>>
	> = {},
) => ({
	owner: { ownerType: "team" as const, ownerId: 42, ownerUpdatedAt: 0 },
	metricsService: buildMockMetricsService(),
	startDate: new Date("2026-01-01"),
	endDate: new Date("2026-01-31"),
	rawData,
	title: "Work Items Completed",
	isPremium: true,
	hasForecastFilter: true,
	...overrides,
});

describe("ThroughputRunChartCard", () => {
	beforeEach(() => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
	});

	it("renders BarRunChart with the raw data on first render", () => {
		render(<ThroughputRunChartCard {...buildProps()} />);

		expect(
			screen.getByTestId("bar-run-chart-Work Items Completed"),
		).toBeInTheDocument();
		expect(screen.getByTestId("chart-total")).toHaveTextContent("5");
	});

	it("renders the filter Switch when premium tenant has a forecast filter configured", () => {
		render(<ThroughputRunChartCard {...buildProps()} />);
		expect(
			screen.getByLabelText(/use filtered throughput/i),
		).toBeInTheDocument();
	});

	it("hides the filter Switch when the team has no forecast filter", () => {
		render(
			<ThroughputRunChartCard {...buildProps({ hasForecastFilter: false })} />,
		);
		expect(
			screen.queryByLabelText(/use filtered throughput/i),
		).not.toBeInTheDocument();
	});

	it("fetches filtered throughput and switches display when toggle flips on", async () => {
		const props = buildProps();
		render(<ThroughputRunChartCard {...props} />);

		await userEvent
			.setup()
			.click(screen.getByLabelText(/use filtered throughput/i));

		await waitFor(() => {
			expect(props.metricsService.getThroughput).toHaveBeenCalledWith(
				42,
				props.startDate,
				props.endDate,
				"filtered",
			);
		});

		await waitFor(() => {
			expect(screen.getByTestId("chart-total")).toHaveTextContent("3");
		});
	});

	it("does not refetch filtered data when the user toggles back to raw and then on again", async () => {
		const props = buildProps();
		render(<ThroughputRunChartCard {...props} />);
		const user = userEvent.setup();
		const toggle = screen.getByLabelText(/use filtered throughput/i);

		await user.click(toggle);
		await waitFor(() => {
			expect(props.metricsService.getThroughput).toHaveBeenCalledTimes(1);
		});

		await user.click(toggle);
		await user.click(toggle);

		expect(props.metricsService.getThroughput).toHaveBeenCalledTimes(1);
	});
});

describe("ThroughputRunChartCard when the filtered series cannot be loaded", () => {
	beforeEach(() => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
	});

	it("logs the failure and keeps showing the unfiltered series", async () => {
		const consoleError = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		const failure = new Error("filtered throughput failed");
		const props = buildProps();
		vi.mocked(props.metricsService.getThroughput).mockRejectedValue(failure);
		render(<ThroughputRunChartCard {...props} />);

		await userEvent
			.setup()
			.click(screen.getByLabelText(/use filtered throughput/i));

		await waitFor(() => {
			expect(consoleError).toHaveBeenCalledWith(
				"Error fetching filtered throughput:",
				failure,
			);
		});
		expect(screen.getByTestId("chart-total")).toHaveTextContent("5");
		consoleError.mockRestore();
	});
});

describe("ThroughputRunChartCard's total", () => {
	beforeEach(() => {
		queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
	});

	it("asks the run chart to show the total it counts", () => {
		render(<ThroughputRunChartCard {...buildProps()} />);

		expect(screen.getByTestId("chart-shows-total")).toHaveTextContent("true");
	});
});

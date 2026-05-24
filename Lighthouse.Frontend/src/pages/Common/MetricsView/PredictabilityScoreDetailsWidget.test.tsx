import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import PredictabilityScoreDetailsWidget from "./PredictabilityScoreDetailsWidget";

vi.mock("../../../components/Common/Charts/PredictabilityScore", () => ({
	default: ({ data }: { data: { predictabilityScore: number } }) => (
		<div data-testid="predictability-score">
			{(data.predictabilityScore * 100).toFixed(0)}%
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

const rawScore = new ForecastPredictabilityScore([], 0.6, new Map());
const filteredScore = new ForecastPredictabilityScore([], 0.9, new Map());

const buildMetricsService = (): IMetricsService<IWorkItem> =>
	({
		getMultiItemForecastPredictabilityScore: vi
			.fn()
			.mockResolvedValue(filteredScore),
	}) as unknown as IMetricsService<IWorkItem>;

const buildProps = (
	overrides: Partial<
		React.ComponentProps<typeof PredictabilityScoreDetailsWidget<IWorkItem>>
	> = {},
) => ({
	predictabilityData: rawScore,
	entityId: 7,
	metricsService: buildMetricsService(),
	startDate: new Date("2026-01-01"),
	endDate: new Date("2026-01-31"),
	isPremium: true,
	hasForecastFilter: true,
	...overrides,
});

describe("PredictabilityScoreDetailsWidget", () => {
	beforeEach(() => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	it("renders the raw predictability score on first render", () => {
		render(<PredictabilityScoreDetailsWidget {...buildProps()} />);
		expect(screen.getByTestId("predictability-score")).toHaveTextContent("60%");
	});

	it("renders the filter Switch when premium tenant has a forecast filter configured", () => {
		render(<PredictabilityScoreDetailsWidget {...buildProps()} />);
		expect(
			screen.getByLabelText(/use filtered throughput/i),
		).toBeInTheDocument();
	});

	it("does not render the Switch when the team has no forecast filter", () => {
		render(
			<PredictabilityScoreDetailsWidget
				{...buildProps({ hasForecastFilter: false })}
			/>,
		);
		expect(
			screen.queryByLabelText(/use filtered throughput/i),
		).not.toBeInTheDocument();
	});

	it("does not render the Switch when refetch dependencies are missing", () => {
		render(
			<PredictabilityScoreDetailsWidget
				predictabilityData={rawScore}
				isPremium={true}
				hasForecastFilter={true}
			/>,
		);
		expect(
			screen.queryByLabelText(/use filtered throughput/i),
		).not.toBeInTheDocument();
	});

	it("fetches the filtered score and swaps the displayed value when the toggle flips on", async () => {
		const props = buildProps();
		render(<PredictabilityScoreDetailsWidget {...props} />);

		await userEvent
			.setup()
			.click(screen.getByLabelText(/use filtered throughput/i));

		await waitFor(() => {
			expect(
				props.metricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledWith(7, props.startDate, props.endDate, "filtered");
		});

		await waitFor(() => {
			expect(screen.getByTestId("predictability-score")).toHaveTextContent(
				"90%",
			);
		});
	});

	it("reuses the cached filtered score instead of refetching when the toggle is flipped twice", async () => {
		const props = buildProps();
		render(<PredictabilityScoreDetailsWidget {...props} />);
		const user = userEvent.setup();
		const toggle = screen.getByLabelText(/use filtered throughput/i);

		await user.click(toggle);
		await waitFor(() => {
			expect(
				props.metricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledTimes(1);
		});

		await user.click(toggle);
		await user.click(toggle);

		expect(
			props.metricsService.getMultiItemForecastPredictabilityScore,
		).toHaveBeenCalledTimes(1);
	});
});

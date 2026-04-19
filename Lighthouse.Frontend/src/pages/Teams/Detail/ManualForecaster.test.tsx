import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import dayjs from "dayjs";
import type React from "react";
import type {
	IFeatureCandidate,
	IForecastInputCandidates,
} from "../../../models/Forecasts/ForecastInputCandidates";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import type { IManualForecast } from "../../../models/Forecasts/ManualForecast";
import { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import ManualForecaster from "./ManualForecaster";

// Test data factory function
const getMockManualForecast = (
	overrides?: Partial<IManualForecast>,
): ManualForecast => {
	const defaults: IManualForecast = {
		remainingItems: 10,
		targetDate: new Date(),
		whenForecasts: [
			WhenForecast.new(50, dayjs().add(1, "week").toDate()),
			WhenForecast.new(70, dayjs().add(2, "weeks").toDate()),
		],
		howManyForecasts: [
			new HowManyForecast(60, 15),
			new HowManyForecast(80, 20),
		],
		likelihood: 70,
	};

	const merged = { ...defaults, ...overrides };
	return new ManualForecast(
		merged.remainingItems,
		merged.targetDate,
		merged.whenForecasts,
		merged.howManyForecasts,
		merged.likelihood,
	);
};

vi.mock("@mui/x-date-pickers/DatePicker", () => ({
	DatePicker: ({
		value,
		onChange,
		slotProps,
	}: {
		value: dayjs.Dayjs | null;
		onChange: (date: dayjs.Dayjs | null) => void;
		slotProps?: {
			field?: {
				clearable?: boolean;
			};
		};
	}) => (
		<div data-testid="date-picker-container">
			<input
				type="text"
				data-testid="date-picker-input"
				value={value ? value.format("YYYY-MM-DD") : ""}
				onChange={(e) =>
					onChange(e.target.value ? dayjs(e.target.value) : null)
				}
			/>
			{slotProps?.field?.clearable && value && (
				<button
					type="submit"
					data-testid="date-picker-clear-button"
					onClick={() => onChange(null)}
					aria-label="Clear"
				>
					Clear
				</button>
			)}
		</div>
	),
}));

vi.mock("@mui/x-date-pickers/LocalizationProvider", () => ({
	LocalizationProvider: ({ children }: { children: React.ReactNode }) => (
		<div data-testid="mocked-localization-provider">{children}</div>
	),
	AdapterDayjs: () => null,
}));

vi.mock("../../../components/Common/Forecasts/ForecastInfoList", () => ({
	default: ({ title }: { title: string }) => (
		<div data-testid="forecast-info-list">{title}</div>
	),
}));

vi.mock("../../../components/Common/Forecasts/ForecastLikelihood", () => ({
	default: ({
		likelihood,
	}: {
		howMany: number;
		when: Date;
		likelihood: number;
	}) => (
		<div data-testid="forecast-likelihood">{`Likelihood: ${likelihood}%`}</div>
	),
}));

describe("ManualForecaster component", () => {
	const mockOnRemainingItemsChange = vi.fn();
	const mockOnTargetDateChange = vi.fn();
	const mockOnModeChange = vi.fn();
	const mockOnFeatureSelectionChange = vi.fn();

	const defaultCandidates: IForecastInputCandidates = {
		currentWipCount: 3,
		backlogCount: 12,
		features: [],
	};

	const defaultProps = {
		remainingItems: 10,
		targetDate: null as dayjs.Dayjs | null,
		manualForecastResult: null as ManualForecast | null,
		forecastInputCandidates: null as IForecastInputCandidates | null,
		onRemainingItemsChange: mockOnRemainingItemsChange,
		onTargetDateChange: mockOnTargetDateChange,
		mode: "manual" as "manual" | "features",
		selectedFeatures: [] as IFeatureCandidate[],
		onModeChange: mockOnModeChange,
		onFeatureSelectionChange: mockOnFeatureSelectionChange,
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Intent phrase labels", () => {
		it("should render intent phrase for remaining items input", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(
				screen.getByText("How many Work Items need to be completed?"),
			).toBeInTheDocument();
		});

		it("should render intent phrase for date input", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(
				screen.getByText("What is your target completion date?"),
			).toBeInTheDocument();
		});

		it("should NOT render old 'When?' label", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.queryByText("When?")).not.toBeInTheDocument();
		});

		it("should NOT render old 'How Many?' label", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.queryByText("How Many?")).not.toBeInTheDocument();
		});
	});

	describe("No Forecast button", () => {
		it("should NOT render a Forecast button", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.queryByText("Forecast")).not.toBeInTheDocument();
		});
	});

	describe("Zero-value hint", () => {
		it("should show hint when remainingItems is 0", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={0} />);
			expect(
				screen.getByText(
					/Forecasting requires at least 1 remaining work item/i,
				),
			).toBeInTheDocument();
		});

		it("should not show hint when remainingItems > 0", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={5} />);
			expect(
				screen.queryByText(/Forecasting requires at least 1/i),
			).not.toBeInTheDocument();
		});
	});

	describe("Quick-pick chips for remaining work", () => {
		it("should render WIP and Backlog chips when candidates provided", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					forecastInputCandidates={defaultCandidates}
				/>,
			);
			expect(screen.getByText("Currently in Progress: 3")).toBeInTheDocument();
			expect(screen.getByText("Backlog: 12")).toBeInTheDocument();
		});

		it("should NOT render WIP/Backlog chips when candidates are null", () => {
			render(
				<ManualForecaster {...defaultProps} forecastInputCandidates={null} />,
			);
			expect(screen.queryByText(/WIP:/)).not.toBeInTheDocument();
			expect(screen.queryByText(/Backlog:/)).not.toBeInTheDocument();
		});

		it("should call onRemainingItemsChange with wipCount when WIP chip clicked", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					forecastInputCandidates={defaultCandidates}
				/>,
			);
			fireEvent.click(screen.getByText("Currently in Progress: 3"));
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(3);
		});

		it("should call onRemainingItemsChange with backlogCount when Backlog chip clicked", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					forecastInputCandidates={defaultCandidates}
				/>,
			);
			fireEvent.click(screen.getByText("Backlog: 12"));
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(12);
		});
	});

	describe("Date quick-pick chips", () => {
		it("should render End of week chip", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.getByText("End of week")).toBeInTheDocument();
		});

		it("should render End of month chip", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.getByText("End of month")).toBeInTheDocument();
		});

		it("should render +1 week chip", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.getByText("+1 week")).toBeInTheDocument();
		});

		it("should render +2 weeks chip", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(screen.getByText("+2 weeks")).toBeInTheDocument();
		});

		it("should call onTargetDateChange with end of month date when chip clicked", () => {
			render(<ManualForecaster {...defaultProps} />);
			fireEvent.click(screen.getByText("End of month"));
			expect(mockOnTargetDateChange).toHaveBeenCalledTimes(1);
			const called = mockOnTargetDateChange.mock.calls[0][0] as dayjs.Dayjs;
			expect(called.date()).toBe(dayjs().endOf("month").date());
		});

		it("should call onTargetDateChange ~1 week from today when +1 week chip clicked", () => {
			render(<ManualForecaster {...defaultProps} />);
			fireEvent.click(screen.getByText("+1 week"));
			expect(mockOnTargetDateChange).toHaveBeenCalledTimes(1);
			const called = mockOnTargetDateChange.mock.calls[0][0] as dayjs.Dayjs;
			const expected = dayjs().add(1, "week");
			expect(called.diff(expected, "day")).toBe(0);
		});

		it("should call onTargetDateChange ~2 weeks from today when +2 weeks chip clicked", () => {
			render(<ManualForecaster {...defaultProps} />);
			fireEvent.click(screen.getByText("+2 weeks"));
			expect(mockOnTargetDateChange).toHaveBeenCalledTimes(1);
			const called = mockOnTargetDateChange.mock.calls[0][0] as dayjs.Dayjs;
			const expected = dayjs().add(2, "weeks");
			expect(called.diff(expected, "day")).toBe(0);
		});
	});

	describe("Input interactions", () => {
		it("should render with initial remainingItems value", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={10} />);
			const itemsTextField = screen.getByLabelText("Number of Work Items");
			expect(itemsTextField).toHaveValue(10);
		});

		it("should render empty remaining-items input when value is null", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={null} />);
			const itemsTextField = screen.getByLabelText(
				"Number of Work Items",
			) as HTMLInputElement;
			expect(itemsTextField.value).toBe("");
		});

		it("should call onRemainingItemsChange when items input changes", () => {
			render(<ManualForecaster {...defaultProps} />);
			const itemsTextField = screen.getByLabelText("Number of Work Items");
			fireEvent.change(itemsTextField, { target: { value: "15" } });
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(15);
		});

		it("should call onRemainingItemsChange with null when items input is cleared", () => {
			render(<ManualForecaster {...defaultProps} />);
			const itemsTextField = screen.getByLabelText("Number of Work Items");
			fireEvent.change(itemsTextField, { target: { value: "" } });
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(null);
		});
	});

	describe("Clear functionality", () => {
		it("should display clear button for remaining items when value is set", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={10} />);
			expect(
				screen.getByLabelText("Clear remaining items"),
			).toBeInTheDocument();
		});

		it("should not display clear button for remaining items when value is null", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={null} />);
			expect(
				screen.queryByLabelText("Clear remaining items"),
			).not.toBeInTheDocument();
		});

		it("should call onRemainingItemsChange with null when clear button clicked", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={10} />);
			fireEvent.click(screen.getByLabelText("Clear remaining items"));
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(null);
		});

		it("should display clear button for date picker when date is set", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					targetDate={dayjs().add(2, "weeks")}
				/>,
			);
			expect(
				screen.getByTestId("date-picker-clear-button"),
			).toBeInTheDocument();
		});

		it("should not display clear button for date picker when date is null", () => {
			render(<ManualForecaster {...defaultProps} targetDate={null} />);
			expect(
				screen.queryByTestId("date-picker-clear-button"),
			).not.toBeInTheDocument();
		});

		it("should call onTargetDateChange with null when date clear button clicked", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					targetDate={dayjs().add(2, "weeks")}
				/>,
			);
			fireEvent.click(screen.getByTestId("date-picker-clear-button"));
			expect(mockOnTargetDateChange).toHaveBeenCalledWith(null);
		});
	});

	describe("Conditional result rendering", () => {
		it("should not render any results when manualForecastResult is null", () => {
			render(
				<ManualForecaster {...defaultProps} manualForecastResult={null} />,
			);
			expect(
				screen.queryByTestId("forecast-info-list"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("forecast-likelihood"),
			).not.toBeInTheDocument();
		});

		it("should render only When results when only whenForecasts populated", () => {
			const forecastResult = getMockManualForecast({
				howManyForecasts: [],
				likelihood: 0,
			});
			render(
				<ManualForecaster
					{...defaultProps}
					manualForecastResult={forecastResult}
				/>,
			);
			expect(
				screen.getByText(/When will 10 Work Items be done\?/),
			).toBeInTheDocument();
			expect(
				screen.queryByText(/How Many Work Items will you get done till/),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("forecast-likelihood"),
			).not.toBeInTheDocument();
		});

		it("should render only How Many results when only howManyForecasts populated", () => {
			const forecastResult = getMockManualForecast({
				whenForecasts: [],
				likelihood: 0,
			});
			render(
				<ManualForecaster
					{...defaultProps}
					remainingItems={0}
					targetDate={dayjs()}
					manualForecastResult={forecastResult}
				/>,
			);
			expect(screen.queryByText(/When will/)).not.toBeInTheDocument();
			expect(
				screen.getByText(/How Many Work Items will you get done till/),
			).toBeInTheDocument();
			expect(
				screen.queryByTestId("forecast-likelihood"),
			).not.toBeInTheDocument();
		});

		it("should render both results and likelihood when both forecasts populated", () => {
			const forecastResult = getMockManualForecast({ likelihood: 75 });
			render(
				<ManualForecaster
					{...defaultProps}
					targetDate={dayjs()}
					manualForecastResult={forecastResult}
				/>,
			);
			expect(
				screen.getByText(/When will 10 Work Items be done\?/),
			).toBeInTheDocument();
			expect(
				screen.getByText(/How Many Work Items will you get done till/),
			).toBeInTheDocument();
			expect(screen.getByTestId("forecast-likelihood")).toBeInTheDocument();
		});
	});

	describe("Mode toggle", () => {
		it("should render a mode toggle with Manual and Features options", () => {
			render(<ManualForecaster {...defaultProps} />);
			expect(
				screen.getByRole("group", { name: /forecast mode/i }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Manual" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Features" }),
			).toBeInTheDocument();
		});

		it("should show the Manual button as selected in Manual mode", () => {
			render(<ManualForecaster {...defaultProps} mode="manual" />);
			const manualBtn = screen.getByRole("button", { name: "Manual" });
			expect(manualBtn).toHaveAttribute("aria-pressed", "true");
		});

		it("should call onModeChange with 'features' when Features toggle clicked", () => {
			render(<ManualForecaster {...defaultProps} mode="manual" />);
			fireEvent.click(screen.getByRole("button", { name: "Features" }));
			expect(mockOnModeChange).toHaveBeenCalledWith("features");
		});

		it("should call onModeChange with 'manual' when Manual toggle clicked in Features mode", () => {
			render(<ManualForecaster {...defaultProps} mode="features" />);
			fireEvent.click(screen.getByRole("button", { name: "Manual" }));
			expect(mockOnModeChange).toHaveBeenCalledWith("manual");
		});
	});

	describe("Features mode UI", () => {
		const candidatesWithFeatures: IForecastInputCandidates = {
			currentWipCount: 3,
			backlogCount: 12,
			features: [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
				{ id: 2, name: "Feature Beta", remainingWork: 8 },
				{ id: 3, name: "Feature Gamma", remainingWork: 3 },
			],
		};

		it("should NOT show manual numeric input in Features mode", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(
				screen.queryByLabelText("Number of Work Items"),
			).not.toBeInTheDocument();
		});

		it("should render an autocomplete for selecting features", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(screen.getByRole("combobox")).toBeInTheDocument();
		});

		it("should show unselected features as options in the autocomplete", async () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={[]}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			await userEvent.click(screen.getByRole("combobox"));
			await waitFor(() => {
				expect(
					screen.getByRole("option", { name: "Feature Alpha" }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("option", { name: "Feature Beta" }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("option", { name: "Feature Gamma" }),
				).toBeInTheDocument();
			});
		});

		it("should filter autocomplete options based on typed text", async () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={[]}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			await userEvent.type(screen.getByRole("combobox"), "Alpha");
			await waitFor(() => {
				expect(
					screen.getByRole("option", { name: "Feature Alpha" }),
				).toBeInTheDocument();
				expect(
					screen.queryByRole("option", { name: "Feature Beta" }),
				).not.toBeInTheDocument();
				expect(
					screen.queryByRole("option", { name: "Feature Gamma" }),
				).not.toBeInTheDocument();
			});
		});

		it("should call onFeatureSelectionChange when a feature is selected from the autocomplete", async () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={[]}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			await userEvent.click(screen.getByRole("combobox"));
			await waitFor(() => {
				expect(
					screen.getByRole("option", { name: "Feature Alpha" }),
				).toBeInTheDocument();
			});
			await userEvent.click(
				screen.getByRole("option", { name: "Feature Alpha" }),
			);
			expect(mockOnFeatureSelectionChange).toHaveBeenCalledWith([
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
			]);
		});

		it("should not show already-selected features as options in the autocomplete", async () => {
			const alreadySelected: IFeatureCandidate[] = [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
			];
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={alreadySelected}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			await userEvent.click(screen.getByRole("combobox"));
			await waitFor(() => {
				expect(
					screen.queryByRole("option", { name: "Feature Alpha" }),
				).not.toBeInTheDocument();
				expect(
					screen.getByRole("option", { name: "Feature Beta" }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("option", { name: "Feature Gamma" }),
				).toBeInTheDocument();
			});
		});

		it("should show selected features as chips with a remove button", () => {
			const selectedFeatures: IFeatureCandidate[] = [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
			];
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={selectedFeatures}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(screen.getByLabelText("Remove Feature Alpha")).toBeInTheDocument();
		});

		it("should call onFeatureSelectionChange with feature removed when remove button clicked", () => {
			const selectedFeatures: IFeatureCandidate[] = [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
				{ id: 2, name: "Feature Beta", remainingWork: 8 },
			];
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={selectedFeatures}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			fireEvent.click(screen.getByLabelText("Remove Feature Alpha"));
			expect(mockOnFeatureSelectionChange).toHaveBeenCalledWith([
				{ id: 2, name: "Feature Beta", remainingWork: 8 },
			]);
		});

		it("should show aggregate remaining-work total from selected features", () => {
			const selectedFeatures: IFeatureCandidate[] = [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
				{ id: 2, name: "Feature Beta", remainingWork: 8 },
			];
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={selectedFeatures}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(screen.getByText(/13 work items/i)).toBeInTheDocument();
		});

		it("should show zero-hint when no features are selected in Features mode", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={[]}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(
				screen.getByText(
					/Forecasting requires at least 1 remaining work item/i,
				),
			).toBeInTheDocument();
		});

		it("should show empty-state message when no features are available", () => {
			const candidatesNoFeatures: IForecastInputCandidates = {
				currentWipCount: 3,
				backlogCount: 12,
				features: [],
			};
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					forecastInputCandidates={candidatesNoFeatures}
				/>,
			);
			expect(screen.getByText(/no features available/i)).toBeInTheDocument();
		});

		it("should show the manual numeric input in Manual mode, not the feature autocomplete", () => {
			render(
				<ManualForecaster
					{...defaultProps}
					mode="manual"
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			expect(screen.getByLabelText("Number of Work Items")).toBeInTheDocument();
			expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		});

		it("should carry over feature aggregate to remainingItems when switching from features to manual", () => {
			const selectedFeatures: IFeatureCandidate[] = [
				{ id: 1, name: "Feature Alpha", remainingWork: 5 },
				{ id: 2, name: "Feature Beta", remainingWork: 8 },
			];
			render(
				<ManualForecaster
					{...defaultProps}
					mode="features"
					selectedFeatures={selectedFeatures}
					forecastInputCandidates={candidatesWithFeatures}
				/>,
			);
			fireEvent.click(screen.getByRole("button", { name: "Manual" }));
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(13);
			expect(mockOnModeChange).toHaveBeenCalledWith("manual");
		});
	});
});

import { fireEvent, render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import type React from "react";
import type { IForecastInputCandidates } from "../../../models/Forecasts/ForecastInputCandidates";
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

		it("should call onRemainingItemsChange when items input changes", () => {
			render(<ManualForecaster {...defaultProps} />);
			const itemsTextField = screen.getByLabelText("Number of Work Items");
			fireEvent.change(itemsTextField, { target: { value: "15" } });
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(15);
		});
	});

	describe("Clear functionality", () => {
		it("should display clear button for remaining items when value is set", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={10} />);
			expect(
				screen.getByLabelText("Clear remaining items"),
			).toBeInTheDocument();
		});

		it("should not display clear button for remaining items when value is 0", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={0} />);
			expect(
				screen.queryByLabelText("Clear remaining items"),
			).not.toBeInTheDocument();
		});

		it("should call onRemainingItemsChange with 0 when clear button clicked", () => {
			render(<ManualForecaster {...defaultProps} remainingItems={10} />);
			fireEvent.click(screen.getByLabelText("Clear remaining items"));
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(0);
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
});

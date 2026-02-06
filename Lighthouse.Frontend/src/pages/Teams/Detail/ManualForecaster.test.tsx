import { fireEvent, render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import type React from "react";
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
	const mockOnRunManualForecast = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Initial render and basic interactions", () => {
		it("should render with initial state: remainingItems=10, targetDate=null", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const itemsTextField = screen.getByLabelText("Number of Work Items");
			expect(itemsTextField).toHaveValue(10);
		});

		it("should render 'When?' label above remaining items input", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			expect(screen.getByText("When?")).toBeInTheDocument();
		});

		it("should render 'How Many?' label above target date input", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			expect(screen.getByText("How Many?")).toBeInTheDocument();
		});

		it("should call onRemainingItemsChange when items input changes", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const itemsTextField = screen.getByLabelText("Number of Work Items");
			fireEvent.change(itemsTextField, { target: { value: "15" } });
			expect(mockOnRemainingItemsChange).toHaveBeenCalled();
			expect(mockOnRemainingItemsChange.mock.calls[0][0]).toBe(15);
		});
	});

	describe("Button state management", () => {
		it("should disable Forecast button when both fields are empty", () => {
			render(
				<ManualForecaster
					remainingItems={0}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			expect(forecastButton).toBeDisabled();
		});

		it("should enable Forecast button when only remainingItems has value", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			expect(forecastButton).not.toBeDisabled();
		});

		it("should enable Forecast button when only targetDate has value", () => {
			render(
				<ManualForecaster
					remainingItems={0}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			expect(forecastButton).not.toBeDisabled();
		});

		it("should enable Forecast button when both fields have values", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			expect(forecastButton).not.toBeDisabled();
		});

		it("should call onRunManualForecast when Forecast button is clicked with only remainingItems", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			fireEvent.click(forecastButton);
			expect(mockOnRunManualForecast).toHaveBeenCalled();
		});

		it("should call onRunManualForecast when Forecast button is clicked with only targetDate", () => {
			render(
				<ManualForecaster
					remainingItems={0}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			fireEvent.click(forecastButton);
			expect(mockOnRunManualForecast).toHaveBeenCalled();
		});

		it("should call onRunManualForecast when Forecast button is clicked with both values", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			fireEvent.click(forecastButton);
			expect(mockOnRunManualForecast).toHaveBeenCalled();
		});
	});

	describe("Conditional result rendering", () => {
		it("should not render any results when manualForecastResult is null", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			expect(
				screen.queryByTestId("forecast-info-list"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("forecast-likelihood"),
			).not.toBeInTheDocument();
		});

		it("should render only 'When?' results when only whenForecasts populated", () => {
			const forecastResult = getMockManualForecast({
				howManyForecasts: [],
				likelihood: 0,
			});

			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={forecastResult}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
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

		it("should render only 'How Many?' results when only howManyForecasts populated", () => {
			const forecastResult = getMockManualForecast({
				whenForecasts: [],
				likelihood: 0,
			});

			render(
				<ManualForecaster
					remainingItems={0}
					targetDate={dayjs()}
					manualForecastResult={forecastResult}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
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
			const forecastResult = getMockManualForecast({
				likelihood: 75,
			});

			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs()}
					manualForecastResult={forecastResult}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
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

		it("should not render likelihood when likelihood is 0 even if both forecasts populated", () => {
			const forecastResult = getMockManualForecast({
				likelihood: 0,
			});

			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs()}
					manualForecastResult={forecastResult}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			expect(
				screen.getByText(/When will 10 Work Items be done\?/),
			).toBeInTheDocument();
			expect(
				screen.getByText(/How Many Work Items will you get done till/),
			).toBeInTheDocument();
			expect(
				screen.queryByTestId("forecast-likelihood"),
			).not.toBeInTheDocument();
		});
	});

	describe("Legacy tests - backwards compatibility", () => {
		it("should call onRemainingItemsChange when items input changes", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const itemsTextField = screen.getByLabelText("Number of Work Items");
			fireEvent.change(itemsTextField, { target: { value: "15" } });
			expect(mockOnRemainingItemsChange).toHaveBeenCalled();
			expect(mockOnRemainingItemsChange.mock.calls[0][0]).toBe(15);
		});

		it("should call onRunManualForecast when Forecast button is clicked", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const forecastButton = screen.getByText("Forecast");
			fireEvent.click(forecastButton);
			expect(mockOnRunManualForecast).toHaveBeenCalled();
		});

		it("should render ForecastInfoList and ForecastLikelihood when manualForecastResult is not null", () => {
			const mockManualForecastResult = getMockManualForecast();

			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={mockManualForecastResult}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const whenForecastList = screen.getAllByTestId((id) =>
				id.startsWith("forecast-info-list"),
			);

			for (const element of whenForecastList) {
				expect(element).toBeInTheDocument();
			}

			const howManyForecastList = screen.getByText(
				`How Many Work Items will you get done till ${new Date().toLocaleDateString()}?`,
			);
			const likelihoodComponent = screen.getByTestId("forecast-likelihood");

			expect(howManyForecastList).toBeInTheDocument();
			expect(likelihoodComponent).toBeInTheDocument();
		});
	});

	describe("Clear functionality", () => {
		it("should display clear button for remaining items when value is set", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.getByLabelText("Clear remaining items");
			expect(clearButton).toBeInTheDocument();
		});

		it("should not display clear button for remaining items when value is 0", () => {
			render(
				<ManualForecaster
					remainingItems={0}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.queryByLabelText("Clear remaining items");
			expect(clearButton).not.toBeInTheDocument();
		});

		it("should call onRemainingItemsChange with 0 when remaining items clear button is clicked", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.getByLabelText("Clear remaining items");
			fireEvent.click(clearButton);
			expect(mockOnRemainingItemsChange).toHaveBeenCalledWith(0);
		});

		it("should display clear button for date picker when date is set", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.getByTestId("date-picker-clear-button");
			expect(clearButton).toBeInTheDocument();
		});

		it("should not display clear button for date picker when date is null", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={null}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.queryByTestId("date-picker-clear-button");
			expect(clearButton).not.toBeInTheDocument();
		});

		it("should call onTargetDateChange with null when date picker clear button is clicked", () => {
			render(
				<ManualForecaster
					remainingItems={10}
					targetDate={dayjs().add(2, "weeks")}
					manualForecastResult={null}
					onRemainingItemsChange={mockOnRemainingItemsChange}
					onTargetDateChange={mockOnTargetDateChange}
					onRunManualForecast={mockOnRunManualForecast}
				/>,
			);

			const clearButton = screen.getByTestId("date-picker-clear-button");
			fireEvent.click(clearButton);
			expect(mockOnTargetDateChange).toHaveBeenCalledWith(null);
		});
	});
});

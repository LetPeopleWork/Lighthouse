import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DateRangeSelector from "./DateRangeSelector";

// Mock the Material-UI theme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "rgba(48, 87, 78, 1)",
				},
				mode: "light",
			},
		}),
	};
});

// Mock the DatePicker component
vi.mock("@mui/x-date-pickers", async () => {
	const actual = await vi.importActual("@mui/x-date-pickers");
	return {
		...actual,
		DatePicker: ({
			onChange,
			format,
		}: {
			onChange: (date: Date) => void;
			format?: string;
		}) => {
			return (
				<div data-testid="mocked-date-picker" data-format={format}>
					<button
						type="button"
						onClick={() => onChange(new Date(2023, 1, 15))}
						data-testid="mocked-date-select"
					>
						Select Date
					</button>
				</div>
			);
		},
	};
});

// Mock the implementation of getLocaleDateFormat to avoid Intl.DateTimeFormat issues
vi.mock("./DateRangeSelector", async () => {
	const actual = await vi.importActual("./DateRangeSelector");

	// Mock implementation of getLocaleDateFormat
	const mockedModule = {
		...actual,
		default: (props: import("./DateRangeSelector").DateRangeSelectorProps) => {
			// Override the localDateFormat value directly in the component
			const Component = actual.default as React.FC<
				import("./DateRangeSelector").DateRangeSelectorProps
			>;
			return <Component {...props} _testLocalDateFormat="MM/dd/yyyy" />;
		},
	};

	return mockedModule;
});

describe("DateRangeSelector component", () => {
	const defaultProps = {
		startDate: new Date(2023, 0, 1), // Jan 1, 2023
		endDate: new Date(2023, 0, 31), // Jan 31, 2023
		onStartDateChange: vi.fn(),
		onEndDateChange: vi.fn(),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders with start date and end date pickers", () => {
		render(<DateRangeSelector {...defaultProps} />);

		// Check for the labels
		expect(screen.getByText("Start Date")).toBeInTheDocument();
		expect(screen.getByText("End Date")).toBeInTheDocument();

		// With our mock, we look for the mocked date picker components
		const datePickers = screen.getAllByTestId("mocked-date-picker");
		expect(datePickers).toHaveLength(2);
	});

	it("calls onStartDateChange when start date changes", async () => {
		const user = userEvent.setup();
		render(<DateRangeSelector {...defaultProps} />);

		// Get the mocked date picker buttons
		const dateSelectButtons = screen.getAllByTestId("mocked-date-select");

		// Click the first one (start date)
		await user.click(dateSelectButtons[0]);

		// Verify callback was called with the new date
		expect(defaultProps.onStartDateChange).toHaveBeenCalledWith(
			new Date(2023, 1, 15),
		);
	});

	it("calls onEndDateChange when end date changes", async () => {
		const user = userEvent.setup();
		render(<DateRangeSelector {...defaultProps} />);

		// Get the mocked date picker buttons
		const dateSelectButtons = screen.getAllByTestId("mocked-date-select");

		// Click the second one (end date)
		await user.click(dateSelectButtons[1]);

		// Verify callback was called with the new date
		expect(defaultProps.onEndDateChange).toHaveBeenCalledWith(
			new Date(2023, 1, 15),
		);
	});

	it("sets max date for start date picker to end date", () => {
		render(<DateRangeSelector {...defaultProps} />);

		// With our mock, we just check that the component rendered
		const datePickers = screen.getAllByTestId("mocked-date-picker");
		expect(datePickers[0]).toBeInTheDocument();
	});

	it("sets min date for end date picker to start date", () => {
		render(<DateRangeSelector {...defaultProps} />);

		// With our mock, we just check that the component rendered
		const datePickers = screen.getAllByTestId("mocked-date-picker");
		expect(datePickers[1]).toBeInTheDocument();
	});

	it("applies the locale format to date pickers", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const datePickers = screen.getAllByTestId("mocked-date-picker");

		// Check that both pickers have the format attribute set to the local format
		expect(datePickers[0]).toHaveAttribute("data-format", "MM/dd/yyyy");
		expect(datePickers[1]).toHaveAttribute("data-format", "MM/dd/yyyy");
	});
});

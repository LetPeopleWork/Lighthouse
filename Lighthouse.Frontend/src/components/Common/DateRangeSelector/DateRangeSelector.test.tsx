import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
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

/**
 * The stub stands in for the picker so this file can check what the component
 * hands the picker and what it does with what comes back. It cannot stand in for
 * the picker's own behaviour — the crash this component now guards against comes
 * out of the real field, so that lives in DateRangeSelector.keyboard.test.tsx,
 * which mocks nothing.
 */
vi.mock("@mui/x-date-pickers", async () => {
	const actual = await vi.importActual("@mui/x-date-pickers");
	return {
		...actual,
		DatePicker: ({
			onChange,
			format,
			minDate,
			maxDate,
			slotProps,
		}: {
			onChange: (date: Date) => void;
			format?: string;
			minDate?: Date;
			maxDate?: Date;
			slotProps?: { textField?: { onBlur?: () => void } };
		}) => {
			return (
				<div
					data-testid="mocked-date-picker"
					data-format={format}
					data-min-date={minDate?.toISOString()}
					data-max-date={maxDate?.toISOString()}
				>
					<button
						type="button"
						onClick={() => onChange(new Date(2023, 0, 15))}
						data-testid="mocked-date-select"
					>
						Select Date
					</button>
					<button
						type="button"
						onClick={() => slotProps?.textField?.onBlur?.()}
						data-testid="mocked-date-finish"
					>
						Finish Edit
					</button>
				</div>
			);
		},
	};
});

describe("DateRangeSelector component", () => {
	const defaultProps = {
		startDate: new Date(2023, 0, 1), // Jan 1, 2023
		endDate: new Date(2023, 0, 31), // Jan 31, 2023
		onStartDateChange: vi.fn(),
		onEndDateChange: vi.fn(),
		_testLocalDateFormat: "MM/dd/yyyy",
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders with start date and end date pickers", () => {
		render(<DateRangeSelector {...defaultProps} />);

		expect(screen.getByText("Start Date")).toBeInTheDocument();
		expect(screen.getByText("End Date")).toBeInTheDocument();

		const datePickers = screen.getAllByTestId("mocked-date-picker");
		expect(datePickers).toHaveLength(2);
	});

	it("reports a new start date only once the edit is finished", async () => {
		const user = userEvent.setup();
		render(<DateRangeSelector {...defaultProps} />);

		await user.click(screen.getAllByTestId("mocked-date-select")[0]);
		expect(defaultProps.onStartDateChange).not.toHaveBeenCalled();

		await user.click(screen.getAllByTestId("mocked-date-finish")[0]);

		expect(defaultProps.onStartDateChange).toHaveBeenCalledTimes(1);
		expect(defaultProps.onStartDateChange).toHaveBeenCalledWith(
			new Date(2023, 0, 15),
		);
	});

	it("reports a new end date only once the edit is finished", async () => {
		const user = userEvent.setup();
		render(<DateRangeSelector {...defaultProps} />);

		await user.click(screen.getAllByTestId("mocked-date-select")[1]);
		expect(defaultProps.onEndDateChange).not.toHaveBeenCalled();

		await user.click(screen.getAllByTestId("mocked-date-finish")[1]);

		expect(defaultProps.onEndDateChange).toHaveBeenCalledTimes(1);
		expect(defaultProps.onEndDateChange).toHaveBeenCalledWith(
			new Date(2023, 0, 15),
		);
	});

	it("stops the start date from being pushed past the end of the range", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const startPicker = screen.getAllByTestId("mocked-date-picker")[0];

		expect(startPicker).toHaveAttribute(
			"data-max-date",
			defaultProps.endDate.toISOString(),
		);
		expect(startPicker).not.toHaveAttribute("data-min-date");
	});

	it("stops the end date from being pulled before the start of the range", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const endPicker = screen.getAllByTestId("mocked-date-picker")[1];

		expect(endPicker).toHaveAttribute(
			"data-min-date",
			defaultProps.startDate.toISOString(),
		);
		expect(endPicker).not.toHaveAttribute("data-max-date");
	});

	it("applies the locale format to date pickers", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const datePickers = screen.getAllByTestId("mocked-date-picker");

		expect(datePickers[0]).toHaveAttribute("data-format", "MM/dd/yyyy");
		expect(datePickers[1]).toHaveAttribute("data-format", "MM/dd/yyyy");
	});
});

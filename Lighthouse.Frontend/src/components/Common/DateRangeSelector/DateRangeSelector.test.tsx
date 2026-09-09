import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DateWindowPreset } from "../../../pages/Common/MetricsView/dateWindow";
import DateRangeSelector from "./DateRangeSelector";

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

const START = 0;
const END = 1;

const pickerFor = (field: number) =>
	screen.getAllByTestId("mocked-date-picker")[field];

const selectDateIn = (field: number) =>
	screen.getAllByTestId("mocked-date-select")[field];

const finishEditIn = (field: number) =>
	screen.getAllByTestId("mocked-date-finish")[field];

describe("DateRangeSelector component", () => {
	const defaultProps = {
		startDate: new Date(2023, 0, 1),
		endDate: new Date(2023, 0, 31),
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

		await user.click(selectDateIn(START));
		expect(defaultProps.onStartDateChange).not.toHaveBeenCalled();

		await user.click(finishEditIn(START));

		expect(defaultProps.onStartDateChange).toHaveBeenCalledTimes(1);
		expect(defaultProps.onStartDateChange).toHaveBeenCalledWith(
			new Date(2023, 0, 15),
		);
	});

	it("reports a new end date only once the edit is finished", async () => {
		const user = userEvent.setup();
		render(<DateRangeSelector {...defaultProps} />);

		await user.click(selectDateIn(END));
		expect(defaultProps.onEndDateChange).not.toHaveBeenCalled();

		await user.click(finishEditIn(END));

		expect(defaultProps.onEndDateChange).toHaveBeenCalledTimes(1);
		expect(defaultProps.onEndDateChange).toHaveBeenCalledWith(
			new Date(2023, 0, 15),
		);
	});

	it("stops the start date from being pushed past the end of the range", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const startPicker = pickerFor(START);

		expect(startPicker).toHaveAttribute(
			"data-max-date",
			defaultProps.endDate.toISOString(),
		);
		expect(startPicker).not.toHaveAttribute("data-min-date");
	});

	it("stops the end date from being pulled before the start of the range", () => {
		render(<DateRangeSelector {...defaultProps} />);

		const endPicker = pickerFor(END);

		expect(endPicker).toHaveAttribute(
			"data-min-date",
			defaultProps.startDate.toISOString(),
		);
		expect(endPicker).not.toHaveAttribute("data-max-date");
	});

	it("applies the locale format to date pickers", () => {
		render(<DateRangeSelector {...defaultProps} />);

		expect(pickerFor(START)).toHaveAttribute("data-format", "MM/dd/yyyy");
		expect(pickerFor(END)).toHaveAttribute("data-format", "MM/dd/yyyy");
	});

	describe("preset chip row", () => {
		const presets: readonly DateWindowPreset[] = [
			{ label: "Last 7 days", days: 7 },
			{ label: "Last 30 days", days: 30 },
		];

		const renderWithPresets = (onSelectPreset = vi.fn()) => {
			render(
				<DateRangeSelector
					{...defaultProps}
					presets={presets}
					selectedPresetDays={30}
					onSelectPreset={onSelectPreset}
				/>,
			);

			return onSelectPreset;
		};

		const chipFor = (label: string) =>
			screen.getByText(label).closest("[role='button']") as HTMLElement;

		// The mocked picker contributes buttons of its own, so a bare button query would
		// count those too. Only a chip says which window it stands for.
		const presetChips = () =>
			screen
				.queryAllByRole("button")
				.filter((element) => element.hasAttribute("aria-pressed"));

		it("offers one chip per preset, marking the window on show, ahead of the pickers", () => {
			renderWithPresets();

			expect(presetChips()).toHaveLength(presets.length);
			expect(chipFor("Last 7 days")).toHaveAttribute("aria-pressed", "false");
			expect(chipFor("Last 30 days")).toHaveAttribute("aria-pressed", "true");

			const followsChips =
				chipFor("Last 7 days").compareDocumentPosition(
					screen.getByText("Start Date"),
				) & Node.DOCUMENT_POSITION_FOLLOWING;
			expect(followsChips).toBeTruthy();
		});

		it("forwards a chip click to the handler it was given", async () => {
			const user = userEvent.setup();
			const onSelectPreset = renderWithPresets();

			await user.click(screen.getByText("Last 7 days"));

			expect(onSelectPreset).toHaveBeenCalledTimes(1);
			expect(onSelectPreset).toHaveBeenCalledWith(7);
		});

		it("shows no chip row at all when it was given no presets", () => {
			render(<DateRangeSelector {...defaultProps} />);

			expect(presetChips()).toHaveLength(0);
		});

		// The three preset props have to arrive together. Passing them one at a time is what a
		// half-wired caller does, and each case has to leave the row off rather than render an empty
		// row or a chip nothing is listening to.
		it("shows no chip row, but still the pickers, for an empty list of presets", () => {
			render(
				<DateRangeSelector
					{...defaultProps}
					presets={[]}
					onSelectPreset={vi.fn()}
				/>,
			);

			expect(presetChips()).toHaveLength(0);
			expect(screen.getByText("Start Date")).toBeInTheDocument();
		});

		it("shows no chip row when nothing is listening for a choice", () => {
			render(<DateRangeSelector {...defaultProps} presets={presets} />);

			expect(presetChips()).toHaveLength(0);
		});
	});
});

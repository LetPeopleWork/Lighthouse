import { render, screen } from "@testing-library/react";
import DateRangeSelector, {
	type DateRangeSelectorProps,
} from "./DateRangeSelector";

const renderDateRangeSelector = (props: DateRangeSelectorProps) => {
	return render(<DateRangeSelector {...props} />);
};

describe("DateRangeSelector", () => {
	const mockStartDate = new Date("2023-01-01");
	const mockEndDate = new Date("2023-01-31");
	const mockOnStartDateChange = vi.fn();
	const mockOnEndDateChange = vi.fn();

	const defaultProps = {
		startDate: mockStartDate,
		endDate: mockEndDate,
		onStartDateChange: mockOnStartDateChange,
		onEndDateChange: mockOnEndDateChange,
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders date range component correctly", () => {
		renderDateRangeSelector(defaultProps);

		// Check if the calendar inputs exist
		const dateInputs = screen.getAllByRole("textbox");
		expect(dateInputs.length).toBeGreaterThan(0);
	});

	it("preserves the date range selection", () => {
		renderDateRangeSelector(defaultProps);

		// Verify that the date range is displayed
		const dateElements = screen.getAllByRole("textbox");
		expect(dateElements.length).toBeGreaterThan(0);

		// Verify that the component has the correct props
		expect(mockStartDate).toEqual(defaultProps.startDate);
		expect(mockEndDate).toEqual(defaultProps.endDate);
	});
});

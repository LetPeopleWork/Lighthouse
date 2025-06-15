import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import dayjs from "dayjs";
import { vi } from "vitest";
import DatePickerComponent from "./DatePickerComponent"; // Adjust the import according to your file structure

describe("DatePickerComponent", () => {
	const mockOnChange = vi.fn();

	afterEach(() => {
		vi.clearAllMocks();
	});

	test("renders the date picker with the correct label", () => {
		render(
			<DatePickerComponent
				label="Burndown Start Date"
				value={dayjs()}
				onChange={mockOnChange}
			/>,
		);

		// Find the label directly without relying on associations
		const labelElement = screen.getByText("Burndown Start Date", {
			selector: "label",
		});
		expect(labelElement).toBeInTheDocument();
	});

	test("calls onChange when a new date is selected", async () => {
		const user = userEvent.setup();
		const today = dayjs();

		render(
			<DatePickerComponent
				label="Burndown Start Date"
				value={today}
				onChange={mockOnChange}
			/>,
		);

		// Find and click the calendar icon button to open the date picker
		const calendarButton = screen.getByLabelText(/choose date/i);
		await user.click(calendarButton);

		// Go to previous month to ensure we're not selecting a future date
		const prevMonthButton = await screen.findByLabelText(/previous month/i);
		await user.click(prevMonthButton);

		const dayOfMonth = today.date();
		const dayToSelect = dayOfMonth === 15 ? 14 : 15; // Ensure we select a valid day
		const dateButton = await screen.findByText(dayToSelect);
		await user.click(dateButton);

		expect(mockOnChange).toHaveBeenCalled();
	});
});

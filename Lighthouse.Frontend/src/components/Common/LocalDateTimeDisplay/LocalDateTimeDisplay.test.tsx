import { render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import LocalDateTimeDisplay from "./LocalDateTimeDisplay";

describe("LocalDateTimeDisplay", () => {
	const testCases = [
		{
			utcDate: new Date("2024-06-24T12:00:00Z"),
			showTime: true,
			expected: "6/24/2024, 12:00:00 PM",
		},
		{
			utcDate: new Date("2024-06-24T12:00:00Z"),
			showTime: false,
			expected: "6/24/2024",
		},
		{
			utcDate: "2024-06-24T12:00:00Z",
			showTime: true,
			expected: "6/24/2024, 12:00:00 PM",
		},
		{ utcDate: "2024-06-24T12:00:00Z", showTime: false, expected: "6/24/2024" },
	];

	const mockToLocaleString = vi.fn(() => "6/24/2024, 12:00:00 PM");
	const mockToLocaleDateString = vi.fn(() => "6/24/2024");

	beforeEach(() => {
		vi.spyOn(Date.prototype, "toLocaleString").mockImplementation(
			mockToLocaleString,
		);
		vi.spyOn(Date.prototype, "toLocaleDateString").mockImplementation(
			mockToLocaleDateString,
		);
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	test.each(testCases)(
		"renders the correct local date and time string",
		({ utcDate, showTime, expected }) => {
			render(<LocalDateTimeDisplay utcDate={utcDate} showTime={showTime} />);
			const dateTimeElement = screen.getByText(expected);
			expect(dateTimeElement).toBeInTheDocument();
		},
	);

	test("handles invalid date string gracefully", () => {
		render(<LocalDateTimeDisplay utcDate="invalid-date-string" />);
		const dateTimeElement = screen.getByText("Invalid Date");
		expect(dateTimeElement).toBeInTheDocument();
	});
});

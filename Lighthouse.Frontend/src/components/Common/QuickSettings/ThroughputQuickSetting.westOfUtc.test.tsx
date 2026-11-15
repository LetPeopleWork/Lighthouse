import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ThroughputQuickSetting from "./ThroughputQuickSetting";

// The backend sends a team's throughput window as UTC midnights. In New York those instants fall on
// the evening of the day before, so a window read back by the local calendar starts and ends a day early.
const STORED_START = new Date("2024-01-01T00:00:00Z");
const STORED_END = new Date("2024-01-31T00:00:00Z");

const renderFixedWindow = (onSave = vi.fn().mockResolvedValue(undefined)) =>
	render(
		<ThroughputQuickSetting
			useFixedDates
			startDate={STORED_START}
			endDate={STORED_END}
			onSave={onSave}
		/>,
	);

describe("ThroughputQuickSetting west of UTC", () => {
	it("names the days the team stored on the button", () => {
		renderFixedWindow();

		expect(
			screen.getByRole("button", {
				name: /Throughput: Fixed dates 2024-01-01 to 2024-01-31/i,
			}),
		).toBeInTheDocument();
	});

	it("shows the stored days in the date fields", async () => {
		const user = userEvent.setup();
		renderFixedWindow();

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		expect(screen.getByLabelText<HTMLInputElement>(/Start Date/i).value).toBe(
			"2024-01-01",
		);
		expect(screen.getByLabelText<HTMLInputElement>(/End Date/i).value).toBe(
			"2024-01-31",
		);
	});

	it("keeps a picked day in the field and saves that day", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		renderFixedWindow(onSave);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));
		const startInput = screen.getByLabelText<HTMLInputElement>(/Start Date/i);
		await user.clear(startInput);
		await user.type(startInput, "2024-01-05");

		expect(startInput.value).toBe("2024-01-05");

		await user.keyboard("{Enter}");

		await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
		const savedStart = onSave.mock.calls[0][2] as Date;
		expect(savedStart.toISOString()).toBe("2024-01-05T00:00:00.000Z");
	});
});

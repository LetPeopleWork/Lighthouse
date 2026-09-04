import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DateRangeSelector from "./DateRangeSelector";

/**
 * These tests drive the real @mui/x-date-pickers field on purpose. The bug they
 * guard lives in the seam between what the picker emits and what this component
 * forwards, so a stubbed picker cannot see it — every earlier test here mocked
 * the picker away and the crash shipped anyway.
 *
 * Two things to know if this file ever breaks after a picker upgrade. The field
 * renders one `role="spinbutton"` per section, addressed by its aria-label
 * ("Day", "Month", "Year"); `getAllByRole("textbox")` finds nothing at all. And
 * the format is pinned via `_testLocalDateFormat` so the assertions do not
 * depend on the machine's Intl locale.
 */

const START_DATE = new Date(2026, 6, 15);
const END_DATE = new Date(2026, 7, 15);

const renderSelector = () => {
	const onStartDateChange = vi.fn();
	const onEndDateChange = vi.fn();

	const view = render(
		<>
			<DateRangeSelector
				startDate={START_DATE}
				endDate={END_DATE}
				onStartDateChange={onStartDateChange}
				onEndDateChange={onEndDateChange}
				_testLocalDateFormat="dd.MM.yyyy"
			/>
			<button type="button">outside</button>
		</>,
	);

	return {
		view,
		onStartDateChange,
		onEndDateChange,
		user: userEvent.setup(),
	};
};

const startSection = (label: string) =>
	screen.getAllByRole("spinbutton", { name: label })[0];

const endSection = (label: string) =>
	screen.getAllByRole("spinbutton", { name: label })[1];

const committedValue = (
	mock: ReturnType<typeof vi.fn>,
	fallback: Date,
): Date => {
	const calls = mock.mock.calls;
	return calls.length > 0 ? calls[calls.length - 1][0] : fallback;
};

const clickOutside = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByRole("button", { name: "outside" }));
};

describe("DateRangeSelector keyboard entry", () => {
	it("never reports an unparseable date when a zero is typed into the day", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("0");

		for (const [reported] of onStartDateChange.mock.calls) {
			expect(Number.isNaN(reported?.getTime())).toBe(false);
		}
	});

	it("never reports an unparseable date when a zero is typed into the month", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Month"));
		await user.keyboard("0");

		for (const [reported] of onStartDateChange.mock.calls) {
			expect(Number.isNaN(reported?.getTime())).toBe(false);
		}
	});

	it("keeps the previously working date after an unusable keystroke", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("0");

		expect(committedValue(onStartDateChange, START_DATE)).toEqual(START_DATE);
	});

	it("reports a typed start date once, when the edit is finished", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("05072026");
		await clickOutside(user);

		expect(onStartDateChange).toHaveBeenCalledTimes(1);
		expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5));
	});

	it("reports a typed end date once, when the edit is finished", async () => {
		const { user, onEndDateChange } = renderSelector();

		await user.click(endSection("Day"));
		await user.keyboard("20082026");
		await clickOutside(user);

		expect(onEndDateChange).toHaveBeenCalledTimes(1);
		expect(onEndDateChange).toHaveBeenCalledWith(new Date(2026, 7, 20));
	});

	it("rejects a finished date beyond the end of the range", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("01092026");
		await clickOutside(user);

		expect(committedValue(onStartDateChange, START_DATE)).toEqual(START_DATE);
	});

	it("rejects a finished date before the start of the range", async () => {
		const { user, onEndDateChange } = renderSelector();

		await user.click(endSection("Day"));
		await user.keyboard("01012020");
		await clickOutside(user);

		expect(committedValue(onEndDateChange, END_DATE)).toEqual(END_DATE);
	});

	it("reports a finished date when the popover closes instead of blurring", async () => {
		const { user, view, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("05072026");
		view.unmount();

		expect(onStartDateChange).toHaveBeenCalledTimes(1);
		expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5));
	});
});

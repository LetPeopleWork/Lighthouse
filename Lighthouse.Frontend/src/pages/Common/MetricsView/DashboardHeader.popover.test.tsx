import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import DashboardHeader from "./DashboardHeader";

/**
 * The sibling test file stubs the date range selector away, so it can never see
 * what happens when a real date field is torn down by a real popover. This file
 * mounts both for real, because that teardown is where a typed date can vanish:
 * the popover removes its children on close and the browser never reports that
 * the field lost focus, so a user types a date, clicks away, and the dashboard
 * silently keeps the old range.
 *
 * Two things to know if this file ever breaks after a picker upgrade. The field
 * renders one `role="spinbutton"` per section, addressed by its aria-label
 * ("Day", "Month", "Year"); `getAllByRole("textbox")` finds nothing at all. And
 * each section is filled by its own label rather than by typing straight
 * through, so the assertions do not depend on the order the machine's locale
 * happens to put day, month and year in.
 */

const START_DATE = new Date(2026, 6, 15);
const END_DATE = new Date(2026, 7, 15);

const renderHeader = () => {
	const onStartDateChange = vi.fn();

	const Harness = () => {
		const [startDate, setStartDate] = useState(START_DATE);

		return (
			<DashboardHeader
				startDate={startDate}
				endDate={END_DATE}
				onStartDateChange={(date) => {
					onStartDateChange(date);
					if (date) {
						setStartDate(date);
					}
				}}
				onEndDateChange={vi.fn()}
				selectedCategory="flow-overview"
				onSelectCategory={vi.fn()}
				showTips={false}
				onToggleTips={vi.fn()}
			/>
		);
	};

	render(<Harness />);

	return { onStartDateChange, user: userEvent.setup() };
};

// `hidden: true` is not optional here. The popover renders in place instead of
// in a portal, so MUI marks everything around it — including the element the
// test renders into — as hidden from screen readers, and a plain role query
// then finds none of the date sections.
const startSection = (label: string) =>
	screen.getAllByRole("spinbutton", { name: label, hidden: true })[0];

const openDateRange = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByTestId("dashboard-date-range-toggle"));
	await screen.findByText("Start Date");
};

const typeStartDate = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(startSection("Day"));
	await user.keyboard("05");
	await user.click(startSection("Month"));
	await user.keyboard("07");
	await user.click(startSection("Year"));
	await user.keyboard("2026");
};

const clickBackdrop = async (user: ReturnType<typeof userEvent.setup>) => {
	const backdrop = document.querySelector(".MuiBackdrop-root");
	if (!backdrop) {
		throw new Error("the popover rendered without a backdrop to click");
	}

	await user.click(backdrop);
};

describe("DashboardHeader date range popover", () => {
	it("keeps a date typed into the popover after Escape closes it", async () => {
		const { user, onStartDateChange } = renderHeader();

		await openDateRange(user);
		await typeStartDate(user);
		await user.keyboard("{Escape}");

		await waitFor(() =>
			expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5)),
		);
		expect(onStartDateChange).toHaveBeenCalledTimes(1);
	});

	it("keeps a date typed into the popover after a click on the backdrop", async () => {
		const { user, onStartDateChange } = renderHeader();

		await openDateRange(user);
		await typeStartDate(user);
		await clickBackdrop(user);

		await waitFor(() =>
			expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5)),
		);
		expect(onStartDateChange).toHaveBeenCalledTimes(1);
	});

	it("shows the date typed into the popover once it is closed", async () => {
		const { user } = renderHeader();

		await openDateRange(user);
		await typeStartDate(user);
		await user.keyboard("{Escape}");

		await screen.findByText(/05 Jul 2026/);
	});
});

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
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

const clickOutside = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(screen.getByRole("button", { name: "outside" }));
};

describe("DateRangeSelector keyboard entry", () => {
	// A zero on its own spells "00", which the field hands out as an unparseable
	// date. Nothing at all reaching the parent is the assertion: the shipped crash
	// was that half-date arriving as if it were a choice the user had made, so a
	// call list that merely happens to hold valid dates would not tell them apart.
	it("reports nothing while a zero sits half-typed in the day", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("0");

		expect(onStartDateChange).not.toHaveBeenCalled();
	});

	it("reports nothing while a zero sits half-typed in the month", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Month"));
		await user.keyboard("0");

		expect(onStartDateChange).not.toHaveBeenCalled();
	});

	it("keeps the previously working date when an edit finishes on an unusable keystroke", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("0");
		await clickOutside(user);

		expect(onStartDateChange).not.toHaveBeenCalled();
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

		expect(onStartDateChange).not.toHaveBeenCalled();
	});

	it("rejects a finished date before the start of the range", async () => {
		const { user, onEndDateChange } = renderSelector();

		await user.click(endSection("Day"));
		await user.keyboard("01012020");
		await clickOutside(user);

		expect(onEndDateChange).not.toHaveBeenCalled();
	});

	it("reports a finished date when the popover closes instead of blurring", async () => {
		const { user, view, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("05072026");
		view.unmount();

		expect(onStartDateChange).toHaveBeenCalledTimes(1);
		expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5));
	});

	it("puts the last working date back in the field when an edit finishes on an unusable keystroke", async () => {
		const { user } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("0");
		await clickOutside(user);

		// Nothing is reported here, so what the field shows is all the person has
		// left to go on — and a day reading "00" is the reported symptom itself.
		expect(startSection("Day")).toHaveTextContent("15");
	});

	it("reports a start date that lands exactly on the end of the range", async () => {
		const { user, onStartDateChange } = renderSelector();

		await user.click(startSection("Day"));
		await user.keyboard("15082026");
		await clickOutside(user);

		// A single-day range is one someone can legitimately ask for; only dates
		// past the other edge are out of bounds.
		expect(onStartDateChange).toHaveBeenCalledWith(END_DATE);
	});

	it("reports an end date that lands exactly on the start of the range", async () => {
		const { user, onEndDateChange } = renderSelector();

		await user.click(endSection("Day"));
		await user.keyboard("15072026");
		await clickOutside(user);

		expect(onEndDateChange).toHaveBeenCalledWith(START_DATE);
	});

	it("reports a date picked from the calendar once, then closes the calendar", async () => {
		const { user, onStartDateChange } = renderSelector();

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

		await user.click(
			screen.getAllByRole("button", { name: /choose date/i })[0],
		);
		await user.click(await screen.findByRole("gridcell", { name: "5" }));

		// The picker also announces an accepted value on the way through a typed
		// date, so an acceptance only means a day was chosen when the calendar is
		// what was open at the time.
		expect(onStartDateChange).toHaveBeenCalledTimes(1);
		expect(onStartDateChange).toHaveBeenCalledWith(new Date(2026, 6, 5));
		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
	});
});

/**
 * The range can also move without anyone touching this field — a preset button, a
 * URL the dashboard was opened with, the other field pushing this one along. The
 * field keeps a draft of its own while an edit is in progress, so it has to notice
 * when the range it is drafting against has been replaced underneath it, or it
 * goes on showing a date the dashboard is no longer using.
 */
describe("DateRangeSelector when the range moves underneath it", () => {
	const MOVED_START_DATE = new Date(2026, 2, 9);

	const Harness = ({ startDate }: { startDate: Date }) => (
		<DateRangeSelector
			startDate={startDate}
			endDate={END_DATE}
			onStartDateChange={vi.fn()}
			onEndDateChange={vi.fn()}
			_testLocalDateFormat="dd.MM.yyyy"
		/>
	);

	const renderSelectorWithMovableRange = () => {
		const view = render(<Harness startDate={START_DATE} />);

		return {
			user: userEvent.setup(),
			moveRangeStartTo: (startDate: Date) =>
				view.rerender(<Harness startDate={startDate} />),
		};
	};

	const expectStartFieldToRead = (day: string, month: string, year: string) => {
		expect(startSection("Day")).toHaveTextContent(day);
		expect(startSection("Month")).toHaveTextContent(month);
		expect(startSection("Year")).toHaveTextContent(year);
	};

	it("shows the new start date when nobody was editing the field", () => {
		const { moveRangeStartTo } = renderSelectorWithMovableRange();

		moveRangeStartTo(MOVED_START_DATE);

		expectStartFieldToRead("09", "03", "2026");
	});

	it("drops a half-typed date in favour of the new start date", async () => {
		const { user, moveRangeStartTo } = renderSelectorWithMovableRange();

		await user.click(startSection("Day"));
		await user.keyboard("0");
		moveRangeStartTo(MOVED_START_DATE);

		expectStartFieldToRead("09", "03", "2026");
	});

	it("shows the new start date after the field was emptied", async () => {
		const { user, moveRangeStartTo } = renderSelectorWithMovableRange();

		// An emptied field holds no date at all rather than an unusable one, which
		// is the case a comparison against the draft has to survive.
		for (const section of ["Day", "Month", "Year"]) {
			await user.click(startSection(section));
			await user.keyboard("{Delete}");
		}
		moveRangeStartTo(MOVED_START_DATE);

		expectStartFieldToRead("09", "03", "2026");
	});
});

/**
 * Left to itself the field writes dates the way the picker library's own default
 * locale does — American order, with slashes — no matter who is reading it. The
 * order below is the one a German-speaking viewer expects, and it is the browser's
 * locale that has to decide it.
 */
describe("DateRangeSelector date format", () => {
	afterEach(() => {
		vi.restoreAllMocks();
	});

	const pinBrowserLocaleTo = (locale: string) => {
		const RealDateTimeFormat = Intl.DateTimeFormat;

		function LocalisedDateTimeFormat(
			locales?: Intl.LocalesArgument,
			options?: Intl.DateTimeFormatOptions,
		) {
			return new RealDateTimeFormat(locales ?? locale, options);
		}

		vi.spyOn(Intl, "DateTimeFormat").mockImplementation(
			LocalisedDateTimeFormat as unknown as typeof Intl.DateTimeFormat,
		);
	};

	it("orders the field's sections the way the viewer's own locale writes a date", () => {
		pinBrowserLocaleTo("de-DE");

		render(
			<DateRangeSelector
				startDate={START_DATE}
				endDate={END_DATE}
				onStartDateChange={vi.fn()}
				onEndDateChange={vi.fn()}
			/>,
		);

		expect(
			screen
				.getAllByRole("spinbutton")
				.slice(0, 3)
				.map((section) => section.getAttribute("aria-label")),
		).toEqual(["Day", "Month", "Year"]);
	});
});

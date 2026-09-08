import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useDateRange } from "./useDateRange";

// The quiet period a burst of stepper clicks settles into before anything is committed or fetched.
const QUIET_PERIOD_MS = 500;

// Every window change that commits resets the visited-category set and refetches the whole category,
// so "how many times were the params written" is the direct measure of the refetch storm this
// feature has to avoid. Recording the writes is the only way a test can see it.
const searchParamWrites: Array<{
	params: URLSearchParams;
	options: { replace?: boolean } | undefined;
}> = [];

vi.mock("react-router", async (importOriginal) => {
	const actual = await importOriginal<typeof import("react-router")>();
	return {
		...actual,
		useSearchParams: () => {
			const [params, setParams] = actual.useSearchParams();
			const record = (
				next: URLSearchParams,
				options?: { replace?: boolean },
			) => {
				searchParamWrites.push({
					params: new URLSearchParams(next),
					options,
				});
				setParams(next, options);
			};
			return [params, record];
		},
	};
});

const localDay = (year: number, month1Based: number, day: number): Date =>
	new Date(year, month1Based - 1, day);

const asLocalIso = (date: Date): string => {
	const month = String(date.getMonth() + 1).padStart(2, "0");
	const day = String(date.getDate()).padStart(2, "0");
	return `${date.getFullYear()}-${month}-${day}`;
};

const TODAY = localDay(2026, 9, 8);

const wrapperFor = (initialEntry: string) => {
	const Wrapper = ({ children }: { children: ReactNode }) => (
		<MemoryRouter initialEntries={[initialEntry]}>{children}</MemoryRouter>
	);
	return Wrapper;
};

const renderDateRange = (
	ownerType: "team" | "portfolio" = "team",
	defaultDateRange = 30,
	initialEntry = "/teams/1",
) =>
	renderHook(() => useDateRange(ownerType, defaultDateRange), {
		wrapper: wrapperFor(initialEntry),
	});

beforeEach(() => {
	searchParamWrites.length = 0;
	vi.useFakeTimers();
	vi.setSystemTime(TODAY);
});

afterEach(() => {
	vi.useRealTimers();
	vi.restoreAllMocks();
});

describe.skip("useDateRange — where the opening window comes from", () => {
	it("opens on the range the owner was configured with", () => {
		const { result } = renderDateRange("team", 30);

		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
		expect(asLocalIso(result.current.startDate)).toBe("2026-08-09");
	});

	it("opens a portfolio on its own longer range", () => {
		const { result } = renderDateRange("portfolio", 90);

		expect(asLocalIso(result.current.startDate)).toBe("2026-06-10");
	});

	it("lets a window already named in the address win over the configured range", () => {
		const { result } = renderDateRange(
			"team",
			30,
			"/teams/1?startDate=2026-01-01&endDate=2026-02-01",
		);

		expect(asLocalIso(result.current.startDate)).toBe("2026-01-01");
		expect(asLocalIso(result.current.endDate)).toBe("2026-02-01");
	});

	it("offers a team its four named windows and a portfolio its three", () => {
		const team = renderDateRange("team", 30);
		expect(team.result.current.presets).toHaveLength(4);

		const portfolio = renderDateRange("portfolio", 90);
		expect(portfolio.result.current.presets).toHaveLength(3);
	});
});

describe.skip("useDateRange — choosing a named window", () => {
	it("moves the window to the named number of days ending today", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.applyPreset(90));

		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
		expect(asLocalIso(result.current.startDate)).toBe("2026-06-10");
	});

	it("names both ends of the window in one write to the address", () => {
		// The regression net for the torn write. The two per-end setters each rebuilt the address
		// from the other end's previous value, so moving both ends by calling them in turn left one
		// end stale — and then fetched it. One write carrying both is what makes that unreachable.
		const { result } = renderDateRange("team", 30);

		act(() => result.current.applyPreset(90));

		expect(searchParamWrites).toHaveLength(1);
		expect(searchParamWrites[0].params.get("startDate")).toBe("2026-06-10");
		expect(searchParamWrites[0].params.get("endDate")).toBe("2026-09-08");
	});

	it("replaces the address rather than stacking a step onto the back button", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.applyPreset(7));

		expect(searchParamWrites[0].options?.replace).toBe(true);
	});

	it("brings a window that was walked into the past back to today", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));
		act(() => result.current.applyPreset(30));

		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
	});

	it("marks the named window the reader is looking at", () => {
		const { result } = renderDateRange("team", 30);

		expect(result.current.selectedPresetDays).toBe(30);
	});

	it("marks no named window once a date has been picked by hand", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.handleStartDateChange(localDay(2026, 8, 20)));

		expect(result.current.selectedPresetDays).toBeNull();
	});
});

describe.skip("useDateRange — picking one date by hand leaves the other alone", () => {
	it("keeps the end where it was when only the start is picked", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.handleStartDateChange(localDay(2026, 7, 1)));

		expect(asLocalIso(result.current.startDate)).toBe("2026-07-01");
		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
		expect(searchParamWrites[0].params.get("endDate")).toBe("2026-09-08");
	});

	it("keeps the start where it was when only the end is picked", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.handleEndDateChange(localDay(2026, 9, 1)));

		expect(asLocalIso(result.current.startDate)).toBe("2026-08-09");
		expect(searchParamWrites[0].params.get("startDate")).toBe("2026-08-09");
	});

	it("ignores a date the browser could not make sense of", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.handleStartDateChange(new Date("nonsense")));

		expect(asLocalIso(result.current.startDate)).toBe("2026-08-09");
		expect(searchParamWrites).toHaveLength(0);
	});
});

describe.skip("useDateRange — walking the window a period at a time", () => {
	it("moves a team's window back one week", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(asLocalIso(result.current.endDate)).toBe("2026-09-01");
		expect(asLocalIso(result.current.startDate)).toBe("2026-08-02");
	});

	it("moves a portfolio's window back four weeks", () => {
		const { result } = renderDateRange("portfolio", 90);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(asLocalIso(result.current.endDate)).toBe("2026-08-11");
	});

	it("settles a burst of clicks into a single window", () => {
		const { result } = renderDateRange("team", 30);

		act(() => {
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
		});
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(asLocalIso(result.current.endDate)).toBe("2026-08-11");
		expect(asLocalIso(result.current.startDate)).toBe("2026-07-12");
	});

	it("settles a burst of clicks into a single trip to the address, and so a single refetch", () => {
		const { result } = renderDateRange("team", 30);

		act(() => {
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
			result.current.stepWindow(-1);
		});
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(searchParamWrites).toHaveLength(1);
	});

	it("shows every click on the label before any of them is applied", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));

		expect(asLocalIso(result.current.pendingEndDate)).toBe("2026-09-01");
		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
		expect(result.current.isCommitPending).toBe(true);
	});

	it("stops saying a window is pending once it has been applied", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(result.current.isCommitPending).toBe(false);
		expect(asLocalIso(result.current.pendingEndDate)).toBe(
			asLocalIso(result.current.endDate),
		);
	});

	it("applies nothing while the reader is still clicking", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS - 1));

		expect(searchParamWrites).toHaveLength(0);
		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
	});

	it("keeps waiting when another click lands inside the quiet period", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS - 1));
		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS - 1));

		expect(searchParamWrites).toHaveLength(0);

		act(() => vi.advanceTimersByTime(1));

		expect(searchParamWrites).toHaveLength(1);
		expect(asLocalIso(result.current.endDate)).toBe("2026-08-25");
	});
});

describe.skip("useDateRange — the window never ends in the future", () => {
	it("offers no way forward from a window that already ends today", () => {
		const { result } = renderDateRange("team", 30);

		expect(result.current.canStepForward).toBe(false);
	});

	it("offers a way forward once the window has been walked into the past", () => {
		const { result } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(result.current.canStepForward).toBe(true);
	});

	it("lands exactly on today, at full length, when a step forward would overshoot", () => {
		const { result } = renderDateRange(
			"team",
			30,
			"/teams/1?startDate=2026-08-06&endDate=2026-09-05",
		);

		act(() => result.current.stepWindow(1));
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS));

		expect(asLocalIso(result.current.endDate)).toBe("2026-09-08");
		expect(asLocalIso(result.current.startDate)).toBe("2026-08-09");
	});
});

describe.skip("useDateRange — leaving the page mid-click", () => {
	it("applies nothing and complains about nothing when the reader navigates away first", () => {
		const consoleError = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		const { result, unmount } = renderDateRange("team", 30);

		act(() => result.current.stepWindow(-1));
		unmount();
		act(() => vi.advanceTimersByTime(QUIET_PERIOD_MS * 2));

		expect(searchParamWrites).toHaveLength(0);
		expect(consoleError).not.toHaveBeenCalled();
	});
});

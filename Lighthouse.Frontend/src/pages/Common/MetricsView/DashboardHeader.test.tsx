import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// Mock DateRangeSelector used inside the popover
vi.mock(
	"../../../components/Common/DateRangeSelector/DateRangeSelector",
	() => ({
		default: (props: {
			onStartDateChange: (d: Date) => void;
			onEndDateChange: (d: Date) => void;
		}) => (
			<div data-testid="date-range-selector">
				<button
					type="button"
					data-testid="change-start-date"
					onClick={() => props.onStartDateChange(new Date("2020-01-01"))}
				>
					Change Start
				</button>
				<button
					type="button"
					data-testid="change-end-date"
					onClick={() => props.onEndDateChange(new Date("2020-01-02"))}
				>
					Change End
				</button>
			</div>
		),
	}),
);

// Helper to control matchMedia (used by MUI's useMediaQuery)
function setMatchMedia(matches: boolean) {
	Object.defineProperty(window, "matchMedia", {
		writable: true,
		value: (query: string) => ({
			matches,
			media: query,
			onchange: null,
			addListener: () => {},
			removeListener: () => {},
			addEventListener: () => {},
			removeEventListener: () => {},
			dispatchEvent: () => false,
		}),
	});
}

describe("DashboardHeader", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	afterEach(() => {
		// restore default matchMedia
		setMatchMedia(false);
	});

	it("shows label and formatted date range and opens popover which calls handlers", async () => {
		setMatchMedia(false); // wide screen

		const onStart = vi.fn();
		const onEnd = vi.fn();

		const { default: DashboardHeader } = await import("./DashboardHeader");

		const start = new Date(2025, 6, 15); // 15 Jul 2025
		const end = new Date(2025, 7, 14); // 14 Aug 2025

		render(
			<DashboardHeader
				startDate={start}
				endDate={end}
				onStartDateChange={onStart}
				onEndDateChange={onEnd}
				dashboardId="Team_1337"
			/>,
		);

		// Label should be visible
		expect(screen.getByText("Metrics shown for:")).toBeInTheDocument();

		// Formatted date range should be visible (substring match)
		expect(
			screen.getByText(/15 Jul 2025\s*→\s*14 Aug 2025/),
		).toBeInTheDocument();

		// Open the popover
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));

		// Popover should render the mocked DateRangeSelector
		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		// Clicking the mocked buttons should call handlers
		fireEvent.click(screen.getByTestId("change-start-date"));
		expect(onStart).toHaveBeenCalled();

		fireEvent.click(screen.getByTestId("change-end-date"));
		expect(onEnd).toHaveBeenCalled();
	});

	it("hides the label on narrow screens but keeps the toggle", async () => {
		setMatchMedia(true); // narrow screen

		const onStart = vi.fn();
		const onEnd = vi.fn();

		const { default: DashboardHeader } = await import("./DashboardHeader");

		const start = new Date(2025, 6, 15);
		const end = new Date(2025, 7, 14);

		render(
			<DashboardHeader
				startDate={start}
				endDate={end}
				onStartDateChange={onStart}
				onEndDateChange={onEnd}
				dashboardId="Team_1337"
			/>,
		);

		// Label should not be present on narrow screens
		expect(screen.queryByText("Metrics shown for:")).toBeNull();

		// The toggle must still be present and operable
		expect(
			screen.getByTestId("dashboard-date-range-toggle"),
		).toBeInTheDocument();
	});

	it("toggles edit mode, persists to localStorage and dispatches event", async () => {
		setMatchMedia(false);

		const onStart = vi.fn();
		const onEnd = vi.fn();

		const { default: DashboardHeader } = await import("./DashboardHeader");

		const start = new Date(2025, 6, 15);
		const end = new Date(2025, 7, 14);
		const dashboardId = "test-dashboard-1";

		// ensure clean state
		localStorage.removeItem(`lighthouse:dashboard:${dashboardId}:edit`);

		const dispatchSpy = vi.spyOn(window, "dispatchEvent");

		render(
			<DashboardHeader
				startDate={start}
				endDate={end}
				onStartDateChange={onStart}
				onEndDateChange={onEnd}
				dashboardId={dashboardId}
			/>,
		);

		const toggle = screen.getByTestId("dashboard-edit-toggle");
		expect(toggle).toBeInTheDocument();
		// initial state not pressed
		expect(toggle).toHaveAttribute("aria-pressed", "false");

		// enable edit mode
		fireEvent.click(toggle);

		// persisted
		expect(
			localStorage.getItem(`lighthouse:dashboard:${dashboardId}:edit`),
		).toBe("1");

		// toggle reflect state
		expect(toggle).toHaveAttribute("aria-pressed", "true");

		// dispatch event called with expected payload
		expect(dispatchSpy).toHaveBeenCalled();
		const lastCall = dispatchSpy.mock.calls[dispatchSpy.mock.calls.length - 1];
		const evt = lastCall ? lastCall[0] : undefined;
		expect(evt).toBeInstanceOf(CustomEvent);
		expect((evt as CustomEvent).detail).toMatchObject({
			dashboardId: dashboardId,
			isEditing: true,
		});

		// disable edit mode
		fireEvent.click(toggle);
		expect(
			localStorage.getItem(`lighthouse:dashboard:${dashboardId}:edit`),
		).toBe("0");

		const lastCall2 = dispatchSpy.mock.calls[dispatchSpy.mock.calls.length - 1];
		const evt2 = lastCall2 ? lastCall2[0] : undefined;
		expect((evt2 as CustomEvent).detail).toMatchObject({
			dashboardId: dashboardId,
			isEditing: false,
		});

		// cleanup
		dispatchSpy.mockRestore();
		localStorage.removeItem(`lighthouse:dashboard:${dashboardId}:edit`);
	});
});

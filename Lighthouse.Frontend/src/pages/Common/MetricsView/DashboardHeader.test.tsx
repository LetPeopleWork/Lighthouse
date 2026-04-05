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
		setMatchMedia(false);
	});

	it("shows label and formatted date range and opens popover which calls handlers", async () => {
		setMatchMedia(false);

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
			/>,
		);

		expect(screen.getByText("Metrics shown for:")).toBeInTheDocument();

		expect(
			screen.getByText(/15 Jul 2025\s*→\s*14 Aug 2025/),
		).toBeInTheDocument();

		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));

		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		fireEvent.click(screen.getByTestId("change-start-date"));
		expect(onStart).toHaveBeenCalled();

		fireEvent.click(screen.getByTestId("change-end-date"));
		expect(onEnd).toHaveBeenCalled();
	});

	it("hides the label on narrow screens but keeps the toggle", async () => {
		setMatchMedia(true);

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
			/>,
		);

		expect(screen.queryByText("Metrics shown for:")).toBeNull();

		expect(
			screen.getByTestId("dashboard-date-range-toggle"),
		).toBeInTheDocument();
	});

	it("does not expose edit toggle or reset layout controls", async () => {
		setMatchMedia(false);

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
			/>,
		);

		expect(
			screen.queryByTestId("dashboard-edit-toggle"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-reset-layout"),
		).not.toBeInTheDocument();
	});
});

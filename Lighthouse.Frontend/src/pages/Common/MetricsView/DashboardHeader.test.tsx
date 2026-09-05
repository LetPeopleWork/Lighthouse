import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { DashboardHeaderProps } from "./DashboardHeader";

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

vi.mock("./CategorySelector", () => ({
	default: ({
		selectedCategory,
		onSelectCategory,
	}: {
		selectedCategory: string;
		onSelectCategory: (key: string) => void;
	}) => (
		<div data-testid="category-selector">
			<span data-testid="selected-category">{selectedCategory}</span>
			<button
				type="button"
				data-testid="select-category"
				onClick={() => onSelectCategory("portfolio")}
			>
				Switch
			</button>
		</div>
	),
}));

// MUI reads matchMedia through useMediaQuery, and jsdom does not provide it.
function setMatchMedia(matches: boolean) {
	Object.defineProperty(globalThis, "matchMedia", {
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

const renderHeader = async (overrides: Partial<DashboardHeaderProps> = {}) => {
	const { default: DashboardHeader } = await import("./DashboardHeader");

	render(
		<DashboardHeader
			startDate={new Date(2025, 6, 15)}
			endDate={new Date(2025, 7, 14)}
			onStartDateChange={vi.fn()}
			onEndDateChange={vi.fn()}
			selectedCategory="flow-overview"
			onSelectCategory={vi.fn()}
			showTips={true}
			onToggleTips={vi.fn()}
			{...overrides}
		/>,
	);
};

describe("DashboardHeader", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	afterEach(() => {
		setMatchMedia(false);
	});

	// Drives a popover through several user interactions, which costs a few seconds on its own and
	// more when the suite is busy. The default five seconds left no headroom, so this test went red
	// whenever tests were added elsewhere in the suite.
	it("shows label and formatted date range and opens popover which calls handlers", {
		timeout: 20000,
	}, async () => {
		setMatchMedia(false);

		const onStartDateChange = vi.fn();
		const onEndDateChange = vi.fn();

		await renderHeader({ onStartDateChange, onEndDateChange });

		expect(screen.getByText("Metrics shown for:")).toBeInTheDocument();

		expect(
			screen.getByText(/15 Jul 2025\s*→\s*14 Aug 2025/),
		).toBeInTheDocument();

		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));

		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		fireEvent.click(screen.getByTestId("change-start-date"));
		expect(onStartDateChange).toHaveBeenCalled();

		fireEvent.click(screen.getByTestId("change-end-date"));
		expect(onEndDateChange).toHaveBeenCalled();
	});

	it("hides the label on narrow screens but keeps the toggle", async () => {
		setMatchMedia(true);

		await renderHeader();

		expect(screen.queryByText("Metrics shown for:")).toBeNull();

		expect(
			screen.getByTestId("dashboard-date-range-toggle"),
		).toBeInTheDocument();
	});

	it("does not expose edit toggle or reset layout controls", async () => {
		setMatchMedia(false);

		await renderHeader();

		expect(
			screen.queryByTestId("dashboard-edit-toggle"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-reset-layout"),
		).not.toBeInTheDocument();
	});

	it("renders category selector with the selected category", async () => {
		setMatchMedia(false);

		await renderHeader({ selectedCategory: "predictability" });

		expect(screen.getByTestId("category-selector")).toBeInTheDocument();
		expect(screen.getByTestId("selected-category")).toHaveTextContent(
			"predictability",
		);
	});

	it("renders a placeholder for an unusable date and leaves the other one formatted", async () => {
		setMatchMedia(false);

		await renderHeader({ startDate: new Date(Number.NaN) });

		expect(screen.getByText(/—\s*→\s*14 Aug 2025/)).toBeInTheDocument();
	});

	it("keeps the header mounted when both dates are unusable", async () => {
		setMatchMedia(false);

		await renderHeader({
			startDate: new Date(Number.NaN),
			endDate: new Date(Number.NaN),
		});

		expect(screen.getByText(/—\s*→\s*—/)).toBeInTheDocument();
		expect(
			screen.getByTestId("dashboard-date-range-toggle"),
		).toBeInTheDocument();
	});

	it("renders tips toggle and calls onToggleTips when clicked", async () => {
		setMatchMedia(false);

		const onToggleTips = vi.fn();

		await renderHeader({ onToggleTips });

		const toggle = screen.getByTestId("metrics-tips-toggle");
		expect(toggle).toBeInTheDocument();
		fireEvent.click(toggle);
		expect(onToggleTips).toHaveBeenCalled();
	});
});

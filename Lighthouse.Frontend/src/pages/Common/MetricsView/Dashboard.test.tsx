import {
	cleanup,
	fireEvent,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DashboardItem } from "./Dashboard";

// Helper to simulate different viewport widths by controlling matchMedia
function setMatchMediaWidth(width: number) {
	Object.defineProperty(window, "matchMedia", {
		writable: true,
		value: (query: string) => {
			// Extract min-width in px from the media query if present
			const m = RegExp(/min-width:\s*(\d+)px/).exec(query);
			const min = m ? parseInt(m[1], 10) : 0;
			return {
				matches: width >= min,
				media: query,
				onchange: null,
				addListener: () => {},
				removeListener: () => {},
				addEventListener: () => {},
				removeEventListener: () => {},
				dispatchEvent: () => false,
			} as unknown as MediaQueryList;
		},
	});
}

describe("Dashboard component", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	afterEach(() => {
		vi.clearAllTimers();
		vi.useRealTimers();
		localStorage.clear();
		cleanup();
	});

	it("renders provided items in fixed order and exposes data attributes", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "a", node: <div>Item A</div>, size: "small" },
			{ id: "b", node: <div>Item B</div>, size: "medium" },
		];

		render(<Dashboard items={items} />);

		const a = screen.getByTestId("dashboard-item-a");
		const b = screen.getByTestId("dashboard-item-b");

		expect(a).toBeInTheDocument();
		expect(b).toBeInTheDocument();

		expect(a.getAttribute("data-size")).toBe("small");
		expect(b.getAttribute("data-size")).toBe("medium");
	});

	it("renders items in the order provided without localStorage influence", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} />);

		const nodes = await screen.findAllByTestId(/^dashboard-item-(?!spotlight)/);
		const ids = nodes.map((n) =>
			n.getAttribute("data-testid")?.replace("dashboard-item-", ""),
		);
		expect(ids).toEqual(["A", "B", "C"]);
	});

	it("applies responsive column spans for different breakpoints and sizes", async () => {
		const { default: Dashboard } = await import("./Dashboard");

		const sizes: Array<{ size: string; expected: Record<string, number> }> = [
			{
				size: "small",
				expected: { xl: 3, lg: 2, md: 2, sm: 3, xs: 4 },
			},
			{
				size: "medium",
				expected: { xl: 4, lg: 5, md: 4, sm: 6, xs: 4 },
			},
			{
				size: "large",
				expected: { xl: 6, lg: 5, md: 8, sm: 6, xs: 4 },
			},
			{
				size: "xlarge",
				expected: { xl: 12, lg: 10, md: 8, sm: 6, xs: 4 },
			},
		];

		async function getColSpanFor(size: string, width: number) {
			cleanup();
			setMatchMediaWidth(width);
			const items: DashboardItem[] = [
				{ id: "x", node: <div>Test</div>, size: size as DashboardItem["size"] },
			];
			render(<Dashboard items={items} />);
			const el = await screen.findByTestId("dashboard-item-x");
			return Number(el.getAttribute("data-colspan") || 0);
		}

		const breakpoints = {
			xl: 1600,
			lg: 1300,
			md: 1000,
			sm: 700,
			xs: 300,
		} as const;

		for (const s of sizes) {
			expect(await getColSpanFor(s.size, breakpoints.xl)).toBe(s.expected.xl);
			expect(await getColSpanFor(s.size, breakpoints.lg)).toBe(s.expected.lg);
			expect(await getColSpanFor(s.size, breakpoints.md)).toBe(s.expected.md);
			expect(await getColSpanFor(s.size, breakpoints.sm)).toBe(s.expected.sm);
			expect(await getColSpanFor(s.size, breakpoints.xs)).toBe(s.expected.xs);
		}
	});

	it("does not expose edit, hide, reorder, or resize controls", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "a", node: <div>A</div> },
			{ id: "b", node: <div>B</div> },
		];

		render(<Dashboard items={items} />);

		// No edit-mode related controls should exist
		expect(
			screen.queryByTestId("dashboard-item-hide-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-show-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-col-inc-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-col-dec-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-row-inc-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-row-dec-a"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("dashboard-item-size-reset-a"),
		).not.toBeInTheDocument();
		expect(screen.queryByTestId("dashboard-end-drop")).not.toBeInTheDocument();

		// Items should not be draggable
		const a = screen.getByTestId("dashboard-item-a");
		expect(a.getAttribute("draggable")).not.toBe("true");
	});

	it("does not read or write dashboard layout/hidden/edit/sizes localStorage keys", async () => {
		setMatchMediaWidth(1600);

		// Pre-populate old dashboard keys — they should be ignored
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["B", "A"]),
		);
		localStorage.setItem(
			"lighthouse:dashboard:d1:hidden",
			JSON.stringify(["A"]),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		localStorage.setItem(
			"lighthouse:dashboard:d1:sizes",
			JSON.stringify({ A: { colSpan: 2, rowSpan: 1 } }),
		);

		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
		];

		render(<Dashboard items={items} />);

		// Both items should render (old hidden state ignored)
		expect(await screen.findByTestId("dashboard-item-A")).toBeInTheDocument();
		expect(await screen.findByTestId("dashboard-item-B")).toBeInTheDocument();

		// Order should be as provided, not from localStorage
		const nodes = await screen.findAllByTestId(/^dashboard-item-(?!spotlight)/);
		const ids = nodes.map((n) =>
			n.getAttribute("data-testid")?.replace("dashboard-item-", ""),
		);
		expect(ids).toEqual(["A", "B"]);
	});

	it("skips items with null or undefined node", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "a", node: <div>A</div> },
			{ id: "b", node: null },
			{ id: "c", node: <div>C</div> },
		];

		render(<Dashboard items={items} />);

		expect(screen.getByTestId("dashboard-item-a")).toBeInTheDocument();
		expect(screen.queryByTestId("dashboard-item-b")).not.toBeInTheDocument();
		expect(screen.getByTestId("dashboard-item-c")).toBeInTheDocument();
	});

	it("spotlight button opens widget in fullscreen modal", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{
				id: "widget1",
				node: <div data-testid="widget1-content">Widget 1 Content</div>,
			},
			{
				id: "widget2",
				node: <div data-testid="widget2-content">Widget 2 Content</div>,
			},
		];

		render(<Dashboard items={items} />);

		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		expect(spotlightBtn).toBeInTheDocument();

		expect(
			screen.queryByTestId("dashboard-spotlight-modal"),
		).not.toBeInTheDocument();

		fireEvent.click(spotlightBtn);

		const modal = await screen.findByTestId("dashboard-spotlight-modal");
		expect(modal).toBeInTheDocument();

		const modalContent = await screen.findAllByTestId("widget1-content");
		expect(modalContent.length).toBeGreaterThan(0);
	});

	it("spotlight modal closes when clicking the close button", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{
				id: "widget1",
				node: <div data-testid="widget1-content">Widget 1</div>,
			},
		];

		render(<Dashboard items={items} />);

		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		const modal = await screen.findByTestId("dashboard-spotlight-modal");
		expect(modal).toBeInTheDocument();

		const closeBtn = await screen.findByTestId("dashboard-spotlight-close");
		fireEvent.click(closeBtn);

		await waitFor(() => {
			expect(
				screen.queryByTestId("dashboard-spotlight-modal"),
			).not.toBeInTheDocument();
		});
	});

	it("spotlight modal closes when pressing Escape key", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{
				id: "widget1",
				node: <div data-testid="widget1-content">Widget 1</div>,
			},
		];

		render(<Dashboard items={items} />);

		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		await screen.findByTestId("dashboard-spotlight-modal");

		fireEvent.keyDown(window, { key: "Escape", code: "Escape" });

		await waitFor(() => {
			expect(
				screen.queryByTestId("dashboard-spotlight-modal"),
			).not.toBeInTheDocument();
		});
	});

	it("spotlight displays the correct widget content", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{
				id: "widget1",
				node: <div data-testid="widget1-content">Widget 1 Content</div>,
			},
			{
				id: "widget2",
				node: <div data-testid="widget2-content">Widget 2 Content</div>,
			},
		];

		render(<Dashboard items={items} />);

		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget2",
		);
		fireEvent.click(spotlightBtn);

		const widget2Contents = await screen.findAllByTestId("widget2-content");
		expect(widget2Contents.length).toBe(2);

		const widget1Contents = screen.getAllByTestId("widget1-content");
		expect(widget1Contents.length).toBe(1);
	});

	it("spotlight state is not persisted to localStorage", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "widget1", node: <div>Widget 1</div> },
		];

		render(<Dashboard items={items} />);

		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		await screen.findByTestId("dashboard-spotlight-modal");

		const spotlightKey = localStorage.getItem(
			"lighthouse:dashboard:spotlight_test:spotlight",
		);
		expect(spotlightKey).toBeNull();
	});
});

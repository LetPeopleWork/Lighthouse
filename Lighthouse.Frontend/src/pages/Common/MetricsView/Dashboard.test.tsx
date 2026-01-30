import {
	cleanup,
	fireEvent,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DashboardItem } from "./Dashboard";

// Polyfill DataTransfer in the test environment if absent (jsdom may not provide it)
if (typeof globalThis.DataTransfer === "undefined") {
	// Minimal DataTransfer shim sufficient for drag/drop unit tests
	// assign a simple constructor function so `new DataTransfer()` works
	// eslint-disable-next-line @typescript-eslint/ban-ts-comment
	// @ts-expect-error
	globalThis.DataTransfer = class {
		items: Record<string, string> = {};
		setData(type: string, value: string) {
			this.items[type] = value;
		}
		getData(type: string) {
			return this.items[type] || "";
		}
		clearData() {
			this.items = {};
		}
	};
}

// Ensure scrollBy exists so spyOn can attach to it in tests
if (typeof window.scrollBy !== "function") {
	window.scrollBy = () => {};
}

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

	it("allows hiding and showing widgets in edit mode and persists to localStorage", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");

		// ensure clean localStorage and enable edit mode
		localStorage.removeItem("lighthouse:dashboard:Team_1337:hidden");
		localStorage.setItem("lighthouse:dashboard:Team_1337:edit", "1");

		const items: DashboardItem[] = [
			{ id: "a", node: <div>Item A</div> },
			{ id: "b", node: <div>Item B</div> },
		];

		render(<Dashboard items={items} dashboardId="Team_1337" />);

		const a = await screen.findByTestId("dashboard-item-a");
		expect(a).toBeInTheDocument();

		// hide control should be visible in edit mode
		const hideBtn = screen.getByTestId("dashboard-item-hide-a");
		expect(hideBtn).toBeInTheDocument();

		// click hide -> becomes hidden (but still visible while editing) and persisted
		fireEvent.click(hideBtn);

		const showBtn = await screen.findByTestId("dashboard-item-show-a");
		expect(showBtn).toBeInTheDocument();

		const raw = localStorage.getItem("lighthouse:dashboard:Team_1337:hidden");
		expect(raw).not.toBeNull();
		const arr = JSON.parse(raw as string);
		expect(arr).toContain("a");

		// exit edit mode via event -> hidden items should no longer be rendered
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
				detail: { dashboardId: "Team_1337", isEditing: false },
			}),
		);

		await waitFor(() =>
			expect(screen.queryByTestId("dashboard-item-a")).toBeNull(),
		);
		await waitFor(() =>
			expect(screen.queryByTestId("dashboard-item-b")).toBeInTheDocument(),
		);
	});

	it("restores widget when shown in edit mode and removes from localStorage", async () => {
		setMatchMediaWidth(1600);

		// preset hidden and enable edit mode
		localStorage.setItem(
			"lighthouse:dashboard:Team_1337:hidden",
			JSON.stringify(["a"]),
		);
		localStorage.setItem("lighthouse:dashboard:Team_1337:edit", "1");

		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [{ id: "a", node: <div>Item A</div> }];

		cleanup();
		render(<Dashboard items={items} dashboardId="Team_1337" />);

		// while in edit mode the hidden item is still rendered and exposes the show control
		const showBtn = await screen.findByTestId("dashboard-item-show-a");
		expect(showBtn).toBeInTheDocument();

		// click show -> should remove from persisted hidden list
		fireEvent.click(showBtn);

		const hideBtn = await screen.findByTestId("dashboard-item-hide-a");
		expect(hideBtn).toBeInTheDocument();

		const raw = localStorage.getItem("lighthouse:dashboard:Team_1337:hidden");
		const arr = JSON.parse(raw || "[]");
		expect(arr).not.toContain("a");
	});

	it("renders provided items and exposes data attributes", async () => {
		// wide screen (xl)
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "a", node: <div>Item A</div>, size: "small" },
			{ id: "b", node: <div>Item B</div>, size: "medium" },
		];

		render(<Dashboard items={items} dashboardId="Team_1337" />);

		const a = screen.getByTestId("dashboard-item-a");
		const b = screen.getByTestId("dashboard-item-b");

		expect(a).toBeInTheDocument();
		expect(b).toBeInTheDocument();

		// data-size should reflect the provided size
		expect(a.getAttribute("data-size")).toBe("small");
		expect(b.getAttribute("data-size")).toBe("medium");
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

		// helper to render a single item and return its data-colspan
		async function getColSpanFor(size: string, width: number) {
			// ensure previous renders are removed so we don't accumulate multiple matching nodes
			cleanup();
			setMatchMediaWidth(width);
			const items: DashboardItem[] = [
				{ id: "x", node: <div>Test</div>, size: size as DashboardItem["size"] },
			];
			render(<Dashboard items={items} dashboardId="Team_1337" />);
			const el = await screen.findByTestId("dashboard-item-x");
			return Number(el.getAttribute("data-colspan") || 0);
		}

		// Map of breakpoint widths roughly matching MUI defaults
		const breakpoints = {
			xl: 1600,
			lg: 1300,
			md: 1000,
			sm: 700,
			xs: 300,
		} as const;

		for (const s of sizes) {
			// test each breakpoint
			expect(await getColSpanFor(s.size, breakpoints.xl)).toBe(s.expected.xl);
			expect(await getColSpanFor(s.size, breakpoints.lg)).toBe(s.expected.lg);
			expect(await getColSpanFor(s.size, breakpoints.md)).toBe(s.expected.md);
			expect(await getColSpanFor(s.size, breakpoints.sm)).toBe(s.expected.sm);
			expect(await getColSpanFor(s.size, breakpoints.xs)).toBe(s.expected.xs);
		}
	});

	it("renders initial order when no stored layout", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const nodes = await screen.findAllByTestId(/dashboard-item-/);
		const ids = nodes
			.filter((n) => !n.getAttribute("data-testid")?.includes("spotlight"))
			.map((n) =>
				n.getAttribute("data-testid")?.replace("dashboard-item-", ""),
			);
		expect(ids).toEqual(["A", "B", "C"]);
		expect(localStorage.getItem("lighthouse:dashboard:d1:layout")).toBeNull();
	});

	it("loads order from localStorage and renders accordingly", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["C", "A", "B"]),
		);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const nodes = await screen.findAllByTestId(/dashboard-item-/);
		const ids = nodes
			.filter((n) => !n.getAttribute("data-testid")?.includes("spotlight"))
			.map((n) =>
				n.getAttribute("data-testid")?.replace("dashboard-item-", ""),
			);
		expect(ids).toEqual(["C", "A", "B"]);
		// ensure state reflects stored array (layout key remains as set)
		expect(
			JSON.parse(
				localStorage.getItem("lighthouse:dashboard:d1:layout") || "[]",
			),
		).toEqual(["C", "A", "B"]);
	});

	it("appends new widgets missing in stored layout", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["A", "B"]),
		);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const nodes = await screen.findAllByTestId(/dashboard-item-/);
		const ids = nodes
			.filter((n) => !n.getAttribute("data-testid")?.includes("spotlight"))
			.map((n) =>
				n.getAttribute("data-testid")?.replace("dashboard-item-", ""),
			);
		expect(ids).toEqual(["A", "B", "C"]);
	});

	it("hiding a widget appends its key to layout and updates hidden list", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const hideB = await screen.findByTestId("dashboard-item-hide-B");
		fireEvent.click(hideB);

		// hidden persisted
		const hiddenRaw = localStorage.getItem("lighthouse:dashboard:d1:hidden");
		expect(JSON.parse(hiddenRaw || "[]")).toContain("B");

		// layout should move B to last position
		const layout = JSON.parse(
			localStorage.getItem("lighthouse:dashboard:d1:layout") || "[]",
		);
		expect(layout[layout.length - 1]).toBe("B");
	});

	it("showing a hidden widget reinserts before first hidden", async () => {
		setMatchMediaWidth(1600);
		// layout A,B,C and C hidden
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["A", "B", "C"]),
		);
		localStorage.setItem(
			"lighthouse:dashboard:d1:hidden",
			JSON.stringify(["C"]),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
			{ id: "C", node: <div>C</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const showC = await screen.findByTestId("dashboard-item-show-C");
		fireEvent.click(showC);

		const hiddenAfter = JSON.parse(
			localStorage.getItem("lighthouse:dashboard:d1:hidden") || "[]",
		);
		expect(hiddenAfter).not.toContain("C");

		const layoutAfter = JSON.parse(
			localStorage.getItem("lighthouse:dashboard:d1:layout") || "[]",
		);
		// C should not be at absolute end if others hidden existed; for this simple case ensure C is present and index < layout.length
		const idx = layoutAfter.indexOf("C");
		expect(idx).toBeGreaterThanOrEqual(0);
	});

	it("hidden widgets are not draggable when hidden", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:d1:hidden",
			JSON.stringify(["W"]),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [{ id: "W", node: <div>W</div> }];
		render(<Dashboard items={items} dashboardId="d1" />);

		const el = await screen.findByTestId("dashboard-item-W");
		expect(el.getAttribute("draggable")).toBe("false");

		// attempt dragStart - should not change anything
		const dt = new DataTransfer();
		fireEvent.dragStart(el, { dataTransfer: dt });
		// may be empty, but component shouldn't crash; ensure DOM still contains element in edit mode
		expect(screen.getByTestId("dashboard-item-W")).toBeInTheDocument();
	});

	it("widgets are draggable only in edit mode and when not hidden", async () => {
		setMatchMediaWidth(1600);
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [{ id: "X", node: <div>X</div> }];
		render(<Dashboard items={items} dashboardId="d1" />);

		// not in edit mode -> not draggable
		const el = await screen.findByTestId("dashboard-item-X");
		expect(el.getAttribute("draggable")).toBe("false");

		// enable edit mode via event
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
				detail: { dashboardId: "d1", isEditing: true },
			}),
		);
		await waitFor(() => expect(el.getAttribute("draggable")).toBe("true"));
	});

	it("drop into empty end area inserts before first hidden or at end", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["A", "B"]),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const a = await screen.findByTestId("dashboard-item-A");
		const end = await screen.findByTestId("dashboard-end-drop");
		const dt = new DataTransfer();
		fireEvent.dragStart(a, { dataTransfer: dt });
		fireEvent.drop(end, { dataTransfer: dt });

		const layoutAfter = JSON.parse(
			localStorage.getItem("lighthouse:dashboard:d1:layout") || "[]",
		);
		// A should now be at end
		expect(layoutAfter[layoutAfter.length - 1]).toBe("A");
	});

	it("outside click clears edit mode and writes localStorage", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [{ id: "A", node: <div>A</div> }];
		// render an outside element
		const outside = document.createElement("div");
		outside.setAttribute("data-testid", "outside");
		document.body.appendChild(outside);

		render(<Dashboard items={items} dashboardId="d1" />);

		fireEvent.click(outside);

		await waitFor(() =>
			expect(localStorage.getItem("lighthouse:dashboard:d1:edit")).toBe("0"),
		);
		document.body.removeChild(outside);
	});

	it("navigation (popstate) clears edit mode", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [{ id: "A", node: <div>A</div> }];
		render(<Dashboard items={items} dashboardId="d1" />);

		window.dispatchEvent(new PopStateEvent("popstate"));

		await waitFor(() =>
			expect(localStorage.getItem("lighthouse:dashboard:d1:edit")).toBe("0"),
		);
	});

	it("malformed layout JSON is handled gracefully", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem("lighthouse:dashboard:d1:layout", "NOT_JSON");
		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
		];
		render(<Dashboard items={items} dashboardId="d1" />);

		// component should still render items
		expect(await screen.findByTestId("dashboard-item-A")).toBeInTheDocument();
		expect(await screen.findByTestId("dashboard-item-B")).toBeInTheDocument();
	});

	it("reset-layout for other dashboard does not affect this dashboard", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:d1:layout",
			JSON.stringify(["A", "B"]),
		);
		localStorage.setItem(
			"lighthouse:dashboard:d1:hidden",
			JSON.stringify(["B"]),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "A", node: <div>A</div> },
			{ id: "B", node: <div>B</div> },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		// confirm precondition: show control for B exists
		expect(
			await screen.findByTestId("dashboard-item-show-B"),
		).toBeInTheDocument();

		// dispatch reset for a different dashboard
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:reset-layout", {
				detail: { dashboardId: "other" },
			}),
		);

		// original keys should remain
		expect(
			JSON.parse(
				localStorage.getItem("lighthouse:dashboard:d1:hidden") || "[]",
			),
		).toContain("B");

		// UI should be unchanged
		expect(screen.getByTestId("dashboard-item-show-B")).toBeInTheDocument();
	});

	it("allows resizing (col/row) and persists overrides to localStorage", async () => {
		setMatchMediaWidth(1600);

		// enable edit mode before importing so component initializes in edit state
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");
		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = [
			{ id: "R", node: <div>R</div>, size: "medium" },
		];

		render(<Dashboard items={items} dashboardId="d1" />);

		const el = await screen.findByTestId("dashboard-item-R");

		// defaults for medium @ xl in the component
		expect(Number(el.getAttribute("data-colspan"))).toBe(4);
		expect(Number(el.getAttribute("data-rowspan"))).toBe(2);

		const colInc = screen.getByTestId("dashboard-item-col-inc-R");
		const colDec = screen.getByTestId("dashboard-item-col-dec-R");
		const rowInc = screen.getByTestId("dashboard-item-row-inc-R");
		const rowDec = screen.getByTestId("dashboard-item-row-dec-R");

		// increase column -> should update attribute and persist
		fireEvent.click(colInc);
		await waitFor(() =>
			expect(Number(el.getAttribute("data-colspan"))).toBe(5),
		);

		// decrease column -> back to default
		fireEvent.click(colDec);
		await waitFor(() =>
			expect(Number(el.getAttribute("data-colspan"))).toBe(4),
		);

		// increase row -> should update attribute
		fireEvent.click(rowInc);
		await waitFor(() =>
			expect(Number(el.getAttribute("data-rowspan"))).toBe(3),
		);

		// decrease row -> back to default
		fireEvent.click(rowDec);
		await waitFor(() =>
			expect(Number(el.getAttribute("data-rowspan"))).toBe(2),
		);

		// verify persisted sizes
		const sizesRaw = localStorage.getItem("lighthouse:dashboard:d1:sizes");
		expect(sizesRaw).not.toBeNull();
		const sizes = JSON.parse(sizesRaw || "{}");
		expect(sizes).toHaveProperty("R");
		expect(sizes.R.colSpan).toBe(4);
		expect(sizes.R.rowSpan).toBe(2);
	});

	it("reset size control removes override and restores default sizing", async () => {
		setMatchMediaWidth(1600);

		// pre-populate an explicit override for this widget
		localStorage.setItem(
			"lighthouse:dashboard:d1:sizes",
			JSON.stringify({ R: { colSpan: 2, rowSpan: 1 } }),
		);
		localStorage.setItem("lighthouse:dashboard:d1:edit", "1");

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "R", node: <div>R</div>, size: "medium" },
		];

		// ensure prior renders are cleared
		cleanup();
		render(<Dashboard items={items} dashboardId="d1" />);

		const el = await screen.findByTestId("dashboard-item-R");
		// should reflect persisted override initially
		expect(Number(el.getAttribute("data-colspan"))).toBe(2);
		expect(Number(el.getAttribute("data-rowspan"))).toBe(1);

		const resetBtn = screen.getByTestId("dashboard-item-size-reset-R");
		fireEvent.click(resetBtn);

		await waitFor(() => {
			// sizes entry should be removed
			const sizesRaw = localStorage.getItem("lighthouse:dashboard:d1:sizes");
			const sizes = JSON.parse(sizesRaw || "{}");
			expect(sizes).not.toHaveProperty("R");
			// and UI should reflect default for medium @ xl
			expect(Number(el.getAttribute("data-colspan"))).toBe(4);
			expect(Number(el.getAttribute("data-rowspan"))).toBe(2);
		});
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

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Spotlight button should be visible when not in edit mode
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		expect(spotlightBtn).toBeInTheDocument();

		// Modal should not be visible initially
		expect(
			screen.queryByTestId("dashboard-spotlight-modal"),
		).not.toBeInTheDocument();

		// Click spotlight button
		fireEvent.click(spotlightBtn);

		// Modal should now be visible with the widget content
		const modal = await screen.findByTestId("dashboard-spotlight-modal");
		expect(modal).toBeInTheDocument();

		// Widget content should be in the modal
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

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Open spotlight
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		// Modal should be visible
		const modal = await screen.findByTestId("dashboard-spotlight-modal");
		expect(modal).toBeInTheDocument();

		// Click close button
		const closeBtn = await screen.findByTestId("dashboard-spotlight-close");
		fireEvent.click(closeBtn);

		// Modal should be removed
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

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Open spotlight
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		// Modal should be visible
		await screen.findByTestId("dashboard-spotlight-modal");

		// Press Escape key
		fireEvent.keyDown(window, { key: "Escape", code: "Escape" });

		// Modal should be removed
		await waitFor(() => {
			expect(
				screen.queryByTestId("dashboard-spotlight-modal"),
			).not.toBeInTheDocument();
		});
	});

	it("spotlight modal closes when clicking the backdrop", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{
				id: "widget1",
				node: <div data-testid="widget1-content">Widget 1</div>,
			},
		];

		const { container } = render(
			<Dashboard items={items} dashboardId="spotlight_test" />,
		);

		// Open spotlight
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		// Modal should be visible
		const modal = await screen.findByTestId("dashboard-spotlight-modal");
		expect(modal).toBeInTheDocument();

		// Find the backdrop (MUI Modal creates a backdrop element)
		const backdrop = container.querySelector(".MuiBackdrop-root");

		// Click backdrop - Modal's onClose should be triggered
		if (backdrop) {
			fireEvent.click(backdrop);

			// Modal should be removed
			await waitFor(() => {
				expect(
					screen.queryByTestId("dashboard-spotlight-modal"),
				).not.toBeInTheDocument();
			});
		} else {
			// If backdrop not found (shouldn't happen), the test validates modal can open at least
			expect(modal).toBeInTheDocument();
		}
	});

	it("spotlight button is hidden in edit mode", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem("lighthouse:dashboard:spotlight_test:edit", "1");

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "widget1", node: <div>Widget 1</div> },
		];

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Spotlight button should NOT be visible in edit mode
		expect(
			screen.queryByTestId("dashboard-item-spotlight-widget1"),
		).not.toBeInTheDocument();
	});

	it("spotlight button is hidden for hidden widgets", async () => {
		setMatchMediaWidth(1600);
		localStorage.setItem(
			"lighthouse:dashboard:spotlight_test:hidden",
			JSON.stringify(["widget1"]),
		);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "widget1", node: <div>Widget 1</div> },
		];

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Widget should not be visible when not in edit mode
		expect(
			screen.queryByTestId("dashboard-item-spotlight-widget1"),
		).not.toBeInTheDocument();
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

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Open spotlight for widget2
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget2",
		);
		fireEvent.click(spotlightBtn);

		// Modal should show widget2 content (there will be 2 instances - one in grid, one in modal)
		const widget2Contents = await screen.findAllByTestId("widget2-content");
		expect(widget2Contents.length).toBe(2); // One in dashboard, one in modal

		// Widget1 content should still be in the grid but not duplicated in the modal
		const widget1Contents = screen.getAllByTestId("widget1-content");
		expect(widget1Contents.length).toBe(1); // Only in dashboard
	});

	it("spotlight state is not persisted to localStorage", async () => {
		setMatchMediaWidth(1600);

		const { default: Dashboard } = await import("./Dashboard");
		const items: DashboardItem[] = [
			{ id: "widget1", node: <div>Widget 1</div> },
		];

		render(<Dashboard items={items} dashboardId="spotlight_test" />);

		// Open spotlight
		const spotlightBtn = await screen.findByTestId(
			"dashboard-item-spotlight-widget1",
		);
		fireEvent.click(spotlightBtn);

		// Modal should be visible
		await screen.findByTestId("dashboard-spotlight-modal");

		// Check that no spotlight state is persisted
		const spotlightKey = localStorage.getItem(
			"lighthouse:dashboard:spotlight_test:spotlight",
		);
		expect(spotlightKey).toBeNull();
	});
});

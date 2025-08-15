import { cleanup, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DashboardItem } from "./Dashboard";

// Helper to simulate different viewport widths by controlling matchMedia
function setMatchMediaWidth(width: number) {
	Object.defineProperty(window, "matchMedia", {
		writable: true,
		value: (query: string) => {
			// Extract min-width in px from the media query if present
			const m = query.match(/min-width:\s*(\d+)px/);
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
	});

	it("renders provided items and exposes data attributes", async () => {
		// wide screen (xl)
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
			render(<Dashboard items={items} />);
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

	it("hides lower-priority items when allowVerticalStacking is false on very small screens", async () => {
		// very small screen
		setMatchMediaWidth(300);

		const { default: Dashboard } = await import("./Dashboard");

		const items: DashboardItem[] = Array.from({ length: 6 }).map((_, i) => ({
			id: i,
			node: <div>Item {i}</div>,
			priority: i < 2 ? 10 : 100, // first two high priority, rest low
		}));

		render(<Dashboard items={items} allowVerticalStacking={false} />);

		// Expect only the high-priority items to be present (others hidden)
		expect(screen.queryByTestId("dashboard-item-0")).toBeInTheDocument();
		expect(screen.queryByTestId("dashboard-item-1")).toBeInTheDocument();
		expect(screen.queryByTestId("dashboard-item-2")).toBeNull();
		expect(screen.queryByTestId("dashboard-item-3")).toBeNull();
	});
});

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { testTheme } from "../../../tests/testTheme";
import BlackoutOverlay from "./BlackoutOverlay";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

const mockXScale = Object.assign(
	(label: string) => {
		const positions: Record<string, number> = {
			"17.3.2026": 100,
			"18.3.2026": 150,
			"19.3.2026": 200,
		};
		return positions[label];
	},
	{
		bandwidth: () => 40,
		step: () => 50,
	},
);

vi.mock("@mui/x-charts/hooks", () => ({
	useDrawingArea: () => ({ top: 10, height: 300 }),
	useXScale: () => mockXScale,
}));

describe("BlackoutOverlay", () => {
	it("should render nothing when blackoutDayLabels is empty", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={[]} />
			</svg>,
		);

		expect(
			container.querySelector('[data-testid="blackout-overlay"]'),
		).toBeNull();
	});

	it("should render overlay group when blackout labels are provided", () => {
		render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026", "18.3.2026"]} />
			</svg>,
		);

		expect(screen.getByTestId("blackout-overlay")).toBeInTheDocument();
	});

	it("should render two rect pairs per blackout day (background + hatch)", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026", "18.3.2026"]} />
			</svg>,
		);

		const overlay = container.querySelector('[data-testid="blackout-overlay"]');

		if (!overlay) {
			throw new Error("Blackout overlay group not found");
		}

		const rects = overlay.querySelectorAll("rect");
		// 2 labels × 2 rects each = 4
		expect(rects.length).toBe(4);
	});

	it("should render the hatch pattern definition", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026"]} />
			</svg>,
		);

		const pattern = container.querySelector("#blackout-hatch");
		expect(pattern).toBeInTheDocument();
		expect(pattern?.getAttribute("patternTransform")).toBe("rotate(45)");
	});

	it("should position rects using xScale coordinates and drawing area", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026"]} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		const firstRect = rects[0];

		// bandwidth > 0, so columnOffset = 0, x = xScale("17.3.2026") = 100
		expect(firstRect.getAttribute("x")).toBe("100");
		expect(firstRect.getAttribute("y")).toBe("10");
		expect(firstRect.getAttribute("width")).toBe("40");
		expect(firstRect.getAttribute("height")).toBe("300");
	});

	it("should skip labels that xScale returns undefined for", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026", "unknown-label"]} />
			</svg>,
		);

		const overlay = container.querySelector('[data-testid="blackout-overlay"]');

		if (!overlay) {
			throw new Error("Blackout overlay group not found");
		}

		const rects = overlay.querySelectorAll("rect");
		// Only "17.3.2026" resolves → 1 label × 2 rects = 2
		expect(rects.length).toBe(2);
	});

	it("should apply hatch fill pattern to second rect of each pair", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<BlackoutOverlay blackoutDayLabels={["17.3.2026"]} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		expect(rects[0].getAttribute("fill-opacity")).toBe("0.08");
		expect(rects[1].getAttribute("fill")).toBe("url(#blackout-hatch)");
	});
});

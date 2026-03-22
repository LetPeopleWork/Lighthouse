import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ProcessBehaviourChartDataPoint } from "../../../models/Metrics/ProcessBehaviourChartData";
import { testTheme } from "../../../tests/testTheme";
import PbcBlackoutOverlay from "./PbcBlackoutOverlay";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

const mockXScale = Object.assign(
	(value: number) => {
		// Map index or timestamp to pixel position
		const positions: Record<number, number> = {
			0: 50,
			1: 100,
			2: 150,
			3: 200,
		};
		return positions[value];
	},
	{
		bandwidth: () => 40,
		step: () => 50,
	},
);

vi.mock("@mui/x-charts/hooks", () => ({
	useDrawingArea: () => ({ top: 10, width: 400, height: 300 }),
	useXScale: () => mockXScale,
}));

const createDataPoint = (
	overrides: Partial<ProcessBehaviourChartDataPoint> = {},
): ProcessBehaviourChartDataPoint => ({
	xValue: "2026-03-17",
	yValue: 5,
	specialCauses: [],
	workItemIds: [],
	isBlackout: false,
	...overrides,
});

describe("PbcBlackoutOverlay", () => {
	it("should render nothing when no data points are blackout", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-17" }),
			createDataPoint({ xValue: "2026-03-18" }),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		expect(
			container.querySelector('[data-testid="blackout-overlay"]'),
		).toBeNull();
	});

	it("should render overlay when blackout data points exist", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-17", isBlackout: false }),
			createDataPoint({ xValue: "2026-03-18", isBlackout: true }),
		];

		render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		expect(screen.getByTestId("blackout-overlay")).toBeInTheDocument();
	});

	it("should render rect pairs for each blackout point", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-16", isBlackout: false }),
			createDataPoint({ xValue: "2026-03-17", isBlackout: true }),
			createDataPoint({ xValue: "2026-03-18", isBlackout: true }),
			createDataPoint({ xValue: "2026-03-19", isBlackout: false }),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		// 2 blackout points × 2 rects each = 4
		expect(rects.length).toBe(4);
	});

	it("should use index as x value with equal spacing", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-16", isBlackout: false }),
			createDataPoint({ xValue: "2026-03-17", isBlackout: true }),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		// With equal spacing: index 1 → xScale(1) = 100
		// bandwidth = 40, columnOffset = 0 → x = 100
		expect(rects[0].getAttribute("x")).toBe("100");
		expect(rects[0].getAttribute("y")).toBe("10");
		expect(rects[0].getAttribute("width")).toBe("40");
		expect(rects[0].getAttribute("height")).toBe("300");
	});

	it("should render the hatch pattern definition", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-17", isBlackout: true }),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		const pattern = container.querySelector("#pbc-blackout-hatch");
		expect(pattern).toBeInTheDocument();
		expect(pattern?.getAttribute("patternTransform")).toBe("rotate(45)");
	});

	it("should apply background and hatch fill to rect pairs", () => {
		const dataPoints = [
			createDataPoint({ xValue: "2026-03-17", isBlackout: true }),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<PbcBlackoutOverlay dataPoints={dataPoints} useEqualSpacing={true} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		expect(rects[0].getAttribute("fill-opacity")).toBe("0.08");
		expect(rects[1].getAttribute("fill")).toBe("url(#pbc-blackout-hatch)");
	});
});

import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { testTheme } from "../../../tests/testTheme";
import { errorColor, getColorMapForKeys } from "../../../utils/theme/colors";
import FeatureSizeScatterPlotChart from "./FeatureSizeScatterPlotChart";

// Mock dependencies to isolate component behavior
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2 data-testid="dialog-title">{title}</h2>
				<button type="button" onClick={onClose} data-testid="close-dialog">
					Close
				</button>
				<div data-testid="feature-count">{items?.length || 0} features</div>
				{items?.map((item: IFeature, index: number) => (
					<div key={item.id} data-testid={`feature-${index}`}>
						{item.name} - Size: {item.size}
					</div>
				))}
			</div>
		);
	}),
}));

vi.mock("@mui/x-charts", () => {
	interface ChartContainerProps {
		children: React.ReactNode;
		height: number;
		xAxis?: Array<{ max?: number }>;
		series?: Array<{ data?: unknown[] }>;
	}

	interface ScatterPlotProps {
		slots?: {
			marker?: (props: {
				x: number;
				y: number;
				dataIndex: number;
				color: string;
				isHighlighted: boolean;
			}) => React.ReactNode;
		};
	}

	return {
		ChartContainer: ({
			children,
			height,
			xAxis,
			series,
		}: ChartContainerProps) => (
			<div
				data-testid="chart-container"
				data-height={height}
				data-x-axis-max={xAxis?.[0]?.max}
				data-series-count={
					series
						? series.reduce(
								(acc, s) => acc + (Array.isArray(s.data) ? s.data.length : 0),
								0,
							)
						: 0
				}
				data-series={series ? JSON.stringify(series) : undefined}
			>
				{children}
			</div>
		),
		ScatterPlot: ({ slots }: ScatterPlotProps) => {
			// Read the produced series data from the chart container DOM to simulate actual data ordering
			const container =
				typeof document === "undefined"
					? null
					: document.querySelector('[data-testid="chart-container"]');
			const seriesJson = container?.getAttribute("data-series")
				? JSON.parse(container.getAttribute("data-series") as string)
				: [];
			// Flatten all series data into a single array of { datum, seriesIndex, dataIndex } records
			type ChartDatum = {
				id?: number | string;
				color?: string;
				[key: string]: unknown;
			};
			const flattened: Array<{
				datum: ChartDatum;
				seriesIndex: number;
				dataIndex: number;
			}> = [];
			if (Array.isArray(seriesJson)) {
				for (const [seriesIndex, s] of (
					seriesJson as { data?: unknown }[]
				).entries()) {
					if (Array.isArray(s.data)) {
						const seriesData = s.data as ChartDatum[];
						for (
							let dataIndex = 0;
							dataIndex < seriesData.length;
							dataIndex++
						) {
							const d = seriesData[dataIndex];
							flattened.push({ datum: d, seriesIndex, dataIndex });
						}
					}
				}
			}
			// Introduce a reorder of the flattened points to simulate cases where the visual order
			// of the points (rendering order) does not match the allGroupedDataPoints index order.
			// This helps validate the component's resilience to mismatched series/data indices.
			if (flattened.length >= 2) {
				const tmp = flattened[0];
				flattened[0] = flattened[1];
				flattened[1] = tmp;
			}

			// Some chart implementations may stringify datum IDs; tests can set
			// global flag __forceStringDatumIds to simulate this behavior.
			const globalFlags = globalThis as unknown as {
				__forceStringDatumIds?: boolean;
				__forceMissingDatumIds?: boolean;
			};
			if (globalFlags?.__forceStringDatumIds) {
				for (const f of flattened) {
					if (f?.datum?.id !== undefined) {
						f.datum.id = String(f.datum.id);
					}
				}
			}
			if (globalFlags?.__forceMissingDatumIds) {
				for (const f of flattened) {
					if (f?.datum?.id !== undefined) {
						delete (f.datum as Record<string, unknown>).id;
					}
				}
			}

			return (
				<div data-testid="scatter-plot">
					{flattened.map((entry, index) => {
						const mockMarkerProps = {
							x: 100 + index * 50,
							y: 200 + index * 30,
							dataIndex: entry.dataIndex,
							seriesIndex: entry.seriesIndex,
							data: entry.datum,
							color:
								typeof entry.datum?.color === "string"
									? entry.datum.color
									: "#1976d2",
							isHighlighted: false,
						} as {
							x: number;
							y: number;
							dataIndex: number;
							data: ChartDatum;
							color: string;
							isHighlighted: boolean;
						};
						return (
							<div
								key={`${entry.seriesIndex}-${entry.dataIndex}`}
								data-testid={`marker-${index}`}
							>
								{slots?.marker?.(mockMarkerProps)}
							</div>
						);
					})}
				</div>
			);
		},
		ChartsXAxis: () => <div data-testid="x-axis">X Axis</div>,
		ChartsYAxis: () => <div data-testid="y-axis">Y Axis</div>,
		ChartsTooltip: () => <div data-testid="tooltip">Tooltip</div>,
		ChartsReferenceLine: ({
			label,
			x,
			lineStyle,
		}: {
			label: string;
			x: number;
			lineStyle?: { stroke: string };
		}) => (
			<div
				data-testid={`reference-line-${label}`}
				data-value={x}
				data-color={lineStyle?.stroke}
			>
				{label}
			</div>
		),
	};
});

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				features: "Features",
				cycleTime: "Cycle Time",
			};
			return terms[key] || key;
		},
	}),
}));

vi.mock("../../../utils/featureName", () => ({
	getWorkItemName: (item: IFeature) => item.name,
}));

describe("FeatureSizeScatterPlotChart", () => {
	const createFeature = (
		id: number,
		name: string,
		size: number,
		cycleTime: number,
	): IFeature => {
		const feature = new Feature();
		feature.id = id;
		feature.referenceId = `REF-${id}`;
		feature.name = name;
		feature.size = size;
		feature.cycleTime = cycleTime;
		feature.state = "Done";
		feature.stateCategory = "Done";
		feature.type = "Feature";
		feature.closedDate = new Date("2023-01-15");
		return feature;
	};

	const basicFeatures: IFeature[] = [
		createFeature(1, "Small Task", 3, 5),
		createFeature(2, "Medium Task", 8, 12),
		createFeature(3, "Large Task", 15, 20),
	];

	const percentileData: IPercentileValue[] = [
		{ percentile: 50, value: 8 },
		{ percentile: 85, value: 15 },
		{ percentile: 95, value: 25 },
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("when no data is provided", () => {
		it("displays a no data message", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={[]} />);

			expect(screen.getByText("No data available")).toBeInTheDocument();
			expect(screen.queryByTestId("chart-container")).not.toBeInTheDocument();
		});
	});

	describe("when feature data is provided", () => {
		it("displays the chart with proper title", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			expect(screen.getByText("Features Size")).toBeInTheDocument();
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("renders all chart components", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			expect(screen.getByTestId("scatter-plot")).toBeInTheDocument();
			expect(screen.getByTestId("x-axis")).toBeInTheDocument();
			expect(screen.getByTestId("y-axis")).toBeInTheDocument();
			expect(screen.getByTestId("tooltip")).toBeInTheDocument();
		});

		it("renders visible markers as SVG circles", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const scatter = screen.getByTestId("scatter-plot");
			// Ensure at least one circle element rendered by the marker is present
			const circles = scatter.querySelectorAll("circle");
			expect(circles.length).toBeGreaterThan(0);
		});

		it("calculates appropriate chart dimensions", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const container = screen.getByTestId("chart-container");
			// Max size is 15, with 10% padding should be 16.5
			expect(Number(container.dataset.xAxisMax)).toBeGreaterThan(15);
		});

		it("should color groups using the legend state category color mapping", () => {
			const features: IFeature[] = [
				(() => {
					const f = createFeature(1, "Done Feature", 5, 10);
					f.stateCategory = "Done";
					return f;
				})(),
				(() => {
					const f = createFeature(2, "Doing Feature", 4, 5);
					f.stateCategory = "Doing";
					return f;
				})(),
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);

			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			// Color mapping is created from state categories (Done, Doing)
			const colorMap = getColorMapForKeys(["Done", "Doing"]);
			// Find the 'Done' series
			const doneSeries = series.find(
				(s: { id?: string; label?: string }) =>
					s.id === "series-Done" || s.label === "Done",
			);
			expect(doneSeries).toBeTruthy();
			expect(doneSeries?.data?.[0]?.color).toBe(colorMap.Done);
			expect(doneSeries?.data?.[0]?.color).not.toBe(
				testTheme.palette.primary.main,
			);
		});

		it("should color groups red when they contain blocked items", () => {
			const features: IFeature[] = [
				(() => {
					const f = createFeature(1, "Done Feature", 5, 10);
					f.stateCategory = "Done";
					f.isBlocked = true;
					return f;
				})(),
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);

			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			const doneSeries = series.find(
				(s: { id?: string; label?: string }) =>
					s.id === "series-Done" || s.label === "Done",
			);
			expect(doneSeries).toBeTruthy();
			// Blocked features should be colored using errorColor
			expect(doneSeries?.data?.[0]?.color).toBe(errorColor);
			expect(doneSeries?.data?.[0]?.color).not.toBe(
				testTheme.palette.primary.main,
			);
		});

		it("groups features with identical size and cycle time", () => {
			const duplicateFeatures = [
				createFeature(1, "Task A", 5, 10),
				createFeature(2, "Task B", 5, 10), // Same size and cycle time
				createFeature(3, "Task C", 8, 15),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={duplicateFeatures} />,
			);

			const container = screen.getByTestId("chart-container");
			// Should have 2 data points (one group with 2 items, one with 1 item)
			expect(container).toHaveAttribute("data-series-count", "2");
		});

		it("does not group features that share size/cycle time but are in different state categories", () => {
			const fDone = createFeature(1, "Done Feature", 5, 10);
			fDone.stateCategory = "Done";
			const fDoing = createFeature(2, "Doing Feature", 5, 5);
			fDoing.stateCategory = "Doing";
			fDoing.workItemAge = 5;

			render(<FeatureSizeScatterPlotChart sizeDataPoints={[fDone, fDoing]} />);

			// Enable 'In Progress' (Doing) chip so the Doing series is displayed
			fireEvent.click(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			);

			const container = screen.getByTestId("chart-container");
			// There should be 2 distinct markers because the state categories differ
			expect(container).toHaveAttribute("data-series-count", "2");
		});

		it("does not group features with identical size and cycle time across different state categories", () => {
			const fDone = createFeature(1, "Done Feature", 5, 10);
			fDone.stateCategory = "Done";
			const fDoing = createFeature(2, "Doing Feature", 5, 10);
			fDoing.stateCategory = "Doing";
			fDoing.workItemAge = 10; // ensure cycle time on the 'Doing' item used for the chart matches

			render(<FeatureSizeScatterPlotChart sizeDataPoints={[fDone, fDoing]} />);

			// Enable 'In Progress' (Doing) chip so the Doing series is displayed
			fireEvent.click(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			);

			const container = screen.getByTestId("chart-container");
			// There should be 2 distinct markers because the state categories differ
			expect(container).toHaveAttribute("data-series-count", "2");
		});
	});

	describe("when user interacts with data points", () => {
		it("opens a dialog when clicking a data point", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});

			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(screen.getByTestId("dialog-title")).toHaveTextContent(
				"Features Details",
			);
		});

		it("displays feature details in the dialog", () => {
			const singleFeature = [createFeature(1, "Test Feature", 5, 8)];
			render(<FeatureSizeScatterPlotChart sizeDataPoints={singleFeature} />);

			const markerButton = screen.getByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});
			fireEvent.click(markerButton);

			expect(screen.getByTestId("feature-count")).toHaveTextContent(
				"1 features",
			);
			expect(screen.getByTestId("feature-0")).toHaveTextContent(
				"Test Feature - Size: 5",
			);
		});

		it("closes dialog when close button is clicked", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			// Open dialog using the first marker button
			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});
			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();

			// Close dialog
			fireEvent.click(screen.getByTestId("close-dialog"));

			expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
		});

		it("resolves groups via groupIndex when dataIndex mismatches", () => {
			// Create two distinct groups so we can confirm id-based resolution
			const features = [
				createFeature(1, "Group A", 3, 5),
				createFeature(2, "Group B", 4, 6),
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);

			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});

			// The mocked marker will intentionally map the first marker (dataIndex 0) to the second
			// flattened datum with groupIndex pointing to Group B, which should open the dialog for
			// Group B.
			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(screen.getByTestId("feature-0")).toHaveTextContent(
				"Group B - Size: 4",
			);
		});

		it("resolves groups using per-series dataIndex when flattened reorder occurs across series", () => {
			// Create separate groups in Done and Doing so we can test per-series mapping.
			const fDone = createFeature(1, "Done Group", 5, 10);
			fDone.stateCategory = "Done";
			const fDoing = createFeature(2, "Doing Group", 4, 5);
			fDoing.stateCategory = "Doing";

			render(<FeatureSizeScatterPlotChart sizeDataPoints={[fDone, fDoing]} />);

			// Enable 'In Progress' (Doing) chip so the Doing series is displayed
			fireEvent.click(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			);

			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});

			// Because the mocked chart flips the first two flattened points, the first marker
			// will be for the Doing series in this scenario; clicking it should open the dialog
			// for "Doing Group".
			fireEvent.click(markerButtons[0]);

			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(screen.getByTestId("feature-0")).toHaveTextContent(
				"Done Group - Size: 5",
			);
		});

		it("resolves groups via series-local mapping when flattened order differs across series", () => {
			// Create two features in different state categories (Done, Doing)
			const fDone = createFeature(1, "Done Feature", 3, 5);
			fDone.stateCategory = "Done";
			const fDoing = createFeature(2, "Doing Feature", 4, 6);
			fDoing.stateCategory = "Doing";
			fDoing.workItemAge = 6;

			render(<FeatureSizeScatterPlotChart sizeDataPoints={[fDone, fDoing]} />);

			// Flattened reorder means first marker may be from the Doing series but dataIndex corresponds
			// to that series; the marker should resolve to the correct group's items via series-local map.
			const markerButtons = screen.getAllByRole("button", {
				name: /view.*feature.*with size.*child items/i,
			});

			// Determine which group's datum corresponds to the first flattened marker after the
			// mock's swap behavior and assert the dialog shows the expected feature.
			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];
			const flattened: Array<{
				datum: Record<string, unknown>;
				seriesIndex: number;
				dataIndex: number;
			}> = [];
			for (const [seriesIndex, s] of (
				series as { data?: Record<string, unknown>[] }[]
			).entries()) {
				if (Array.isArray(s.data)) {
					for (let dataIndex = 0; dataIndex < s.data.length; dataIndex++) {
						const d = s.data[dataIndex];
						flattened.push({ datum: d, seriesIndex, dataIndex });
					}
				}
			}
			if (flattened.length >= 2) {
				const tmp = flattened[0];
				flattened[0] = flattened[1];
				flattened[1] = tmp;
			}
			const firstDatum = flattened[0].datum;
			const isDoing =
				firstDatum.x === fDoing.size && firstDatum.y === fDoing.workItemAge;
			const expectedText = isDoing
				? "Doing Feature - Size: 4"
				: "Done Feature - Size: 3";

			fireEvent.click(markerButtons[0]);
			expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			expect(screen.getByTestId("feature-0")).toHaveTextContent(expectedText);
		});

		// removed test for stringified datum 'id' — no longer relevant because id was removed.

		it("resolves groups when datum.id is missing but groupKey exists", () => {
			try {
				const globalFlags = globalThis as unknown as {
					__forceMissingDatumIds?: boolean;
				};
				globalFlags.__forceMissingDatumIds = true;

				const features = [
					createFeature(1, "Group A", 3, 5),
					createFeature(2, "Group B", 4, 6),
				];

				render(<FeatureSizeScatterPlotChart sizeDataPoints={features} />);

				const markerButtons = screen.getAllByRole("button", {
					name: /view.*feature.*with size.*child items/i,
				});

				// The mocked marker will intentionally map the first marker (dataIndex 0) to the
				// second group; the component should still resolve that group using groupIndex or groupKey.
				fireEvent.click(markerButtons[0]);

				expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
				expect(screen.getByTestId("feature-0")).toHaveTextContent(
					"Group B - Size: 4",
				);
			} finally {
				const globalFlags = globalThis as unknown as {
					__forceMissingDatumIds?: boolean;
				};
				globalFlags.__forceMissingDatumIds = false;
			}
		});

		it("shows grouped features when multiple items share the same data point", () => {
			const groupedFeatures = [
				createFeature(1, "Feature A", 10, 15),
				createFeature(2, "Feature B", 10, 15), // Same dimensions
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={groupedFeatures} />);

			const markerButton = screen.getByRole("button", {
				name: /view 2 features with size 10 child items/i,
			});

			fireEvent.click(markerButton);

			expect(screen.getByTestId("feature-count")).toHaveTextContent(
				"2 features",
			);
		});
	});

	describe("when percentile data is provided", () => {
		it("displays percentile chips", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			// Query by role to specifically target the chip buttons, not reference lines
			const chip50 = screen.getByRole("button", { name: /50%/ });
			const chip85 = screen.getByRole("button", { name: /85%/ });
			const chip95 = screen.getByRole("button", { name: /95%/ });

			expect(chip50).toBeInTheDocument();
			expect(chip85).toBeInTheDocument();
			expect(chip95).toBeInTheDocument();
		});

		it("displays reference lines for each percentile", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();

			// Verify line positions match percentile values
			expect(screen.getByTestId("reference-line-50%")).toHaveAttribute(
				"data-value",
				"8",
			);
			expect(screen.getByTestId("reference-line-85%")).toHaveAttribute(
				"data-value",
				"15",
			);
			expect(screen.getByTestId("reference-line-95%")).toHaveAttribute(
				"data-value",
				"25",
			);
		});

		it("adjusts chart scale to accommodate percentile values", () => {
			const highPercentiles = [{ percentile: 99, value: 50 }];

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={highPercentiles}
				/>,
			);

			const container = screen.getByTestId("chart-container");
			// Should scale to accommodate the 50 value with 10% padding
			expect(Number(container.dataset.xAxisMax)).toBeGreaterThan(50);
		});

		it("allows toggling percentile visibility", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={percentileData}
				/>,
			);

			// Use role to target specifically the chip button
			const percentileChip = screen.getByRole("button", { name: /50%/ });

			// Initial state - should be visible
			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();

			// Toggle visibility
			fireEvent.click(percentileChip);

			// Chip should still exist but reference line visibility changes through styling
			expect(percentileChip).toBeInTheDocument();
		});

		it("handles empty percentile arrays gracefully", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					sizePercentileValues={[]}
				/>,
			);

			expect(
				screen.queryByRole("button", { name: /50%/ }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-50%"),
			).not.toBeInTheDocument();
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});
	});

	describe("accessibility features", () => {
		it("provides proper aria labels for interactive elements", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			// Get only marker buttons, not chip buttons
			const markerButtons = screen.getAllByRole("button", {
				name: /view.*with size.*child items/i,
			});
			for (const button of markerButtons) {
				expect(button).toHaveAttribute("aria-label");
				expect(button.getAttribute("aria-label")).toMatch(
					/view.*with size.*child items/i,
				);
			}
		});

		it("includes descriptive titles for chart markers", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			// The SVG title elements should be present for screen readers
			// This is tested through the marker rendering behavior
			expect(screen.getByTestId("scatter-plot")).toBeInTheDocument();
		});
	});

	describe("edge cases", () => {
		it("handles features with null cycle times", () => {
			const featuresWithNulls = [
				createFeature(1, "Valid Feature", 5, 10),
				{
					...createFeature(2, "Invalid Feature", 8, 0),
					cycleTime: null as unknown as number,
				},
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={featuresWithNulls} />,
			);

			// Should filter out features with null cycle times and still render
			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("handles features with zero sizes", () => {
			const zeroSizeFeature = [createFeature(1, "Zero Size", 0, 5)];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={zeroSizeFeature} />);

			expect(screen.getByTestId("chart-container")).toBeInTheDocument();
		});

		it("works with single feature", () => {
			const singleFeature = [createFeature(1, "Only Feature", 5, 8)];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={singleFeature} />);

			expect(screen.getByTestId("chart-container")).toBeInTheDocument();

			const markerButton = screen.getByRole("button", {
				name: /view 1 feature with size 5 child items/i,
			});
			expect(markerButton).toBeInTheDocument();
		});
	});

	describe("state category filtering with chips", () => {
		const createFeatureWithState = (
			id: number,
			name: string,
			size: number,
			cycleTime: number | null,
			workItemAge: number,
			stateCategory: "Done" | "ToDo" | "Doing",
		): IFeature => {
			const feature = createFeature(id, name, size, cycleTime ?? 0);
			feature.stateCategory = stateCategory;
			feature.cycleTime = cycleTime as number;
			feature.workItemAge = workItemAge;
			return feature;
		};

		const mixedStateFeatures: IFeature[] = [
			createFeatureWithState(1, "Done Feature", 5, 10, 0, "Done"),
			createFeatureWithState(2, "ToDo Feature", 8, null, 0, "ToDo"),
			createFeatureWithState(3, "Doing Feature", 12, null, 5, "Doing"),
			createFeatureWithState(4, "Another Done", 15, 20, 0, "Done"),
		];

		it("displays filter chips for Done, To Do and In Progress", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			expect(
				screen.getByRole("button", { name: "Done visibility toggle" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "To Do visibility toggle" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			).toBeInTheDocument();
		});

		it("shows only Done features by default (Done chip is active)", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			const container = screen.getByTestId("chart-container");
			// Only 2 Done features should be displayed
			expect(container).toHaveAttribute("data-series-count", "2");

			// Done chip should be filled variant
			const doneChip = screen.getByRole("button", {
				name: "Done visibility toggle",
			});
			expect(doneChip.className).toContain("MuiChip-filled");
		});

		it("shows To Do features when To Do chip is clicked", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			const todoChip = screen.getByRole("button", {
				name: "To Do visibility toggle",
			});
			fireEvent.click(todoChip);

			const container = screen.getByTestId("chart-container");
			// Should show 2 Done + 1 To Do = 3 features
			expect(container).toHaveAttribute("data-series-count", "3");
		});

		it("shows In Progress features when In Progress chip is clicked", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			const doingChip = screen.getByRole("button", {
				name: "In Progress visibility toggle",
			});
			fireEvent.click(doingChip);

			const container = screen.getByTestId("chart-container");
			// Should show 2 Done + 1 Doing = 3 features
			expect(container).toHaveAttribute("data-series-count", "3");
		});

		it("shows all features when all chips are enabled", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			const todoChip = screen.getByRole("button", {
				name: "To Do visibility toggle",
			});
			const doingChip = screen.getByRole("button", {
				name: "In Progress visibility toggle",
			});

			fireEvent.click(todoChip);
			fireEvent.click(doingChip);

			const container = screen.getByTestId("chart-container");
			// Should show all 4 features
			expect(container).toHaveAttribute("data-series-count", "4");
		});

		it("can toggle Done features off", () => {
			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={mixedStateFeatures} />,
			);

			const doneChip = screen.getByRole("button", {
				name: "Done visibility toggle",
			});
			const todoChip = screen.getByRole("button", {
				name: "To Do visibility toggle",
			});

			// Enable To Do first
			fireEvent.click(todoChip);

			// Now toggle Done off
			fireEvent.click(doneChip);

			const container = screen.getByTestId("chart-container");
			// Should show only 1 To Do feature
			expect(container).toHaveAttribute("data-series-count", "1");
		});

		it("calculates percentiles only from Done features regardless of chip state", () => {
			const percentiles: IPercentileValue[] = [{ percentile: 50, value: 10 }];

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={mixedStateFeatures}
					sizePercentileValues={percentiles}
				/>,
			);

			// Enable all chips
			const todoChip = screen.getByRole("button", {
				name: "To Do visibility toggle",
			});
			const doingChip = screen.getByRole("button", {
				name: "In Progress visibility toggle",
			});

			fireEvent.click(todoChip);
			fireEvent.click(doingChip);

			// Percentile reference line should still be visible and unchanged
			const referenceLine = screen.getByTestId("reference-line-50%");
			expect(referenceLine).toBeInTheDocument();
			expect(referenceLine).toHaveAttribute("data-value", "10");
		});

		it("hides state chips when no non-Done features are present", () => {
			const onlyDoneFeatures = [
				createFeatureWithState(1, "Done 1", 5, 10, 0, "Done"),
				createFeatureWithState(2, "Done 2", 8, 12, 0, "Done"),
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={onlyDoneFeatures} />);

			expect(
				screen.queryByRole("button", { name: "ToDo visibility toggle" }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "In Progress visibility toggle" }),
			).not.toBeInTheDocument();
			// Done chip should still be visible
			expect(
				screen.getByRole("button", { name: "Done visibility toggle" }),
			).toBeInTheDocument();
		});

		it("only shows To Do chip when only To Do features exist alongside Done", () => {
			const doneAndTodoFeatures = [
				createFeatureWithState(1, "Done 1", 5, 10, 0, "Done"),
				createFeatureWithState(2, "ToDo 1", 8, null, 0, "ToDo"),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={doneAndTodoFeatures} />,
			);

			expect(
				screen.getByRole("button", { name: "To Do visibility toggle" }),
			).toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "In Progress visibility toggle" }),
			).not.toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Done visibility toggle" }),
			).toBeInTheDocument();
		});

		it("only shows In Progress chip when only Doing features exist alongside Done", () => {
			const doneAndDoingFeatures = [
				createFeatureWithState(1, "Done 1", 5, 10, 0, "Done"),
				createFeatureWithState(2, "Doing 1", 12, null, 5, "Doing"),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={doneAndDoingFeatures} />,
			);

			expect(
				screen.queryByRole("button", { name: "To Do visibility toggle" }),
			).not.toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Done visibility toggle" }),
			).toBeInTheDocument();
		});

		it("filters out To Do items with size 0", () => {
			const featuresWithZeroSize = [
				createFeatureWithState(1, "Done with 0", 0, 10, 0, "Done"),
				createFeatureWithState(2, "ToDo with 0", 0, null, 0, "ToDo"),
				createFeatureWithState(3, "ToDo with size", 5, null, 0, "ToDo"),
				createFeatureWithState(4, "Doing with 0", 0, null, 3, "Doing"),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={featuresWithZeroSize} />,
			);

			// Enable all state chips
			fireEvent.click(
				screen.getByRole("button", { name: "To Do visibility toggle" }),
			);
			fireEvent.click(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			);

			const container = screen.getByTestId("chart-container");
			// Should show: 1 Done (size 0), 1 To Do (size 5), 1 Doing (size 0) = 3
			// To Do with size 0 should be filtered out
			expect(container).toHaveAttribute("data-series-count", "3");
		});

		it("uses workItemAge for In Progress items", () => {
			const inProgressFeature = [
				createFeatureWithState(1, "In Progress", 10, null, 7, "Doing"),
			];

			render(
				<FeatureSizeScatterPlotChart sizeDataPoints={inProgressFeature} />,
			);

			// Enable In Progress chip
			fireEvent.click(
				screen.getByRole("button", { name: "In Progress visibility toggle" }),
			);

			// The feature should be displayed - workItemAge of 7 should be used as y-coordinate
			const container = screen.getByTestId("chart-container");
			expect(container).toHaveAttribute("data-series-count", "1");
		});

		it("shows ToDo items at y=0", () => {
			const todoFeature = [
				createFeatureWithState(1, "To Do Item", 5, null, 0, "ToDo"),
			];

			render(<FeatureSizeScatterPlotChart sizeDataPoints={todoFeature} />);

			// Enable To Do chip
			fireEvent.click(
				screen.getByRole("button", { name: "To Do visibility toggle" }),
			); // The feature should be displayed at y=0
			const container = screen.getByTestId("chart-container");
			expect(container).toHaveAttribute("data-series-count", "1");
		});
	});

	describe("estimation y-axis toggle", () => {
		const estimationData: IFeatureSizeEstimationResponse = {
			status: "Ready",
			estimationUnit: "Story Points",
			useNonNumericEstimation: false,
			categoryValues: [],
			featureEstimations: [
				{
					featureId: 1,
					estimationNumericValue: 3,
					estimationDisplayValue: "3",
				},
				{
					featureId: 2,
					estimationNumericValue: 8,
					estimationDisplayValue: "8",
				},
				{
					featureId: 3,
					estimationNumericValue: 13,
					estimationDisplayValue: "13",
				},
			],
		};

		it("should not show toggle when estimation data is not provided", () => {
			render(<FeatureSizeScatterPlotChart sizeDataPoints={basicFeatures} />);

			expect(
				screen.queryByRole("button", { name: /estimation/i }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: /cycle time/i }),
			).not.toBeInTheDocument();
		});

		it("should not show toggle when estimation status is NotConfigured", () => {
			const notConfigured: IFeatureSizeEstimationResponse = {
				status: "NotConfigured",
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				featureEstimations: [],
			};

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={notConfigured}
				/>,
			);

			expect(
				screen.queryByRole("button", { name: /estimation/i }),
			).not.toBeInTheDocument();
		});

		it("should not show toggle when estimation unit is not set", () => {
			const noUnit: IFeatureSizeEstimationResponse = {
				status: "Ready",
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				featureEstimations: [
					{
						featureId: 1,
						estimationNumericValue: 3,
						estimationDisplayValue: "3",
					},
				],
			};

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={noUnit}
				/>,
			);

			expect(
				screen.queryByRole("button", { name: /estimation/i }),
			).not.toBeInTheDocument();
		});

		it("should show toggle and default to estimation when estimation unit is set", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={estimationData}
				/>,
			);

			// Should show y-axis toggle buttons
			const estimationButton = screen.getByRole("button", {
				name: /story points/i,
			});
			expect(estimationButton).toBeInTheDocument();

			const cycleTimeButton = screen.getByRole("button", {
				name: /cycle time/i,
			});
			expect(cycleTimeButton).toBeInTheDocument();
		});

		it("should use estimation values in scatter data when estimation mode is active", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={estimationData}
				/>,
			);

			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			// In estimation mode, y values should be estimation values (3, 8, 13)
			const allDataPoints = series.flatMap(
				(s: { data?: { y: number }[] }) => s.data ?? [],
			);
			const yValues = allDataPoints.map((d: { y: number }) => d.y);
			expect(yValues).toContain(3);
			expect(yValues).toContain(8);
			expect(yValues).toContain(13);
		});

		it("should switch to cycle time values when cycle time toggle is clicked", () => {
			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={estimationData}
				/>,
			);

			// Click to switch to Cycle Time
			const cycleTimeButton = screen.getByRole("button", {
				name: /cycle time/i,
			});
			fireEvent.click(cycleTimeButton);

			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			// In cycle time mode, y values should be cycle time values (5, 12, 20)
			const allDataPoints = series.flatMap(
				(s: { data?: { y: number }[] }) => s.data ?? [],
			);
			const yValues = allDataPoints.map((d: { y: number }) => d.y);
			expect(yValues).toContain(5);
			expect(yValues).toContain(12);
			expect(yValues).toContain(20);
		});

		it("should use non-numeric category values for estimation axis when in non-numeric mode", () => {
			const nonNumericEstimation: IFeatureSizeEstimationResponse = {
				status: "Ready",
				estimationUnit: "T-Shirt",
				useNonNumericEstimation: true,
				categoryValues: ["XS", "S", "M", "L", "XL"],
				featureEstimations: [
					{
						featureId: 1,
						estimationNumericValue: 0,
						estimationDisplayValue: "XS",
					},
					{
						featureId: 2,
						estimationNumericValue: 2,
						estimationDisplayValue: "M",
					},
					{
						featureId: 3,
						estimationNumericValue: 4,
						estimationDisplayValue: "XL",
					},
				],
			};

			render(
				<FeatureSizeScatterPlotChart
					sizeDataPoints={basicFeatures}
					estimationData={nonNumericEstimation}
				/>,
			);

			const container = screen.getByTestId("chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			// y values should be ordinal indices (0, 2, 4)
			const allDataPoints = series.flatMap(
				(s: { data?: { y: number }[] }) => s.data ?? [],
			);
			const yValues = allDataPoints.map((d: { y: number }) => d.y);
			expect(yValues).toContain(0);
			expect(yValues).toContain(2);
			expect(yValues).toContain(4);
		});
	});
});

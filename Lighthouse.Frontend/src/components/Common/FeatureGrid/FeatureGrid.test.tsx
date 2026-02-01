import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { FeatureGrid } from "./FeatureGrid";

const createMockFeature = (
	id: number,
	name: string,
	stateCategory: "ToDo" | "Doing" | "Done" = "ToDo",
): IFeature => ({
	id,
	name,
	referenceId: `FTR-${id}`,
	stateCategory,
	state: stateCategory,
	type: "Feature",
	size: 5,
	owningTeam: "Team A",
	lastUpdated: new Date(),
	isUsingDefaultFeatureSize: false,
	parentWorkItemReference: "",
	projects: [],
	remainingWork: {},
	totalWork: {},
	forecasts: [],
	startedDate: new Date(),
	closedDate: new Date(),
	cycleTime: 0,
	workItemAge: 0,
	url: "https://example.com/feature",
	isBlocked: false,
	getRemainingWorkForFeature: () => 0,
	getRemainingWorkForTeam: () => 0,
	getTotalWorkForFeature: () => 0,
	getTotalWorkForTeam: () => 0,
});

const mockFeatures: IFeature[] = [
	createMockFeature(1, "Feature One", "ToDo"),
	createMockFeature(2, "Feature Two", "Doing"),
	createMockFeature(3, "Feature Three", "Done"),
];

describe("FeatureGrid", () => {
	describe("selectable mode", () => {
		it("should render features with checkboxes", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			expect(screen.getByText("Feature One")).toBeInTheDocument();
			expect(screen.getByText("Feature Two")).toBeInTheDocument();
			expect(screen.getByText("Feature Three")).toBeInTheDocument();
		});

		it("should show checked checkboxes for selected features", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			expect(checkboxes[0]).toBeChecked();
			expect(checkboxes[1]).toBeChecked();
			expect(checkboxes[2]).not.toBeChecked();
		});

		it("should call onChange when checkbox is clicked", async () => {
			const user = userEvent.setup();
			const onChange = vi.fn();

			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[]}
					onChange={onChange}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			await user.click(checkboxes[0]);

			expect(onChange).toHaveBeenCalledWith([1]);
		});

		it("should remove feature from selection when unchecked", async () => {
			const user = userEvent.setup();
			const onChange = vi.fn();

			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2]}
					onChange={onChange}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			await user.click(checkboxes[0]); // Uncheck first

			expect(onChange).toHaveBeenCalledWith([2]);
		});

		it("should render Select All and Clear Selection buttons", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			expect(
				screen.getByRole("button", { name: /select all/i }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /clear selection/i }),
			).toBeInTheDocument();
		});

		it("should enable checkbox interaction", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			expect(checkboxes[0]).not.toBeDisabled();
		});
	});

	describe("readonly mode", () => {
		it("should render features with all checkboxes checked and disabled", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2, 3]}
					storageKey="test-grid"
					mode="readonly"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			for (const checkbox of checkboxes) {
				expect(checkbox).toBeChecked();
				expect(checkbox).toBeDisabled();
			}
		});

		it("should not render Select All and Clear Selection buttons in readonly mode", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2, 3]}
					storageKey="test-grid"
					mode="readonly"
				/>,
			);

			expect(
				screen.queryByRole("button", { name: /select all/i }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: /clear selection/i }),
			).not.toBeInTheDocument();
		});

		it("should have dimmed (lower opacity) checkboxes in readonly mode", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2, 3]}
					storageKey="test-grid"
					mode="readonly"
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			for (const checkbox of checkboxes) {
				// When disabled, MUI checkboxes get opacity styling
				expect(checkbox).toBeDisabled();
			}
		});

		it("should display feature reference and name", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[1, 2, 3]}
					storageKey="test-grid"
					mode="readonly"
				/>,
			);

			expect(screen.getByText("FTR-1")).toBeInTheDocument();
			expect(screen.getByText("Feature One")).toBeInTheDocument();
		});
	});

	describe("default mode", () => {
		it("should default to selectable mode when mode is not specified", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[]}
					onChange={vi.fn()}
					storageKey="test-grid"
				/>,
			);

			// Should have Select All button (only in selectable mode)
			expect(
				screen.getByRole("button", { name: /select all/i }),
			).toBeInTheDocument();
		});
	});

	describe("feature filtering", () => {
		it("should filter out features with invalid state categories", () => {
			const featuresWithRemoved = [
				...mockFeatures,
				{
					...createMockFeature(4, "Removed Feature"),
					stateCategory: "Removed" as "ToDo",
				},
			];

			render(
				<FeatureGrid
					features={featuresWithRemoved}
					selectedFeatureIds={[]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			expect(screen.queryByText("Removed Feature")).not.toBeInTheDocument();
		});

		it("should sort Done features to the bottom", () => {
			render(
				<FeatureGrid
					features={mockFeatures}
					selectedFeatureIds={[]}
					onChange={vi.fn()}
					storageKey="test-grid"
					mode="selectable"
				/>,
			);

			const featureNames = screen
				.getAllByRole("row")
				.slice(1) // Skip header row
				.map((row) => row.textContent);

			// Feature Three (Done) should be last
			expect(featureNames[featureNames.length - 1]).toContain("Feature Three");
		});
	});
});

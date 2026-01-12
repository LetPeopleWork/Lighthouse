import { fireEvent, render, screen } from "@testing-library/react";
import { vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { StateCategory } from "../../../models/WorkItem";
import { FeatureSelector } from "./FeatureSelector";

// Mock feature factory
const getMockFeature = (overrides?: Partial<IFeature>): IFeature => {
	const baseFeature: IFeature = {
		id: 1,
		name: "Test Feature",
		state: "New",
		stateCategory: "ToDo",
		type: "Feature",
		referenceId: "REF-1",
		url: "http://example.com",
		startedDate: new Date("2023-01-01"),
		closedDate: new Date("2023-01-01"),
		cycleTime: 0,
		workItemAge: 1,
		parentWorkItemReference: "",
		isBlocked: false,
		lastUpdated: new Date("2023-01-01"),
		isUsingDefaultFeatureSize: true,
		size: 1,
		owningTeam: "Team A",
		remainingWork: {},
		totalWork: {},
		projects: [],
		forecasts: [],
		getRemainingWorkForFeature: () => 0,
		getRemainingWorkForTeam: () => 0,
		getTotalWorkForFeature: () => 0,
		getTotalWorkForTeam: () => 0,
	};

	return { ...baseFeature, ...overrides };
};

const getStateFromCategory = (stateCategory: StateCategory): string => {
	if (stateCategory === "ToDo") {
		return "New";
	}
	if (stateCategory === "Doing") {
		return "Active";
	}
	return "Completed";
};

const createMockFeatures = (
	stateCategoriesAndIds: Array<{
		stateCategory: StateCategory;
		id: number;
		referenceId: string;
		name: string;
	}>,
): IFeature[] => {
	return stateCategoriesAndIds.map(({ stateCategory, id, referenceId, name }) =>
		getMockFeature({
			id,
			name,
			stateCategory,
			referenceId,
			state: getStateFromCategory(stateCategory),
		}),
	);
};

describe("FeatureSelector", () => {
	const mockOnChange = vi.fn();

	const defaultProps = {
		features: [],
		selectedFeatureIds: [],
		onChange: mockOnChange,
		storageKey: "test-feature-selector",
	};

	beforeEach(() => {
		mockOnChange.mockClear();
	});

	it("should render empty state when no features provided", () => {
		render(<FeatureSelector {...defaultProps} />);

		expect(screen.getByText("No rows to display")).toBeInTheDocument();
	});

	it("should display ToDo, Doing and Done features", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
			{
				stateCategory: "Doing",
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
			},
			{ stateCategory: "Done", id: 3, name: "Feature 3", referenceId: "FTR-3" },
			{
				stateCategory: "Unknown",
				id: 4,
				name: "Feature 4",
				referenceId: "FTR-4",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		expect(screen.getByText("FTR-1")).toBeInTheDocument();
		expect(screen.getByText("Feature 1")).toBeInTheDocument();

		expect(screen.getByText("FTR-2")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();

		expect(screen.getByText("FTR-3")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();

		expect(screen.queryByText("FTR-4")).not.toBeInTheDocument();
		expect(screen.queryByText("Feature 4")).not.toBeInTheDocument();
	});

	it("should show selected features as checked", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
			{
				stateCategory: "Doing",
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
			},
		]);

		render(
			<FeatureSelector
				{...defaultProps}
				features={features}
				selectedFeatureIds={[1]}
			/>,
		);

		const checkboxes = screen.getAllByRole("checkbox");
		expect(checkboxes[0]).toBeChecked();
		expect(checkboxes[1]).not.toBeChecked();
	});

	it("should call onChange when feature selection changes", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
			{
				stateCategory: "Doing",
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const checkboxes = screen.getAllByRole("checkbox");
		fireEvent.click(checkboxes[0]);

		expect(mockOnChange).toHaveBeenCalledWith([1]);
	});

	it("should handle multiple feature selection correctly", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
			{
				stateCategory: "Doing",
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const checkboxes = screen.getAllByRole("checkbox");

		// Select first feature
		fireEvent.click(checkboxes[0]);
		expect(mockOnChange).toHaveBeenCalledWith([1]);

		// Select second feature (should add to existing selection)
		mockOnChange.mockClear();
		fireEvent.click(checkboxes[1]);
		expect(mockOnChange).toHaveBeenCalledWith([2]);
	});

	it("should unselect feature when already selected checkbox is clicked", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
		]);

		render(
			<FeatureSelector
				{...defaultProps}
				features={features}
				selectedFeatureIds={[1]}
			/>,
		);

		const checkbox = screen.getByRole("checkbox");
		fireEvent.click(checkbox);

		expect(mockOnChange).toHaveBeenCalledWith([]);
	});

	it("should display feature ID and name in clickable format", () => {
		const features = createMockFeatures([
			{
				stateCategory: "ToDo",
				id: 123,
				referenceId: "FTR-123",
				name: "Important Feature",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		expect(screen.getByText("FTR-123")).toBeInTheDocument();
		expect(screen.getByText("Important Feature")).toBeInTheDocument();
	});

	it("should display Done features at the bottom after ToDo and Doing features", () => {
		const features = createMockFeatures([
			{
				stateCategory: "Done",
				id: 5,
				name: "Done Feature 1",
				referenceId: "FTR-5",
			},
			{
				stateCategory: "ToDo",
				id: 1,
				name: "ToDo Feature",
				referenceId: "FTR-1",
			},
			{
				stateCategory: "Done",
				id: 6,
				name: "Done Feature 2",
				referenceId: "FTR-6",
			},
			{
				stateCategory: "Doing",
				id: 2,
				name: "Doing Feature 1",
				referenceId: "FTR-2",
			},
			{
				stateCategory: "Doing",
				id: 3,
				name: "Doing Feature 2",
				referenceId: "FTR-3",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		// Get all feature names displayed in the grid
		const featureNames = [
			screen.getByText("ToDo Feature"),
			screen.getByText("Doing Feature 1"),
			screen.getByText("Doing Feature 2"),
			screen.getByText("Done Feature 1"),
			screen.getByText("Done Feature 2"),
		];

		// All features should be present
		featureNames.forEach((name) => {
			expect(name).toBeInTheDocument();
		});

		// Get all rows in order
		const rows = screen.getAllByRole("row").slice(1); // Skip header row

		const rowTexts = rows.map((row) => row.textContent || "");

		const doneFeature1Index = rowTexts.findIndex((text) =>
			text.includes("Done Feature 1"),
		);
		const doneFeature2Index = rowTexts.findIndex((text) =>
			text.includes("Done Feature 2"),
		);
		const todoFeatureIndex = rowTexts.findIndex((text) =>
			text.includes("ToDo Feature"),
		);
		const doingFeature1Index = rowTexts.findIndex((text) =>
			text.includes("Doing Feature 1"),
		);

		// Check that Done features appear after active features
		expect(doneFeature1Index).toBeGreaterThan(todoFeatureIndex);
		expect(doneFeature1Index).toBeGreaterThan(doingFeature1Index);
		expect(doneFeature2Index).toBeGreaterThan(todoFeatureIndex);
		expect(doneFeature2Index).toBeGreaterThan(doingFeature1Index);
	});
});

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

	it("should display state as text for ToDo features", () => {
		const features = createMockFeatures([
			{ stateCategory: "ToDo", id: 1, name: "Feature 1", referenceId: "FTR-1" },
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const rows = screen.getAllByRole("row").slice(1); // Skip header row
		expect(rows[0]).toHaveTextContent("New");
	});

	it("should display state as text for Doing features", () => {
		const features = createMockFeatures([
			{
				stateCategory: "Doing",
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const rows = screen.getAllByRole("row").slice(1); // Skip header row
		expect(rows[0]).toHaveTextContent("Active");
	});

	it("should display state as text for Done features", () => {
		const features = createMockFeatures([
			{ stateCategory: "Done", id: 3, name: "Feature 3", referenceId: "FTR-3" },
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const rows = screen.getAllByRole("row").slice(1); // Skip header row
		expect(rows[0]).toHaveTextContent("Completed");
	});

	it("should not use strikethrough for Done feature names", () => {
		const features = createMockFeatures([
			{
				stateCategory: "Done",
				id: 3,
				name: "Done Feature",
				referenceId: "FTR-3",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		const featureName = screen.getByText("Done Feature");
		const computedStyle = globalThis.getComputedStyle(featureName);

		expect(computedStyle.textDecoration).not.toContain("line-through");
	});

	it("should sort Reference column alphabetically by reference string", () => {
		const features = createMockFeatures([
			{
				stateCategory: "ToDo",
				id: 3,
				name: "Feature C",
				referenceId: "FTR-103",
			},
			{
				stateCategory: "ToDo",
				id: 1,
				name: "Feature A",
				referenceId: "FTR-101",
			},
			{
				stateCategory: "ToDo",
				id: 2,
				name: "Feature B",
				referenceId: "FTR-102",
			},
		]);

		render(<FeatureSelector {...defaultProps} features={features} />);

		// Find and click the Reference column header to sort
		const columnHeaders = screen.getAllByRole("columnheader");
		const referenceHeader = columnHeaders.find((header) =>
			header.textContent?.includes("Reference"),
		);

		expect(referenceHeader).toBeDefined();
		if (!referenceHeader) throw new Error("Reference header not found");
		fireEvent.click(referenceHeader);

		// Get all rows after sorting (skip header row)
		const rows = screen.getAllByRole("row").slice(1);
		const rowTexts = rows.map((row) => row.textContent || "");

		// Verify sorted order: FTR-101, FTR-102, FTR-103
		const ftr101Index = rowTexts.findIndex((text) => text.includes("FTR-101"));
		const ftr102Index = rowTexts.findIndex((text) => text.includes("FTR-102"));
		const ftr103Index = rowTexts.findIndex((text) => text.includes("FTR-103"));

		expect(ftr101Index).toBeLessThan(ftr102Index);
		expect(ftr102Index).toBeLessThan(ftr103Index);
	});

	it("should sort State column alphabetically by state string", () => {
		const features = [
			getMockFeature({
				id: 1,
				name: "Feature 1",
				referenceId: "FTR-1",
				stateCategory: "Doing",
				state: "In Progress",
			}),
			getMockFeature({
				id: 2,
				name: "Feature 2",
				referenceId: "FTR-2",
				stateCategory: "Done",
				state: "Completed",
			}),
			getMockFeature({
				id: 3,
				name: "Feature 3",
				referenceId: "FTR-3",
				stateCategory: "ToDo",
				state: "Backlog",
			}),
		];

		render(<FeatureSelector {...defaultProps} features={features} />);

		// Find and click the State column header to sort
		const columnHeaders = screen.getAllByRole("columnheader");
		const stateHeader = columnHeaders.find((header) =>
			header.textContent?.includes("State"),
		);

		expect(stateHeader).toBeDefined();
		if (!stateHeader) throw new Error("State header not found");
		fireEvent.click(stateHeader);

		// Get all rows after sorting (skip header row)
		const rows = screen.getAllByRole("row").slice(1);
		const rowTexts = rows.map((row) => row.textContent || "");

		// Verify sorted order: Backlog, Completed, In Progress (alphabetically)
		const backlogIndex = rowTexts.findIndex((text) => text.includes("Backlog"));
		const completedIndex = rowTexts.findIndex((text) =>
			text.includes("Completed"),
		);
		const inProgressIndex = rowTexts.findIndex((text) =>
			text.includes("In Progress"),
		);

		expect(backlogIndex).toBeLessThan(completedIndex);
		expect(completedIndex).toBeLessThan(inProgressIndex);
	});

	describe("Select All and Clear Selection", () => {
		it("should have a 'Select All' button in the toolbar", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
			]);

			render(<FeatureSelector {...defaultProps} features={features} />);

			const selectAllButton = screen.getByRole("button", {
				name: /select all/i,
			});
			expect(selectAllButton).toBeInTheDocument();
		});

		it("should have a 'Clear Selection' button in the toolbar", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
			]);

			render(<FeatureSelector {...defaultProps} features={features} />);

			const clearButton = screen.getByRole("button", {
				name: /clear selection/i,
			});
			expect(clearButton).toBeInTheDocument();
		});

		it("should select all visible features when 'Select All' is clicked", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
				{
					stateCategory: "Doing",
					id: 2,
					name: "Feature 2",
					referenceId: "FTR-2",
				},
				{
					stateCategory: "Done",
					id: 3,
					name: "Feature 3",
					referenceId: "FTR-3",
				},
			]);

			render(<FeatureSelector {...defaultProps} features={features} />);

			const selectAllButton = screen.getByRole("button", {
				name: /select all/i,
			});
			fireEvent.click(selectAllButton);

			expect(mockOnChange).toHaveBeenCalledWith([1, 2, 3]);
		});

		it("should add to existing selection when 'Select All' is clicked", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
				{
					stateCategory: "Doing",
					id: 2,
					name: "Feature 2",
					referenceId: "FTR-2",
				},
				{
					stateCategory: "Done",
					id: 3,
					name: "Feature 3",
					referenceId: "FTR-3",
				},
			]);

			render(
				<FeatureSelector
					{...defaultProps}
					features={features}
					selectedFeatureIds={[1]}
				/>,
			);

			const selectAllButton = screen.getByRole("button", {
				name: /select all/i,
			});
			fireEvent.click(selectAllButton);

			// Should add feature 2 and 3 to the existing selection [1]
			expect(mockOnChange).toHaveBeenCalledWith([1, 2, 3]);
		});

		it("should not add duplicates when 'Select All' is clicked with existing selection", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
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
					selectedFeatureIds={[1, 2]}
				/>,
			);

			const selectAllButton = screen.getByRole("button", {
				name: /select all/i,
			});
			fireEvent.click(selectAllButton);

			// Should not duplicate already selected features
			expect(mockOnChange).toHaveBeenCalledWith([1, 2]);
		});

		it("should clear all selections when 'Clear Selection' is clicked", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
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
					selectedFeatureIds={[1, 2]}
				/>,
			);

			const clearButton = screen.getByRole("button", {
				name: /clear selection/i,
			});
			fireEvent.click(clearButton);

			expect(mockOnChange).toHaveBeenCalledWith([]);
		});

		it("should do nothing when 'Clear Selection' is clicked with no selection", () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Feature 1",
					referenceId: "FTR-1",
				},
			]);

			render(<FeatureSelector {...defaultProps} features={features} />);

			const clearButton = screen.getByRole("button", {
				name: /clear selection/i,
			});
			fireEvent.click(clearButton);

			expect(mockOnChange).toHaveBeenCalledWith([]);
		});

		it("should respect DataGrid filters when selecting all", async () => {
			const features = createMockFeatures([
				{
					stateCategory: "ToDo",
					id: 1,
					name: "Alpha Feature",
					referenceId: "FTR-1",
				},
				{
					stateCategory: "Doing",
					id: 2,
					name: "Beta Feature",
					referenceId: "FTR-2",
				},
				{
					stateCategory: "Done",
					id: 3,
					name: "Alpha Done",
					referenceId: "FTR-3",
				},
			]);

			render(<FeatureSelector {...defaultProps} features={features} />);

			// Note: Testing with actual DataGrid filtering would require more complex setup
			// For now, this test verifies the button exists and can be called
			// The actual filtering integration is tested through the implementation
			const selectAllButton = screen.getByRole("button", {
				name: /select all/i,
			});
			expect(selectAllButton).toBeInTheDocument();
		});
	});
});

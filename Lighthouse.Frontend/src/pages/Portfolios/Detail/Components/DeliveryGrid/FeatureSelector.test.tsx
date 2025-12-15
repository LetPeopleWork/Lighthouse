import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { FeatureSelector } from "./FeatureSelector";

// Mock the useTerminology hook
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

describe("FeatureSelector", () => {
	const mockOnSelectionChange = vi.fn();

	const createMockFeature = (
		id: number,
		name: string,
		stateCategory: "ToDo" | "Doing" | "Done",
	): IFeature => {
		return {
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
			url: "",
			isBlocked: false,
			getRemainingWorkForFeature: () => 0,
			getRemainingWorkForTeam: () => 0,
			getTotalWorkForFeature: () => 0,
			getTotalWorkForTeam: () => 0,
		};
	};

	const mockFeatures: IFeature[] = [
		createMockFeature(1, "Feature Alpha", "ToDo"),
		createMockFeature(2, "Feature Beta", "Doing"),
		createMockFeature(3, "Feature Gamma", "Done"),
		createMockFeature(4, "Feature Delta", "ToDo"),
		createMockFeature(5, "Feature Echo", "Doing"),
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	const renderFeatureSelector = (
		features = mockFeatures,
		selectedIds: number[] = [],
	) => {
		return render(
			<FeatureSelector
				features={features}
				selectedFeatureIds={selectedIds}
				onSelectionChange={mockOnSelectionChange}
			/>,
		);
	};

	it("should render DataGrid with eligible features only (ToDo and Doing)", () => {
		renderFeatureSelector();

		// Should show ToDo and Doing features
		expect(screen.getByText("Feature Alpha")).toBeInTheDocument();
		expect(screen.getByText("Feature Beta")).toBeInTheDocument();
		expect(screen.getByText("Feature Delta")).toBeInTheDocument();
		expect(screen.getByText("Feature Echo")).toBeInTheDocument();

		// Should not show Done features
		expect(screen.queryByText("Feature Gamma")).not.toBeInTheDocument();
	});

	it("should display feature columns: id, name", () => {
		renderFeatureSelector();

		// Check for column headers
		expect(screen.getByText("ID")).toBeInTheDocument();
		expect(screen.getByText("Name")).toBeInTheDocument();

		// Check feature data is displayed
		expect(screen.getByText("1")).toBeInTheDocument(); // Feature Alpha ID
		expect(screen.getByText("Feature Alpha")).toBeInTheDocument();
		expect(screen.getByText("Feature Beta")).toBeInTheDocument();
	});

	it("should have search functionality", async () => {
		const user = userEvent.setup();
		renderFeatureSelector();

		const searchInput = screen.getByPlaceholderText("Search features...");
		await user.type(searchInput, "Alpha");

		// Should show only matching features
		expect(screen.getByText("Feature Alpha")).toBeInTheDocument();
		expect(screen.queryByText("Feature Beta")).not.toBeInTheDocument();
		expect(screen.queryByText("Feature Delta")).not.toBeInTheDocument();
	});

	it("should search by feature name", async () => {
		const user = userEvent.setup();
		renderFeatureSelector();

		const searchInput = screen.getByPlaceholderText("Search features...");
		await user.type(searchInput, "Alpha");

		// Should show Feature Alpha based on name search
		expect(screen.getByText("Feature Alpha")).toBeInTheDocument();
		// Should not show Feature Beta
		expect(screen.queryByText("Feature Beta")).not.toBeInTheDocument();
	});

	it("should have select all functionality", async () => {
		const user = userEvent.setup();
		renderFeatureSelector();

		// Find select all checkbox by its text label
		const selectAllLabel = screen.getByText("Select All");
		const selectAllCheckbox =
			selectAllLabel.previousElementSibling?.querySelector(
				"input[type='checkbox']",
			);

		if (!selectAllCheckbox) {
			throw new Error("Select All checkbox not found");
		}

		await user.click(selectAllCheckbox);

		// Should select all eligible features (ToDo and Doing only)
		expect(mockOnSelectionChange).toHaveBeenCalledWith([1, 2, 4, 5]);
	});

	it("should have deselect all functionality", async () => {
		const user = userEvent.setup();
		renderFeatureSelector(mockFeatures, [1, 2, 4, 5]); // Start with ALL eligible features selected

		// Find select all checkbox by its text label (should say "Deselect All" when all are selected)
		const selectAllLabel = screen.getByText("Deselect All");
		const selectAllCheckbox =
			selectAllLabel.previousElementSibling?.querySelector(
				"input[type='checkbox']",
			);

		if (!selectAllCheckbox) {
			throw new Error("Select All checkbox not found");
		}

		await user.click(selectAllCheckbox); // This should deselect all since some were selected

		expect(mockOnSelectionChange).toHaveBeenCalledWith([]);
	});

	it("should handle individual feature selection", async () => {
		const user = userEvent.setup();
		renderFeatureSelector();

		// Find and click checkbox for Feature Alpha by finding its row
		const featureAlphaName = screen.getByText("Feature Alpha");
		const featureRow = featureAlphaName.closest("[role='row']");
		const featureCheckbox = featureRow?.querySelector("input[type='checkbox']");

		if (!featureCheckbox) {
			throw new Error("Feature Alpha checkbox not found");
		}

		await user.click(featureCheckbox);

		expect(mockOnSelectionChange).toHaveBeenCalledWith([1]);
	});

	it("should show selected features as checked", () => {
		renderFeatureSelector(mockFeatures, [1, 4]);

		// Check Feature Alpha checkbox (ID 1)
		const featureAlphaName = screen.getByText("Feature Alpha");
		const alphaRow = featureAlphaName.closest("[role='row']");
		const alphaCheckbox = alphaRow?.querySelector(
			"input[type='checkbox']",
		) as HTMLInputElement;

		// Check Feature Delta checkbox (ID 4)
		const featureDeltaName = screen.getByText("Feature Delta");
		const deltaRow = featureDeltaName.closest("[role='row']");
		const deltaCheckbox = deltaRow?.querySelector(
			"input[type='checkbox']",
		) as HTMLInputElement;

		// Check Feature Beta checkbox (ID 2) - should not be checked
		const featureBetaName = screen.getByText("Feature Beta");
		const betaRow = featureBetaName.closest("[role='row']");
		const betaCheckbox = betaRow?.querySelector(
			"input[type='checkbox']",
		) as HTMLInputElement;

		expect(alphaCheckbox?.checked).toBe(true);
		expect(deltaCheckbox?.checked).toBe(true);
		expect(betaCheckbox?.checked).toBe(false);
	});

	it("should show select all as indeterminate when some features are selected", () => {
		renderFeatureSelector(mockFeatures, [1, 2]); // Some but not all eligible features

		const selectAllLabel = screen.getByText("Select All");
		const selectAllCheckbox =
			selectAllLabel.previousElementSibling?.querySelector(
				"input[type='checkbox']",
			) as HTMLInputElement;

		expect(selectAllCheckbox?.dataset.indeterminate).toBe("true");
	});

	it("should show select all as checked when all eligible features are selected", () => {
		renderFeatureSelector(mockFeatures, [1, 2, 4, 5]); // All eligible features

		const selectAllLabel = screen.getByText("Deselect All");
		const selectAllCheckbox =
			selectAllLabel.previousElementSibling?.querySelector(
				"input[type='checkbox']",
			) as HTMLInputElement;

		expect(selectAllCheckbox?.checked).toBe(true);
		expect(selectAllCheckbox?.dataset.indeterminate).toBe("false");
	});

	it("should not include Done features in select all operation", async () => {
		const user = userEvent.setup();
		renderFeatureSelector();

		// Find select all checkbox by its text label
		const selectAllLabel = screen.getByText("Select All");
		const selectAllCheckbox =
			selectAllLabel.previousElementSibling?.querySelector(
				"input[type='checkbox']",
			);

		if (!selectAllCheckbox) {
			throw new Error("Select All checkbox not found");
		}

		await user.click(selectAllCheckbox);

		// Should only select ToDo and Doing features (1, 2, 4, 5), not Done feature (3)
		expect(mockOnSelectionChange).toHaveBeenCalledWith([1, 2, 4, 5]);
	});
});

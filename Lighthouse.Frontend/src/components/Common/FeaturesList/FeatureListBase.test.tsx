import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { Feature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../tests/MockApiServiceProvider";
import FeatureListBase from "./FeatureListBase";

describe("FeatureListBase component", () => {
	const createFeature = (
		id: number,
		name: string,
		stateCategory: "ToDo" | "Doing" | "Done" | "Unknown",
		parentWorkItemReference?: string,
	): Feature => {
		const feature = new Feature();
		feature.name = name;
		feature.id = id;
		feature.stateCategory = stateCategory;
		feature.referenceId = `FTR-${id}`;
		feature.remainingWork = { 1: 5 };
		feature.totalWork = { 1: 10 };
		feature.forecasts = [WhenForecast.new(80, new Date())];
		if (parentWorkItemReference) {
			feature.parentWorkItemReference = parentWorkItemReference;
		}

		return feature;
	};

	const features = [
		createFeature(1, "Feature 1", "ToDo"),
		createFeature(2, "Feature 2", "Doing"),
		createFeature(3, "Feature 3", "Done"),
	];

	// Mock localStorage before each test
	let mockLocalStorage: { [key: string]: string } = {};

	beforeEach(() => {
		mockLocalStorage = {};

		// Mock localStorage methods
		Storage.prototype.getItem = vi.fn((key) => mockLocalStorage[key] || null);
		Storage.prototype.setItem = vi.fn((key, value) => {
			mockLocalStorage[key] = value.toString();
		});
		Storage.prototype.removeItem = vi.fn((key) => {
			delete mockLocalStorage[key];
		});
		Storage.prototype.clear = vi.fn(() => {
			mockLocalStorage = {};
		});
	});

	it("should render all features initially", () => {
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();
	});

	it("should hide completed features when toggle is activated", async () => {
		const user = userEvent.setup();
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify all features are shown initially
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();

		// Find and activate the toggle
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		await user.click(toggle);

		// Verify completed feature is hidden
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.queryByText("Feature 3")).not.toBeInTheDocument();
	});

	it("should show all features again when toggle is turned off", async () => {
		const user = userEvent.setup();
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Find and activate the toggle
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		await user.click(toggle);

		// Verify completed feature is hidden
		expect(screen.queryByText("Feature 3")).not.toBeInTheDocument();

		// Turn the toggle off again
		await user.click(toggle);

		// Verify all features are shown again
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();
	});

	it("should load the saved preference from localStorage on mount", async () => {
		mockLocalStorage.lighthouse_hide_completed_features_project_1 = "true";

		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify completed feature is hidden on initial render because of localStorage
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.queryByText("Feature 3")).not.toBeInTheDocument();

		expect(screen.queryByText("Feature 3")).not.toBeInTheDocument();
	});

	it("should use different storage keys for different contexts", async () => {
		const user = userEvent.setup();

		// Render with project context
		const { unmount } = render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Activate toggle for project context
		const projectToggle = screen.getByTestId("hide-completed-features-toggle");
		await user.click(projectToggle);

		// Verify localStorage was set for project context
		expect(mockLocalStorage.lighthouse_hide_completed_features_project_1).toBe(
			"true",
		);

		// Unmount the component
		unmount();

		// Render with team context
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="team"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify the team context toggle starts unchecked (not affected by project setting)
		expect(
			screen.getByTestId("hide-completed-features-toggle"),
		).not.toBeChecked();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();

		// Activate toggle for team context
		const teamToggle = screen.getByTestId("hide-completed-features-toggle");
		await user.click(teamToggle);

		// Verify localStorage was set for team context
		expect(mockLocalStorage.lighthouse_hide_completed_features_team_1).toBe(
			"true",
		);

		// Verify both settings exist independently in localStorage
		expect(mockLocalStorage.lighthouse_hide_completed_features_project_1).toBe(
			"true",
		);
		expect(mockLocalStorage.lighthouse_hide_completed_features_team_1).toBe(
			"true",
		);
	});

	it("should use default setting when no localStorage value exists", () => {
		// No localStorage value is set

		render(
			<FeatureListBase
				features={features}
				contextId={42}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify all features are shown by default (including completed ones)
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();

		// Verify the toggle is unchecked by default (by confirming all features are visible)
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();
	});

	it("should have the group features by parent toggle unchecked by default", () => {
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		const groupingToggle = screen.getByTestId(
			"group-features-by-parent-toggle",
		);
		const input = groupingToggle.querySelector('input[type="checkbox"]');
		expect(input).not.toBeChecked();
	});

	it("should update localStorage when group features by parent toggle is changed", async () => {
		const user = userEvent.setup();
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Find and activate the toggle
		const groupingToggle = screen.getByTestId(
			"group-features-by-parent-toggle",
		);
		await user.click(groupingToggle);

		// Verify localStorage was updated
		expect(mockLocalStorage.lighthouse_group_features_by_parent_project_1).toBe(
			"true",
		);

		// Toggle it off
		await user.click(groupingToggle);

		// Verify localStorage was updated
		expect(mockLocalStorage.lighthouse_group_features_by_parent_project_1).toBe(
			"false",
		);
	});

	it("should load the saved grouping preference from localStorage on mount", async () => {
		mockLocalStorage.lighthouse_group_features_by_parent_project_1 = "true";

		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify the toggle is checked based on localStorage value
		const groupingToggle = screen.getByTestId(
			"group-features-by-parent-toggle",
		);
		const input = groupingToggle.querySelector('input[type="checkbox"]');
		expect(input).toBeChecked();
	});

	it("should use different storage keys for different contexts for grouping feature", async () => {
		const user = userEvent.setup();

		// Render with project context
		const { unmount } = render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="project"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Activate grouping toggle for project context
		const projectGroupingToggle = screen.getByTestId(
			"group-features-by-parent-toggle",
		);
		await user.click(projectGroupingToggle);

		// Verify localStorage was set for project context
		expect(mockLocalStorage.lighthouse_group_features_by_parent_project_1).toBe(
			"true",
		);

		// Unmount the component
		unmount();

		// Render with team context
		render(
			<FeatureListBase
				features={features}
				contextId={1}
				contextType="team"
				renderTableHeader={() => (
					<tr>
						<th>Name</th>
					</tr>
				)}
				renderTableRow={(feature: IFeature) => (
					<tr key={feature.id}>
						<td>{feature.name}</td>
					</tr>
				)}
			/>,
		);

		// Verify the team context toggle starts unchecked (not affected by project setting)
		const teamGroupingToggle = screen.getByTestId(
			"group-features-by-parent-toggle",
		);
		const input = teamGroupingToggle.querySelector('input[type="checkbox"]');
		expect(input).not.toBeChecked();

		// Activate toggle for team context
		await user.click(teamGroupingToggle);

		// Verify localStorage was set for team context
		expect(mockLocalStorage.lighthouse_group_features_by_parent_team_1).toBe(
			"true",
		);

		// Verify both settings exist independently in localStorage
		expect(mockLocalStorage.lighthouse_group_features_by_parent_project_1).toBe(
			"true",
		);
		expect(mockLocalStorage.lighthouse_group_features_by_parent_team_1).toBe(
			"true",
		);
	});

	// Tests for the grouping features functionality
	describe("Feature grouping", () => {
		// Features with different parent work items
		const featuresWithParents = [
			createFeature(1, "Feature 1", "ToDo", "PARENT-100"),
			createFeature(2, "Feature 2", "Doing", "PARENT-100"),
			createFeature(3, "Feature 3", "Done", "PARENT-100"),
			createFeature(4, "Feature 4", "ToDo", "PARENT-200"),
			createFeature(5, "Feature 5", "Doing"), // No parent
			createFeature(6, "Feature 6", "Done"), // No parent
		];

		// Test for parent feature name display
		it("should display parent feature names as clickable links when available", async () => {
			const user = userEvent.setup();

			// Create mock parent features
			const mockParent100 = new Feature();
			mockParent100.name = "Parent Feature 100";
			mockParent100.referenceId = "PARENT-100";
			mockParent100.url = "https://example.com/parent-100";

			const mockParent200 = new Feature();
			mockParent200.name = "Parent Feature 200";
			mockParent200.referenceId = "PARENT-200";
			mockParent200.url = "https://example.com/parent-200";

			// Setup mock feature service
			const mockFeatureService = createMockFeatureService();
			const getParentFeaturesMock =
				mockFeatureService.getParentFeatures as Mock;
			getParentFeaturesMock.mockResolvedValue([mockParent100, mockParent200]);

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Render with context provider
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={featuresWithParents}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Wait for parent features to load using the combined text format
			await screen.findByText("PARENT-100: Parent Feature 100");

			// Check that parent names are displayed instead of IDs
			const parent100Link = screen.getByText("PARENT-100: Parent Feature 100");
			expect(parent100Link).toBeInTheDocument();
			expect(parent100Link.closest("a")).toHaveAttribute(
				"href",
				"https://example.com/parent-100",
			);
			expect(parent100Link.closest("a")).toHaveAttribute("target", "_blank");

			const parent200Link = screen.getByText("PARENT-200: Parent Feature 200");
			expect(parent200Link).toBeInTheDocument();
			expect(parent200Link.closest("a")).toHaveAttribute(
				"href",
				"https://example.com/parent-200",
			);
			expect(parent200Link.closest("a")).toHaveAttribute("target", "_blank");

			// "No Parent" should still be shown for features without parents
			expect(screen.getByText("No Parent")).toBeInTheDocument();
		});

		it("should show loading state while fetching parent features", async () => {
			const user = userEvent.setup();

			// Setup mock feature service with a delayed response
			const mockFeatureService = createMockFeatureService();
			let resolvePromise: (value: Feature[]) => void = () => {};
			const parentFeaturesPromise = new Promise<Feature[]>((resolve) => {
				resolvePromise = resolve;
			});
			const getParentFeaturesMock =
				mockFeatureService.getParentFeatures as Mock;
			getParentFeaturesMock.mockReturnValue(parentFeaturesPromise);

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Render with context provider
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={featuresWithParents}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Verify loading messages are shown
			expect(
				screen.getByText("Loading parent Feature PARENT-100..."),
			).toBeInTheDocument();
			expect(
				screen.getByText("Loading parent Feature PARENT-200..."),
			).toBeInTheDocument();

			// Create mock parent features and resolve the promise
			const mockParent100 = new Feature();
			mockParent100.name = "Parent Feature 100";
			mockParent100.referenceId = "PARENT-100";
			mockParent100.url = "https://example.com/parent-100";

			const mockParent200 = new Feature();
			mockParent200.name = "Parent Feature 200";
			mockParent200.referenceId = "PARENT-200";
			mockParent200.url = "https://example.com/parent-200";

			// Resolve the promise
			resolvePromise([mockParent100, mockParent200]);

			// Wait for parent features to load
			await screen.findByText(
				(content) =>
					content.includes("PARENT-100") &&
					content.includes("Parent Feature 100"),
			);

			// Check that parent names are now displayed
			expect(
				screen.queryByText("Loading parent feature PARENT-100..."),
			).not.toBeInTheDocument();
			expect(
				screen.getByText(
					(content) =>
						content.includes("PARENT-100") &&
						content.includes("Parent Feature 100"),
				),
			).toBeInTheDocument();
			expect(
				screen.getByText(
					(content) =>
						content.includes("PARENT-200") &&
						content.includes("Parent Feature 200"),
				),
			).toBeInTheDocument();
		});

		it("should fallback to parent ID if parent feature name is not available", async () => {
			const user = userEvent.setup();

			// Create mock parent feature without a name
			const mockParent100 = new Feature();
			// Intentionally not setting a name
			mockParent100.referenceId = "PARENT-100";
			mockParent100.url = "https://example.com/parent-100";

			// Setup mock feature service
			const mockFeatureService = createMockFeatureService();
			(mockFeatureService.getParentFeatures as Mock).mockResolvedValue([
				mockParent100,
			]);

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Filter features to only include those with PARENT-100
			const filteredFeatures = featuresWithParents.filter(
				(f) => f.parentWorkItemReference === "PARENT-100",
			);

			// Render with context provider
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={filteredFeatures}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Wait for API call to complete
			await new Promise((resolve) => setTimeout(resolve, 0));

			// Check that we fallback to showing PARENT-100 with undefined as name
			const parentLink = screen.getByText(
				(content) =>
					content.includes("PARENT-100") && content.includes("undefined"),
			);
			expect(parentLink).toBeInTheDocument();
			expect(parentLink.closest("a")).toHaveAttribute(
				"href",
				"https://example.com/parent-100",
			);
		});

		it("should preserve backend order of parent features", async () => {
			const user = userEvent.setup();

			// Create features with non-alphabetically ordered parent IDs
			const featuresWithOrderedParents = [
				createFeature(7, "Feature 7", "ToDo", "PARENT-300"),
				createFeature(8, "Feature 8", "ToDo", "PARENT-100"),
				createFeature(9, "Feature 9", "ToDo", "PARENT-200"),
			];

			// Create mock parent features in a specific order
			const mockParent100 = new Feature();
			mockParent100.name = "Parent Feature 100";
			mockParent100.referenceId = "PARENT-100";

			const mockParent200 = new Feature();
			mockParent200.name = "Parent Feature 200";
			mockParent200.referenceId = "PARENT-200";

			const mockParent300 = new Feature();
			mockParent300.name = "Parent Feature 300";
			mockParent300.referenceId = "PARENT-300";

			// Return parents in this specific order - this is the order we should see in the UI
			const mockFeatureService = createMockFeatureService();
			const getParentFeaturesMock =
				mockFeatureService.getParentFeatures as Mock;
			getParentFeaturesMock.mockResolvedValue([
				mockParent300,
				mockParent100,
				mockParent200,
			]);

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Render with context provider
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={featuresWithOrderedParents}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Wait for parent features to load
			await screen.findByText("PARENT-300: Parent Feature 300");

			// Get all parent headers
			const parentHeaders = screen.getAllByText(
				(content) =>
					content.includes("PARENT-100") ||
					content.includes("PARENT-200") ||
					content.includes("PARENT-300"),
			);

			// Check order of parent headers (should match the order returned by the API)
			expect(parentHeaders[0].textContent).toBe(
				"PARENT-300: Parent Feature 300",
			);
			expect(parentHeaders[1].textContent).toBe(
				"PARENT-100: Parent Feature 100",
			);
			expect(parentHeaders[2].textContent).toBe(
				"PARENT-200: Parent Feature 200",
			);
		});

		it("should handle error when loading parent features", async () => {
			const user = userEvent.setup();

			// Setup mock feature service with error
			const mockFeatureService = createMockFeatureService();
			const getParentFeaturesMock =
				mockFeatureService.getParentFeatures as Mock;
			getParentFeaturesMock.mockRejectedValue(
				new Error("Failed to load parent features"),
			);

			// Spy on console.error
			const consoleErrorSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Render with context provider
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={featuresWithParents}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Wait for API call to complete (it will fail)
			await new Promise((resolve) => setTimeout(resolve, 0));

			// Check that we still see the parent IDs as fallback
			expect(screen.getByText("Parent ID: PARENT-100")).toBeInTheDocument();
			expect(screen.getByText("Parent ID: PARENT-200")).toBeInTheDocument();

			// Check that error was logged
			expect(consoleErrorSpy).toHaveBeenCalledWith(
				"Error fetching parent Features:",
				expect.any(Error),
			);

			// Restore console.error
			consoleErrorSpy.mockRestore();
		});

		// Original tests
		it("should group features by parent when the grouping toggle is activated", async () => {
			const user = userEvent.setup();

			// Setup mock feature service
			const mockFeatureService = createMockFeatureService();
			// Create a promise that won't resolve during the test
			const parentFeaturesPromise = new Promise<Feature[]>(() => {
				// This promise intentionally never resolves to keep the loading state
			});

			const getParentFeaturesMock =
				mockFeatureService.getParentFeatures as Mock;
			getParentFeaturesMock.mockReturnValue(parentFeaturesPromise);

			// Create mock API context
			const mockContext = createMockApiServiceContext({
				featureService: mockFeatureService,
			});

			// Render the component with features that have parents and the mock context
			render(
				<ApiServiceContext.Provider value={mockContext}>
					<FeatureListBase
						features={featuresWithParents}
						contextId={1}
						contextType="project"
						renderTableHeader={() => (
							<tr>
								<th>Name</th>
							</tr>
						)}
						renderTableRow={(feature: IFeature) => (
							<tr data-testid={`feature-${feature.id}`} key={feature.id}>
								<td>{feature.name}</td>
							</tr>
						)}
					/>
				</ApiServiceContext.Provider>,
			);

			// Find and activate the grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Verify parent headers are shown with loading state
			expect(
				screen.getByText((content) =>
					content.includes("Loading parent Feature PARENT-100"),
				),
			).toBeInTheDocument();
			expect(
				screen.getByText((content) =>
					content.includes("Loading parent Feature PARENT-200"),
				),
			).toBeInTheDocument();
			expect(screen.getByText("No Parent")).toBeInTheDocument();

			// Verify features are still visible
			expect(screen.getByText("Feature 1")).toBeInTheDocument();
			expect(screen.getByText("Feature 4")).toBeInTheDocument();
			expect(screen.getByText("Feature 5")).toBeInTheDocument();
		});

		it("should work with hide completed features toggle", async () => {
			const user = userEvent.setup();

			// Render the component
			render(
				<FeatureListBase
					features={featuresWithParents}
					contextId={1}
					contextType="project"
					renderTableHeader={() => (
						<tr>
							<th>Name</th>
						</tr>
					)}
					renderTableRow={(feature: IFeature) => (
						<tr data-testid={`feature-${feature.id}`} key={feature.id}>
							<td>{feature.name}</td>
						</tr>
					)}
				/>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Verify all features are shown initially
			expect(screen.getByText("Feature 1")).toBeInTheDocument();
			expect(screen.getByText("Feature 3")).toBeInTheDocument();
			expect(screen.getByText("Feature 6")).toBeInTheDocument();

			// Activate hide completed features toggle
			const hideCompletedToggle = screen.getByTestId(
				"hide-completed-features-toggle",
			);
			await user.click(hideCompletedToggle);

			// Verify completed features are hidden, but groups still show
			expect(screen.getByText("Feature 1")).toBeInTheDocument(); // Feature with "In Progress" status
			expect(screen.queryByText("Feature 3")).not.toBeInTheDocument(); // Completed feature with parent (hidden)
			expect(screen.queryByText("Feature 6")).not.toBeInTheDocument(); // Completed feature without parent (hidden)

			// Parent headers should still be visible
			expect(screen.getByText("Parent ID: PARENT-100")).toBeInTheDocument();
			expect(screen.getByText("Parent ID: PARENT-200")).toBeInTheDocument();
			expect(screen.getByText("No Parent")).toBeInTheDocument();
		});

		it("should place features without parent in 'No Parent' group at the bottom", async () => {
			const user = userEvent.setup();

			render(
				<FeatureListBase
					features={featuresWithParents}
					contextId={1}
					contextType="project"
					renderTableHeader={() => (
						<tr>
							<th>Name</th>
						</tr>
					)}
					renderTableRow={(feature: IFeature) => (
						<tr data-testid={`feature-${feature.id}`} key={feature.id}>
							<td>{feature.name}</td>
						</tr>
					)}
				/>,
			);

			// Activate grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Get all group headers (finding by text content since we can't query by colSpan directly)
			const groupHeaderElements = screen.getAllByText(
				/^(Parent ID:|No Parent)/,
				{ exact: false },
			);
			const headers = groupHeaderElements.map((el) => el.textContent);

			// Check that "No Parent" is the last group
			expect(headers[headers.length - 1]).toBe("No Parent");
		});

		it("should load and save grouping preference from localStorage", async () => {
			// Set localStorage value before rendering
			mockLocalStorage.lighthouse_group_features_by_parent_project_1 = "true";

			render(
				<FeatureListBase
					features={featuresWithParents}
					contextId={1}
					contextType="project"
					renderTableHeader={() => (
						<tr>
							<th>Name</th>
						</tr>
					)}
					renderTableRow={(feature: IFeature) => (
						<tr key={feature.id}>
							<td>{feature.name}</td>
						</tr>
					)}
				/>,
			);

			// Parent group headers should be visible on initial render due to localStorage setting
			expect(
				screen.getByText("Loading parent Feature PARENT-100..."),
			).toBeInTheDocument();
			expect(
				screen.getByText("Loading parent Feature PARENT-200..."),
			).toBeInTheDocument();
			expect(screen.getByText("No Parent")).toBeInTheDocument();

			// Toggle should be checked
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			// Find the input element and check it's checked state
			const input = within(groupingToggle).getByRole("switch");
			expect(input).toBeChecked();
		});
	});
});

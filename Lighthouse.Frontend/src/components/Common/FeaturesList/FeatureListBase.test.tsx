import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { Feature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
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

		it("should group features by parent when the grouping toggle is activated", async () => {
			const user = userEvent.setup();

			// Render the component with features that have parents
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

			// Find and activate the grouping toggle
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			await user.click(groupingToggle);

			// Verify parent headers are shown
			expect(screen.getByText("Parent ID: PARENT-100")).toBeInTheDocument();
			expect(screen.getByText("Parent ID: PARENT-200")).toBeInTheDocument();
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

			// Check that parent groups are sorted alphabetically
			expect(headers[0]).toBe("Parent ID: PARENT-100");
			expect(headers[1]).toBe("Parent ID: PARENT-200");
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
			expect(screen.getByText("Parent ID: PARENT-100")).toBeInTheDocument();
			expect(screen.getByText("Parent ID: PARENT-200")).toBeInTheDocument();
			expect(screen.getByText("No Parent")).toBeInTheDocument();

			// Toggle should be checked
			const groupingToggle = screen.getByTestId(
				"group-features-by-parent-toggle",
			);
			// Find the input element and check it's checked state
			const input = within(groupingToggle).getByRole("checkbox");
			expect(input).toBeChecked();
		});
	});
});

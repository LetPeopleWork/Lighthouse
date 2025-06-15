import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import type { IFeature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import FeatureListBase from "./FeatureListBase";

describe("FeatureListBase component", () => {
	const createFeature = (
		id: number,
		name: string,
		stateCategory: "ToDo" | "Doing" | "Done" | "Unknown",
	): Feature => {
		const feature = new Feature();
		feature.name = name;
		feature.id = id;
		feature.stateCategory = stateCategory;
		feature.referenceId = `FTR-${id}`;
		feature.remainingWork = { 1: 5 };
		feature.totalWork = { 1: 10 };
		feature.forecasts = [WhenForecast.new(80, new Date())];

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
});

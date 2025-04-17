import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
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
		return new Feature(
			name,
			id,
			`FTR-${id}`,
			"",
			"Unknown",
			new Date(),
			false,
			{},
			{ 1: 5 },
			{ 1: 10 },
			{},
			[new WhenForecast(80, new Date())],
			null,
			stateCategory,
			new Date(),
			new Date(),
			9,
			10,
		);
	};

	const features = [
		createFeature(1, "Feature 1", "ToDo"),
		createFeature(2, "Feature 2", "Doing"),
		createFeature(3, "Feature 3", "Done"),
	];

	it("should render all features initially", () => {
		render(
			<FeatureListBase
				features={features}
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
});

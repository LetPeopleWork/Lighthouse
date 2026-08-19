import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import DependencyDialog from "./DependencyDialog";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "feature") return "Feature";
			if (key === "features") return "Features";
			if (key === "portfolio") return "Portfolio";
			if (key === "portfolios") return "Portfolios";
			return key;
		},
	}),
}));

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	id: 9,
	referenceId: "F-9",
	name: "Warehouse sync",
	state: "In Progress",
	url: "https://tracker.example/F-9",
	source: "TrackerLink",
	notHonouredReason: null,
	isWithheld: false,
	portfolios: [{ id: 1, name: "Platform" }],
	...overrides,
});

const renderDialog = (dependencies: IFeatureDependency[], open = true) =>
	render(
		<DependencyDialog
			featureName="Publish the partner catalogue"
			dependencies={dependencies}
			open={open}
			onClose={() => {}}
		/>,
	);

describe("DependencyDialog", () => {
	it("names each Feature waited on, with its state and where it lives", () => {
		renderDialog([aDependency()]);

		expect(screen.getByText("Warehouse sync")).toBeInTheDocument();
		expect(screen.getByText("In Progress")).toBeInTheDocument();
		expect(screen.getByText("Platform")).toBeInTheDocument();
	});

	it("leads to the record in the work tracking system", () => {
		renderDialog([aDependency()]);

		expect(
			screen.getByRole("link", { name: "Warehouse sync" }),
		).toHaveAttribute("href", "https://tracker.example/F-9");
	});

	it("says why Lighthouse will not act on a dependency", () => {
		renderDialog([aDependency({ notHonouredReason: "InALoop" })]);

		expect(screen.getByTestId("dependency-reason-F-9")).toHaveTextContent(
			/waiting on each other/i,
		);
	});

	it("says nothing at all about a dependency with nothing wrong with it", () => {
		renderDialog([aDependency()]);

		expect(
			screen.queryByTestId("dependency-reason-F-9"),
		).not.toBeInTheDocument();
	});

	// The row is here because the count on the grid counts it. Dropping it would leave the list
	// shorter than the number the reader just clicked, with nothing on screen to explain it.
	it("shows a withheld entry with its reason and nothing else about the Feature", () => {
		renderDialog([
			aDependency({
				referenceId: "",
				name: "",
				url: null,
				portfolios: [],
				isWithheld: true,
				notHonouredReason: "OutsideThisPortfolio",
			}),
		]);

		const row = screen.getByTestId("dependency-withheld");
		expect(row).toHaveTextContent(/you do not have access/i);
		expect(screen.queryByText("Warehouse sync")).not.toBeInTheDocument();
	});

	it("never uses the word that already names something else", () => {
		renderDialog([
			aDependency({ notHonouredReason: "OutsideThisPortfolio" }),
			aDependency({ referenceId: "F-8", id: 8, notHonouredReason: "InALoop" }),
			aDependency({
				referenceId: "F-7",
				id: 7,
				notHonouredReason: "BlockerCannotBeForecast",
			}),
		]);

		expect(document.body.textContent?.toLowerCase()).not.toContain("block");
	});
});

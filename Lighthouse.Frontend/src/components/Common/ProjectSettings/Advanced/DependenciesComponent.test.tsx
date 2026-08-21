import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import DependenciesComponent from "./DependenciesComponent";

const { terms } = vi.hoisted(() => ({
	terms: {
		feature: "Feature",
		features: "Features",
		portfolio: "Portfolio",
	} as Record<string, string>,
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms[key] ?? key,
	}),
}));

const theConnectionsFields: IAdditionalFieldDefinition[] = [
	{ id: 1, displayName: "Waits On", reference: "Custom.WaitsOn" },
	{ id: 2, displayName: "Size", reference: "Custom.Size" },
];

const renderIt = (settings: Partial<IPortfolioSettings> = {}) => {
	const onProjectSettingsChange = vi.fn();

	render(
		<DependenciesComponent
			projectSettings={{ ...createMockProjectSettings(), ...settings }}
			onProjectSettingsChange={onProjectSettingsChange}
			additionalFieldDefinitions={theConnectionsFields}
		/>,
	);

	return onProjectSettingsChange;
};

const openTheGroup = () => {
	fireEvent.click(screen.getByLabelText("toggle"));
};

const openTheFieldList = () => {
	fireEvent.mouseDown(screen.getByRole("combobox"));

	return within(screen.getByRole("listbox"));
};

describe("DependenciesComponent", () => {
	it("offers the fields this connection defines, and nothing else", () => {
		renderIt();
		openTheGroup();

		const fields = openTheFieldList();

		expect(fields.getByText("Waits On")).toBeInTheDocument();
		expect(fields.getByText("Size")).toBeInTheDocument();
	});

	it("reports which field was named", () => {
		const onProjectSettingsChange = renderIt();
		openTheGroup();

		fireEvent.click(openTheFieldList().getByText("Waits On"));

		expect(onProjectSettingsChange).toHaveBeenCalledWith(
			"dependencyOverrideAdditionalFieldDefinitionId",
			1,
		);
	});

	// Naming a field is a declaration, and undoing it has to be as easy as making it, or an instance
	// that tries the setting once can never go back to the tracker's own links.
	it("lets the field be un-named again", () => {
		const onProjectSettingsChange = renderIt({
			dependencyOverrideAdditionalFieldDefinitionId: 1,
		});
		openTheGroup();

		fireEvent.click(openTheFieldList().getByText("None"));

		expect(onProjectSettingsChange).toHaveBeenCalledWith(
			"dependencyOverrideAdditionalFieldDefinitionId",
			null,
		);
	});

	it("starts with the dependencies being acted on", () => {
		renderIt();
		openTheGroup();

		expect(screen.getByLabelText("Ignore Dependencies")).not.toBeChecked();
	});

	it("reports when the dependencies are set aside, and when they are picked back up", () => {
		const onProjectSettingsChange = renderIt();
		openTheGroup();

		fireEvent.click(screen.getByLabelText("Ignore Dependencies"));

		expect(onProjectSettingsChange).toHaveBeenCalledWith(
			"ignoreDependencies",
			true,
		);
	});

	it("shows the switch as on for a Portfolio that has set its dependencies aside", () => {
		renderIt({ ignoreDependencies: true });
		openTheGroup();

		expect(screen.getByLabelText("Ignore Dependencies")).toBeChecked();
	});
});

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

	// Both settings are things most instances never touch, so the group is closed until somebody goes
	// looking for it.
	it("keeps the group closed until it is asked for", () => {
		renderIt();

		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	// The page renders before the settings have arrived. Reaching into them would blank the form with a
	// crash rather than show it empty.
	it("renders before there are any settings to show", () => {
		render(
			<DependenciesComponent
				projectSettings={null}
				onProjectSettingsChange={vi.fn()}
				additionalFieldDefinitions={theConnectionsFields}
			/>,
		);
		openTheGroup();

		expect(screen.getByLabelText("Ignore Dependencies")).not.toBeChecked();
	});

	// Comma or semicolon is the whole contract of the field, and it is a field somebody fills in by
	// hand. Saying it anywhere other than beside the control means saying it in documentation nobody
	// opens while they are typing.
	it("says what the field is expected to contain, beside the field", () => {
		renderIt();
		openTheGroup();

		expect(
			screen.getByText(
				"Read what each Feature waits on from this field, separated by commas or semicolons, instead of from the links in your work tracking system.",
			),
		).toBeInTheDocument();
	});

	// Ignoring is not hiding, and a reader who thinks it deletes their dependencies will not use it.
	it("says the dependencies stay visible when they are set aside", () => {
		renderIt();
		openTheGroup();

		expect(
			screen.getByText(
				"Ignore Dependencies (Features still show what they wait on, but this Portfolio does not act on any of it)",
			),
		).toBeInTheDocument();
	});

	it("shows the switch as on for a Portfolio that has set its dependencies aside", () => {
		renderIt({ ignoreDependencies: true });
		openTheGroup();

		expect(screen.getByLabelText("Ignore Dependencies")).toBeChecked();
	});
});

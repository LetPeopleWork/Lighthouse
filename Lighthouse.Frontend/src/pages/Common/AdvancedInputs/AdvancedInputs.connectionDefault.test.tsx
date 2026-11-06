import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

/**
 * A Team on an Azure DevOps connection that follows the connection's Parent sources reads the default in words,
 * with Azure DevOps' own parent named as such, and has no list of its own to edit until it picks Set here.
 */

const TheFields: IAdditionalFieldDefinition[] = [
	{ id: 11, displayName: "Caused by", reference: "Custom.CausedBy" },
	{ id: 12, displayName: "Parent Link", reference: "Custom.ParentLink" },
];

const CausedByThenOwnParent: ParentSourceEntry[] = [
	{ kind: "AdditionalField", additionalFieldDefinitionId: 11 },
	{ kind: "TrackerParent" },
];

const aTeamFollowing = (parentSourcesDefault: ParentSourceEntry[]) =>
	Object.assign(createMockTeamSettings(), {
		parentOverrideSource: "ConnectionDefault",
		parentSources: parentSourcesDefault,
		parentSourcesDefault,
		workTrackingSystem: "AzureDevOps",
	}) as IBaseSettings;

const givenTheTeamsSettingsPage = (settings: IBaseSettings) => {
	const onSettingsChange = vi.fn();
	render(
		<AdvancedInputsComponent
			settings={settings}
			onSettingsChange={onSettingsChange}
			parentOverrideDefault="parentOverrideForTeams"
			additionalFieldDefinitions={TheFields}
		/>,
	);
	fireEvent.click(screen.getByLabelText("toggle"));
	return onSettingsChange;
};

describe("a Team that follows the connection's Parent sources", () => {
	it("on Azure DevOps shows the followed list in words, with nothing to edit", () => {
		givenTheTeamsSettingsPage(aTeamFollowing(CausedByThenOwnParent));

		expect(
			screen.getByRole("radio", {
				name: "Connection default: Caused by, then Parent (Azure DevOps' own)",
			}),
		).toBeChecked();
		expect(screen.queryAllByRole("listitem")).toHaveLength(0);
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("on Azure DevOps starts from the followed list once Set here is picked", () => {
		const onSettingsChange = givenTheTeamsSettingsPage(
			aTeamFollowing(CausedByThenOwnParent),
		);

		fireEvent.click(screen.getByRole("radio", { name: "Set here" }));

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"Own",
		);
		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentSources",
			CausedByThenOwnParent,
		);
	});

	it("before the settings arrive there is no tracker to offer a parent setting for", () => {
		render(
			<AdvancedInputsComponent
				settings={null}
				onSettingsChange={vi.fn()}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(screen.queryByText("Parent sources")).not.toBeInTheDocument();
		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});
});

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

const TheFields: IAdditionalFieldDefinition[] = [
	{ id: 1, displayName: "Is caused by", reference: "customfield_10500" },
	{ id: 2, displayName: "Program Ref", reference: "customfield_10014" },
];

const aTeamOn = (tracker: string) =>
	Object.assign(createMockTeamSettings(), {
		workTrackingSystem: tracker,
		parentOverrideSource: "Own",
		parentSources: [
			{ kind: "TrackerParent" },
			{ kind: "AdditionalField", additionalFieldDefinitionId: 2 },
		],
	}) as IBaseSettings;

const openTheSettingsPageOf = (settings: IBaseSettings) => {
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

// Skipped until Story 6501 builds the parent sources list; enable one test at a time.
describe("a Team's Advanced Configuration", () => {
	// biome-ignore lint/suspicious/noSkippedTests: Story 6501 builds this; enabled in DELIVER
	it.skip("shows its Parent sources on Jira and hands the whole new list on when one is moved up", () => {
		const onSettingsChange = openTheSettingsPageOf(aTeamOn("Jira"));

		expect(screen.getAllByText("Parent sources")).toHaveLength(1);
		expect(screen.getAllByRole("listitem")).toHaveLength(2);
		expect(screen.queryAllByText("Parent Override Field")).toHaveLength(0);

		fireEvent.click(screen.getByLabelText("Move Program Ref up"));

		expect(onSettingsChange).toHaveBeenCalledWith("parentSources", [
			{ kind: "AdditionalField", additionalFieldDefinitionId: 2 },
			{ kind: "TrackerParent" },
		]);
	});

	// biome-ignore lint/suspicious/noSkippedTests: Story 6501 builds this; enabled in DELIVER
	it.skip("shows a Team that follows an unset connection default the tracker's own parent as its one source", () => {
		openTheSettingsPageOf(
			Object.assign(aTeamOn("Jira"), {
				parentOverrideSource: "ConnectionDefault",
				parentSources: undefined,
				parentSourcesDefault: [{ kind: "TrackerParent" }],
			}) as IBaseSettings,
		);

		expect(
			screen.getByText(/Connection default.*Parent \(Jira's own\)/),
		).toBeInTheDocument();
		expect(screen.getAllByText("Parent sources")).toHaveLength(1);
	});

	// biome-ignore lint/suspicious/noSkippedTests: Story 6501 builds this; enabled in DELIVER
	it.skip("shows no Parent sources on a tracker that does not read them", () => {
		openTheSettingsPageOf(aTeamOn("Linear"));

		expect(screen.queryByText("Parent sources")).not.toBeInTheDocument();
		expect(screen.queryAllByText("Parent Override Field")).toHaveLength(0);
	});
});

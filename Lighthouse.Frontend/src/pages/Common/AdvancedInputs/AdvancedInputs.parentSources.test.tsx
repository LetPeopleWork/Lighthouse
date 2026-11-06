import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { parentSourcesAreOffered } from "../../../components/Common/ConnectionDefault/parentSources";
import { useModifySettings } from "../../../hooks/useModifySettings";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
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

const aConnection = (
	workTrackingSystem: WorkTrackingSystemType,
	defaults: IWorkTrackingSystemConnection["defaults"],
): IWorkTrackingSystemConnection => ({
	id: 2,
	name: "The chosen connection",
	workTrackingSystem,
	options: [],
	authenticationMethodKey: "jira.cloud",
	additionalFieldDefinitions: TheFields,
	writeBackMappingDefinitions: [],
	workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
	defaults,
});

const aFollower = (parentSourcesDefault: IBaseSettings["parentSources"]) =>
	Object.assign(aTeamOn("Jira"), {
		parentOverrideSource: "ConnectionDefault",
		parentSources: parentSourcesDefault ?? [{ kind: "TrackerParent" }],
		parentSourcesDefault,
	}) as IBaseSettings;

const IsCausedByThenOwnParent: ParentSourceEntry[] = [
	{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
	{ kind: "TrackerParent" },
];

describe("a Team's Advanced Configuration", () => {
	it("shows its Parent sources on Jira and hands the whole new list on when one is moved up", () => {
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

	it("shows a Team that follows an unset connection default the tracker's own parent as its one source", () => {
		openTheSettingsPageOf(
			Object.assign(aTeamOn("Jira"), {
				parentOverrideSource: "ConnectionDefault",
				parentSources: undefined,
				parentSourcesDefault: [{ kind: "TrackerParent" }],
			}) as IBaseSettings,
		);

		expect(
			screen.getByText(/^Connection default: Parent \(Jira's own\)$/),
		).toBeInTheDocument();
		expect(screen.getAllByText("Parent sources")).toHaveLength(1);
	});

	it("shows no Parent sources on a tracker that does not read them", () => {
		openTheSettingsPageOf(aTeamOn("Linear"));

		expect(screen.queryByText("Parent sources")).not.toBeInTheDocument();
		expect(screen.queryAllByText("Parent Override Field")).toHaveLength(0);
	});

	it("shows the Parent sources list on Azure DevOps, naming Azure DevOps' own parent as such", () => {
		openTheSettingsPageOf(aTeamOn("AzureDevOps"));

		expect(screen.getAllByText("Parent sources")).toHaveLength(1);
		expect(screen.getAllByRole("listitem")).toHaveLength(2);
		expect(screen.getByText("Parent (Azure DevOps' own)")).toBeInTheDocument();
		expect(
			screen.queryByRole("combobox", { name: "Parent sources" }),
		).not.toBeInTheDocument();
	});

	it("tells a Portfolio on Azure DevOps what leaving out Azure DevOps' own parent costs its Features", () => {
		render(
			<AdvancedInputsComponent
				settings={Object.assign(
					aFollower([
						{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
					]),
					{ workTrackingSystem: "AzureDevOps" },
				)}
				onSettingsChange={vi.fn()}
				parentOverrideDefault="parentOverrideForPortfolios"
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			screen.getByText(
				"Not in the list: Parent (Azure DevOps' own). Features whose only parent is Azure DevOps' own parent get no parent.",
			),
		).toBeInTheDocument();
	});

	it("reads the tracker off the connection for a Team not saved yet", () => {
		render(
			<AdvancedInputsComponent
				settings={createMockTeamSettings()}
				onSettingsChange={vi.fn()}
				connection={aConnection("Jira", {})}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(screen.getByText("Parent sources")).toBeInTheDocument();
	});

	it("offers no Connection default while the connection holds no list and the Team does not follow it", () => {
		openTheSettingsPageOf(aTeamOn("Jira"));

		expect(screen.queryByRole("radio")).not.toBeInTheDocument();
	});

	it("lets a Team that keeps its own list follow the connection's default", () => {
		const onSettingsChange = openTheSettingsPageOf(
			Object.assign(aTeamOn("Jira"), {
				parentSourcesDefault: IsCausedByThenOwnParent,
			}) as IBaseSettings,
		);

		expect(screen.getByRole("radio", { name: "Set here" })).toBeChecked();
		expect(screen.getAllByRole("listitem")).toHaveLength(2);

		fireEvent.click(
			screen.getByRole("radio", {
				name: "Connection default: Is caused by, then Parent (Jira's own)",
			}),
		);

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"ConnectionDefault",
		);
	});

	it("shows a follower the default in words, with nothing to edit", () => {
		openTheSettingsPageOf(aFollower(IsCausedByThenOwnParent));

		expect(
			screen.getByRole("radio", {
				name: "Connection default: Is caused by, then Parent (Jira's own)",
			}),
		).toBeChecked();
		expect(screen.queryAllByRole("listitem")).toHaveLength(0);
		expect(
			screen.queryByRole("combobox", { name: "Add a source" }),
		).not.toBeInTheDocument();
	});

	it("judges the consequence line on the list a follower reads", () => {
		openTheSettingsPageOf(
			aFollower([{ kind: "AdditionalField", additionalFieldDefinitionId: 1 }]),
		);

		expect(
			screen.getByText(/^Not in the list: Parent \(Jira's own\)\./),
		).toBeInTheDocument();
	});

	it("tells a Portfolio that follows a default without the own Parent what that costs its Features", () => {
		render(
			<AdvancedInputsComponent
				settings={aFollower([
					{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
				])}
				onSettingsChange={vi.fn()}
				parentOverrideDefault="parentOverrideForPortfolios"
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			screen.getByText(
				"Not in the list: Parent (Jira's own). Features whose only parent is Jira's own parent get no parent.",
			),
		).toBeInTheDocument();
		expect(screen.queryAllByRole("listitem")).toHaveLength(0);
	});

	it("starts a follower that picks Set here from the list it was following", () => {
		const onSettingsChange = openTheSettingsPageOf(
			aFollower(IsCausedByThenOwnParent),
		);

		fireEvent.click(screen.getByRole("radio", { name: "Set here" }));

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"Own",
		);
		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentSources",
			IsCausedByThenOwnParent,
		);
	});

	it("keeps the way back for a follower of an unset default that picks Set here", () => {
		const ThePageHoldingItsEdits = () => {
			const [settings, setSettings] = useState(aFollower(null));
			return (
				<AdvancedInputsComponent
					settings={settings}
					onSettingsChange={(key, value) =>
						setSettings((current) => ({ ...current, [key]: value }))
					}
					parentOverrideDefault="parentOverrideForTeams"
					additionalFieldDefinitions={TheFields}
				/>
			);
		};
		render(<ThePageHoldingItsEdits />);
		fireEvent.click(screen.getByLabelText("toggle"));
		const theDefault = {
			name: "Connection default: Parent (Jira's own)",
		};

		fireEvent.click(screen.getByRole("radio", { name: "Set here" }));

		expect(screen.getByRole("radio", { name: "Set here" })).toBeChecked();
		expect(screen.getAllByRole("listitem")).toHaveLength(1);
		expect(screen.getByRole("radio", theDefault)).not.toBeChecked();

		fireEvent.click(screen.getByRole("radio", theDefault));

		expect(screen.getByRole("radio", theDefault)).toBeChecked();
		expect(screen.queryAllByRole("listitem")).toHaveLength(0);
	});

	it("once another Jira connection is chosen, follows that connection's default", () => {
		render(
			<AdvancedInputsComponent
				settings={aFollower([{ kind: "TrackerParent" }])}
				onSettingsChange={vi.fn()}
				connection={aConnection("Jira", { parentOverrideForTeams: 2 })}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			screen.getByText(
				/^Connection default: Program Ref, then Parent \(Jira's own\)$/,
			),
		).toBeInTheDocument();
	});

	it("keeps the last of two quick reorders when the first is still being saved", async () => {
		let finishTheFirstSave = () => {};
		const saveSettings = vi
			.fn()
			.mockImplementationOnce(
				() =>
					new Promise<undefined>((resolve) => {
						finishTheFirstSave = () => resolve(undefined);
					}),
			)
			.mockResolvedValue(undefined);
		const threeSources = Object.assign(aTeamOn("Jira"), {
			parentSources: [
				{ kind: "TrackerParent" },
				{ kind: "AdditionalField", additionalFieldDefinitionId: 2 },
				{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
			],
		}) as unknown as ITeamSettings;

		const ThePage = () => {
			const { settings, updateSettings } = useModifySettings<ITeamSettings>({
				getWorkTrackingSystems: async () => [],
				getSettings: async () => threeSources,
				saveSettings,
				validateSettings: async () => true,
				modifyDefaultSettings: false,
				validateForm: () => [],
				getSchemaForSystem: () => null,
				autoSave: { enabled: true, canSave: true, debounceMs: 1 },
				readsParentSources: parentSourcesAreOffered,
			});
			return (
				<AdvancedInputsComponent
					settings={settings}
					onSettingsChange={updateSettings}
					parentOverrideDefault="parentOverrideForTeams"
					additionalFieldDefinitions={TheFields}
				/>
			);
		};
		render(<ThePage />);
		fireEvent.click(await screen.findByLabelText("toggle"));

		fireEvent.click(await screen.findByLabelText("Move Program Ref up"));
		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1));
		fireEvent.click(screen.getByLabelText("Move Is caused by up"));
		await new Promise((resolve) => setTimeout(resolve, 5));
		await act(async () => finishTheFirstSave());

		await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(2));
		expect(saveSettings.mock.lastCall?.[0].parentSources).toEqual([
			{ kind: "AdditionalField", additionalFieldDefinitionId: 2 },
			{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
			{ kind: "TrackerParent" },
		]);
	});
});

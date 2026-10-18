import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

/**
 * A Team on "Jira DC – Payments" whose Parent Override Field follows the connection's default. Its
 * settings page shows which field that is, as the first entry of the dropdown, "Connection default (Caused
 * by)", with "Set on the Jira connection." beneath it, and "Connection default (None)" while the default
 * names nothing. A Team admin who cannot read the connection still sees the name, because it travels on
 * the Team's own settings. Picking any other entry, None included, makes the value the Team's own. Only
 * Jira and Azure DevOps connections hold this default, so no other tracker offers the entry.
 */

const CausedBy = "Caused by";

const TheFields: IAdditionalFieldDefinition[] = [
	{ id: 11, displayName: CausedBy, reference: "customfield_10500" },
	{ id: 12, displayName: "Parent Link", reference: "customfield_10014" },
];

type Source = "ConnectionDefault" | "Own";

const aTeam = ({
	source,
	own = null,
	tracker = "Jira",
	defaultName = CausedBy,
}: {
	source: Source;
	own?: number | null;
	tracker?: string;
	defaultName?: string | null;
}) =>
	Object.assign(createMockTeamSettings(), {
		parentOverrideAdditionalFieldDefinitionId:
			source === "Own" || defaultName === null ? own : 11,
		parentOverrideSource: source,
		parentOverrideDefaultName: defaultName,
		workTrackingSystem: tracker,
	}) as IBaseSettings;

const givenTheTeamsSettingsPage = (
	settings: IBaseSettings,
	fields: IAdditionalFieldDefinition[] = TheFields,
) => {
	const onSettingsChange = vi.fn();
	render(
		<AdvancedInputsComponent
			settings={settings}
			onSettingsChange={onSettingsChange}
			additionalFieldDefinitions={fields}
		/>,
	);
	fireEvent.click(screen.getByLabelText("toggle"));
	return onSettingsChange;
};

const theDropdown = () =>
	screen.getByRole("combobox", { name: "Parent Override Field" });

const theEntries = () => {
	fireEvent.mouseDown(theDropdown());
	return within(screen.getByRole("listbox"))
		.getAllByRole("option")
		.map((entry) => entry.textContent);
};

const whenTheAdminPicks = (entry: string) => {
	fireEvent.mouseDown(theDropdown());
	fireEvent.click(
		within(screen.getByRole("listbox")).getByRole("option", { name: entry }),
	);
};

describe("a Team that follows the connection's Parent Override Field", () => {
	// @driving_port @us-04 @ac-4.6 @contract-shape:pure-function
	it("shows the followed field as the first entry, with where it is set", () => {
		givenTheTeamsSettingsPage(aTeam({ source: "ConnectionDefault" }));

		expect(theDropdown()).toHaveTextContent("Connection default (Caused by)");
		expect(screen.getByText("Set on the Jira connection.")).toBeInTheDocument();
		expect(theEntries()).toEqual([
			"Connection default (Caused by)",
			"None",
			CausedBy,
			"Parent Link",
		]);
	});

	// @us-04 @ac-4.6 @contract-shape:pure-function
	it("names the Azure DevOps connection the default is set on", () => {
		givenTheTeamsSettingsPage(
			aTeam({ source: "ConnectionDefault", tracker: "AzureDevOps" }),
		);

		expect(
			screen.getByText("Set on the Azure DevOps connection."),
		).toBeInTheDocument();
	});

	// @edge @us-04 @ac-4.6 @contract-shape:pure-function
	it.each(["Linear", "Csv", "ServiceNow"])(
		"offers no connection default on a %s connection, which holds none",
		(tracker) => {
			givenTheTeamsSettingsPage(
				aTeam({ source: "ConnectionDefault", tracker, defaultName: null }),
			);

			expect(screen.queryByText(/^Set on the /)).not.toBeInTheDocument();
			expect(theEntries()).toEqual(["None", CausedBy, "Parent Link"]);
		},
	);

	// @edge @us-04 @ac-4.6 @contract-shape:pure-function
	it("following a default that names nothing reads Connection default (None), with where it is set", () => {
		givenTheTeamsSettingsPage(
			aTeam({ source: "ConnectionDefault", defaultName: null }),
		);

		expect(theDropdown()).toHaveTextContent("Connection default (None)");
		expect(screen.getByText("Set on the Jira connection.")).toBeInTheDocument();
		expect(theEntries()).toEqual([
			"Connection default (None)",
			"None",
			CausedBy,
			"Parent Link",
		]);
	});

	// @us-04 @ac-4.6 @contract-shape:bounded-change
	it("a Team that chose None can go back to following a default that names nothing", () => {
		const onSettingsChange = givenTheTeamsSettingsPage(
			aTeam({ source: "Own", defaultName: null }),
		);

		expect(theDropdown()).toHaveTextContent("None");
		expect(screen.queryByText(/^Set on the /)).not.toBeInTheDocument();
		whenTheAdminPicks("Connection default (None)");

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"ConnectionDefault",
		);
	});

	// @edge @contract-shape:pure-function
	it("before the settings arrive the dropdown offers no connection default and chooses nothing", () => {
		render(
			<AdvancedInputsComponent
				settings={null}
				onSettingsChange={vi.fn()}
				additionalFieldDefinitions={TheFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(theEntries()).toEqual(["None", CausedBy, "Parent Link"]);
		expect(
			within(screen.getByRole("listbox")).getByRole("option", {
				name: "None",
				selected: true,
			}),
		).toBeInTheDocument();
	});

	// @edge @us-04 @ac-4.6 @contract-shape:pure-function
	it("shows the followed field to a Team admin who cannot read the connection's fields", () => {
		givenTheTeamsSettingsPage(aTeam({ source: "ConnectionDefault" }), []);

		expect(theDropdown()).toHaveTextContent("Connection default (Caused by)");
	});

	// @error @us-04 @ac-4.3 @ac-4.6 @contract-shape:bounded-change
	it("choosing None makes None the Team's own choice", () => {
		const onSettingsChange = givenTheTeamsSettingsPage(
			aTeam({ source: "ConnectionDefault" }),
		);

		whenTheAdminPicks("None");

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"Own",
		);
		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideAdditionalFieldDefinitionId",
			null,
		);
	});

	// @us-04 @ac-4.6 @contract-shape:bounded-change
	it("choosing a field makes it the Team's own", () => {
		const onSettingsChange = givenTheTeamsSettingsPage(
			aTeam({ source: "ConnectionDefault" }),
		);

		whenTheAdminPicks("Parent Link");

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"Own",
		);
		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideAdditionalFieldDefinitionId",
			12,
		);
	});

	// @us-04 @ac-4.6 @contract-shape:bounded-change
	it("a Team that chose None can go back to following the connection", () => {
		const onSettingsChange = givenTheTeamsSettingsPage(
			aTeam({ source: "Own" }),
		);

		expect(theDropdown()).toHaveTextContent("None");
		whenTheAdminPicks("Connection default (Caused by)");

		expect(onSettingsChange).toHaveBeenCalledWith(
			"parentOverrideSource",
			"ConnectionDefault",
		);
	});
});

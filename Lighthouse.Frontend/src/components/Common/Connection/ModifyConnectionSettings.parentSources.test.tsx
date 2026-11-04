import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import {
	type ConnectionDefaultName,
	type IConnectionDefaults,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { renderWithTerms } from "../../../tests/FieldTipsTestKit";
import ModifyConnectionSettings from "./ModifyConnectionSettings";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
	}),
}));

const EveryDefault: ConnectionDefaultName[] = [
	"parentOverrideForTeams",
	"estimationForTeams",
	"parentOverrideForPortfolios",
	"dependencyFieldForPortfolios",
	"sizeEstimateFieldForPortfolios",
	"featureOwnerFieldForPortfolios",
	"estimationForPortfolios",
];

const aJiraConnectionWhoseTeamsDefaultListsTwoSources = () =>
	new WorkTrackingSystemConnection({
		id: 7,
		name: "Jira DC – Payments",
		workTrackingSystem: "Jira",
		authenticationMethodKey: "none",
		availableAuthenticationMethods: [
			{ key: "none", displayName: "None", options: [] },
		],
		options: [],
		additionalFieldDefinitions: [
			{ id: 11, displayName: "Is caused by", reference: "customfield_10500" },
			{ id: 12, displayName: "Program Ref", reference: "customfield_10014" },
		],
		usedBy: { teams: 11, portfolios: 0 },
		defaults: {
			parentSourcesForTeams: [
				{ kind: "AdditionalField", additionalFieldDefinitionId: 11 },
				{ kind: "AdditionalField", additionalFieldDefinitionId: 12 },
			],
		} as unknown as IConnectionDefaults,
		supportedDefaults: EveryDefault,
	});

// Skipped until Story 6501 builds the parent sources list on the connection page.
describe("the connection's defaults for its Teams", () => {
	// biome-ignore lint/suspicious/noSkippedTests: Story 6501 builds this; enabled in DELIVER
	it.skip("show the Parent sources as the same ordered list, and save the new order when one is moved up", async () => {
		const connection = aJiraConnectionWhoseTeamsDefaultListsTwoSources();
		const saveConnectionSettings = vi.fn().mockResolvedValue(undefined);
		const user = userEvent.setup();
		await renderWithTerms(
			<MemoryRouter>
				<ModifyConnectionSettings
					title="Update Connection"
					getSupportedSystems={vi.fn().mockResolvedValue([connection])}
					getConnectionSettings={vi.fn().mockResolvedValue(connection)}
					saveConnectionSettings={saveConnectionSettings}
					validateConnectionSettings={vi.fn().mockResolvedValue(true)}
				/>
			</MemoryRouter>,
			{},
		);
		await screen.findByDisplayValue("Jira DC – Payments");

		const teams = within(
			screen.getByRole("region", { name: /^Defaults for / }),
		).getByRole("group", { name: "Teams" });
		expect(within(teams).getByText("Parent sources")).toBeInTheDocument();
		expect(
			within(teams).queryByText("Parent Override Field"),
		).not.toBeInTheDocument();

		await user.click(within(teams).getByLabelText("Move Program Ref up"));
		await user.click(screen.getByRole("button", { name: /Save/i }));

		expect(saveConnectionSettings).toHaveBeenCalledWith(
			expect.objectContaining({
				defaults: expect.objectContaining({
					parentSourcesForTeams: [
						{ kind: "AdditionalField", additionalFieldDefinitionId: 12 },
						{ kind: "AdditionalField", additionalFieldDefinitionId: 11 },
					],
				}),
			}),
		);
	});
});

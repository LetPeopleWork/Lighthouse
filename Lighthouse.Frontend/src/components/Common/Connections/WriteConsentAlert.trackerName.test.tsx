import { screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	type AuthenticationMethodKey,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ConnectionsSettings from "../../../pages/Settings/Connections/ConnectionsSettings";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { renderWithTerms } from "../../../tests/FieldTipsTestKit";
import { createMockWorkTrackingSystemService } from "../../../tests/MockApiServiceProvider";

/**
 * Priya, System Admin, has a Jira OAuth connection and an Azure DevOps OAuth connection, both set up for
 * write-back, both signed in with read rights only. Each connection's warning on the connections list says
 * which tracker Lighthouse can only read from, and keeps its "Sign in again" button.
 */

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: vi.fn() }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
	}),
}));

const copyFor = (tracker: string) =>
	`Write-back needs more rights. Lighthouse can only read from ${tracker} with this sign-in. Sign in again to allow Lighthouse to write the mapped fields.`;

const readOnlyOAuthConnection = (
	id: number,
	name: string,
	workTrackingSystem: WorkTrackingSystemType,
	authenticationMethodKey: AuthenticationMethodKey,
) =>
	new WorkTrackingSystemConnection({
		id,
		name,
		workTrackingSystem,
		authenticationMethodKey,
		options: [],
		grantedRights: "Read",
		needsWriteConsent: true,
	});

const givenPriyaOpensTheConnectionsList = async (
	...connections: WorkTrackingSystemConnection[]
) => {
	const service: IWorkTrackingSystemService =
		createMockWorkTrackingSystemService();
	vi.mocked(service.getConfiguredWorkTrackingSystems).mockResolvedValue(
		connections,
	);
	const oauthService: IOAuthService = {
		initiateConnect: vi.fn(),
		disconnect: vi.fn(),
	};
	await renderWithTerms(
		<MemoryRouter>
			<ConnectionsSettings />
		</MemoryRouter>,
		{},
		{ workTrackingSystemService: service, oauthService },
	);
};

describe("The write-consent warning names the tracker of the connection it belongs to", () => {
	afterEach(() => {
		vi.clearAllMocks();
	});

	it("tells a Jira connection it can only read from Jira, and an Azure DevOps connection it can only read from Azure DevOps, each keeping Sign in again", async () => {
		await givenPriyaOpensTheConnectionsList(
			readOnlyOAuthConnection(
				1,
				"Contoso Jira",
				"Jira",
				"jira.oauth" as AuthenticationMethodKey,
			),
			readOnlyOAuthConnection(
				2,
				"Fabrikam ADO",
				"AzureDevOps",
				"ado.oauth" as AuthenticationMethodKey,
			),
		);
		await screen.findByText("Contoso Jira");

		const warnings = screen
			.getAllByRole("alert")
			.filter((alert) =>
				alert.textContent?.includes("Write-back needs more rights."),
			);
		expect(
			warnings,
			"WHAT: expected one write-consent warning per read-only connection. WHY: both connections have write-back set up on a sign-in that may only read. HOW: render WriteConsentAlert for every connection whose needsWriteConsent is true.",
		).toHaveLength(2);

		const [jiraWarning, adoWarning] = warnings;
		expect(
			jiraWarning,
			"WHAT: the Jira connection's warning does not name Jira. WHY: the admin must learn which tracker the sign-in can only read from. HOW: build the warning copy from the connection's own tracker name.",
		).toHaveTextContent(copyFor("Jira"));
		expect(
			adoWarning,
			"WHAT: the Azure DevOps connection's warning does not name Azure DevOps. WHY: the admin must learn which tracker the sign-in can only read from. HOW: build the warning copy from the connection's own tracker name.",
		).toHaveTextContent(copyFor("Azure DevOps"));
		for (const warning of [jiraWarning, adoWarning]) {
			expect(
				within(warning).getByRole("button", { name: /^Sign in again$/ }),
				"WHAT: a warning lost its Sign in again button. WHY: it is how the admin gives write consent. HOW: keep the action on every write-consent warning.",
			).toBeInTheDocument();
		}
	});
});

import { screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	type AuthenticationMethodKey,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import { renderWithTerms } from "../../../tests/FieldTipsTestKit";
import WriteConsentAlert from "./WriteConsentAlert";

/**
 * Priya, System Admin, has a Jira OAuth connection and an Azure DevOps OAuth connection, both set up for
 * write-back, both signed in with read rights only. Each connection's warning says which tracker Lighthouse
 * can only read from, and keeps its "Sign in again" button.
 */

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: vi.fn() }),
}));

const copyFor = (tracker: string) =>
	`Write-back needs more rights. Lighthouse can only read from ${tracker} with this sign-in. Sign in again to allow Lighthouse to write the mapped fields.`;

const readOnlyOAuthConnection = (
	workTrackingSystem: WorkTrackingSystemType,
	authenticationMethodKey: AuthenticationMethodKey,
) =>
	new WorkTrackingSystemConnection({
		id: 1,
		name: "Read-only connection",
		workTrackingSystem,
		authenticationMethodKey,
		options: [],
		grantedRights: "Read",
		needsWriteConsent: true,
	});

const givenPriyaSeesTheWarningOf = async (
	connection: WorkTrackingSystemConnection,
) => {
	const oauthService: IOAuthService = {
		initiateConnect: vi.fn(),
		disconnect: vi.fn(),
	};
	await renderWithTerms(
		<WriteConsentAlert connection={connection} />,
		{},
		{ oauthService },
	);
	return screen.getByRole("alert");
};

const thenTheWarningNamesOnly = (
	warning: HTMLElement,
	tracker: string,
	otherTracker: string,
) => {
	expect(
		warning,
		`WHAT: the warning does not say Lighthouse can only read from ${tracker}. WHY: the admin must learn which tracker the sign-in can only read from. HOW: build the warning copy from the connection's own tracker name.`,
	).toHaveTextContent(copyFor(tracker));
	expect(
		warning,
		`WHAT: the warning also carries the ${otherTracker} sentence. WHY: a warning for ${tracker} must never name the other tracker. HOW: build the copy from the connection's tracker only.`,
	).not.toHaveTextContent(copyFor(otherTracker));
	expect(
		screen.getByRole("button", { name: /^Sign in again$/ }),
		"WHAT: the warning lost its Sign in again button. WHY: it is how the admin gives write consent. HOW: keep the action on every write-consent warning.",
	).toBeInTheDocument();
};

describe("The write-consent warning names the tracker of the connection it belongs to", () => {
	afterEach(() => {
		vi.clearAllMocks();
	});

	it("tells a Jira connection it can only read from Jira, keeping Sign in again", async () => {
		const warning = await givenPriyaSeesTheWarningOf(
			readOnlyOAuthConnection("Jira", "jira.oauth" as AuthenticationMethodKey),
		);

		thenTheWarningNamesOnly(warning, "Jira", "Azure DevOps");
	});

	it("tells an Azure DevOps connection it can only read from Azure DevOps, keeping Sign in again", async () => {
		const warning = await givenPriyaSeesTheWarningOf(
			readOnlyOAuthConnection(
				"AzureDevOps",
				"ado.oauth" as AuthenticationMethodKey,
			),
		);

		thenTheWarningNamesOnly(warning, "Azure DevOps", "Jira");
	});
});

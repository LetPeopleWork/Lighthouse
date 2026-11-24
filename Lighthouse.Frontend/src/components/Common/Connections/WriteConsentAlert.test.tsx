import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../../models/WorkTracking/WriteBackMappingDefinition";
import ConnectionsSettings from "../../../pages/Settings/Connections/ConnectionsSettings";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { renderWithTerms } from "../../../tests/FieldTipsTestKit";
import { createMockWorkTrackingSystemService } from "../../../tests/MockApiServiceProvider";
import ModifyConnectionSettings from "../Connection/ModifyConnectionSettings";

/**
 * Priya, System Admin, set up write-back on "Fabrikam ADO", an Azure DevOps OAuth connection whose sign-in
 * may only read work items. The connection's settings say which rights the sign-in holds, and a warning with
 * "Sign in again" sits above the write-back mappings and beside the connection on the connections list, so
 * she can give write consent without disconnecting first.
 */

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
	}),
}));

const FABRIKAM = "Fabrikam ADO";
const AUTHORIZATION_URL =
	"https://login.example.test/authorize?scope=vso.work_write";
const WRITE_CONSENT_COPY =
	"Write-back needs more rights. Lighthouse can only read from Azure DevOps with this sign-in. Sign in again to allow Lighthouse to write the mapped fields.";
const READ_ONLY_RIGHTS = "Rights: Read work items";
const READ_AND_WRITE_RIGHTS = "Rights: Read and write work items";

type SignIn =
	| { grantedRights: "Read"; needsWriteConsent: true }
	| { grantedRights: "ReadAndWrite"; needsWriteConsent: false };

const MAY_ONLY_READ: SignIn = {
	grantedRights: "Read",
	needsWriteConsent: true,
};
const MAY_READ_AND_WRITE: SignIn = {
	grantedRights: "ReadAndWrite",
	needsWriteConsent: false,
};

const fabrikamWithWriteBack = (signIn: SignIn, id = 7, name = FABRIKAM) =>
	Object.assign(
		new WorkTrackingSystemConnection({
			id,
			name,
			workTrackingSystem: "AzureDevOps",
			authenticationMethodKey: "ado.oauth",
			availableAuthenticationMethods: [
				{
					key: "ado.oauth",
					displayName: "Azure DevOps (OAuth)",
					options: [
						{
							key: "url",
							displayName: "Organization URL",
							isSecret: false,
							isOptional: false,
						},
					],
				},
			],
			options: [
				{
					key: "url",
					value: "https://dev.azure.com/fabrikam",
					isSecret: false,
					isOptional: false,
				},
			],
			additionalFieldDefinitions: [
				{ id: 11, displayName: "Forecast Date", reference: "Custom.Forecast" },
			],
			writeBackMappingDefinitions: [
				{
					id: 1,
					valueSource: WriteBackValueSource.ForecastPercentile85,
					appliesTo: WriteBackAppliesTo.Portfolio,
					additionalFieldDefinitionId: 11,
					targetValueType: WriteBackTargetValueType.Date,
				},
			],
			usedBy: { teams: 0, portfolios: 0 },
			defaults: {},
			supportedDefaults: [],
		}),
		signIn,
	);

const anOAuthServiceThatSignsIn = (): IOAuthService => ({
	initiateConnect: vi
		.fn()
		.mockResolvedValue({ authorizationUrl: AUTHORIZATION_URL }),
	disconnect: vi.fn(),
});

const aListOf = (
	...connections: WorkTrackingSystemConnection[]
): IWorkTrackingSystemService => {
	const service = createMockWorkTrackingSystemService();
	vi.mocked(service.getConfiguredWorkTrackingSystems).mockResolvedValue(
		connections,
	);
	return service;
};

const givenPriyaOpensTheConnection = async (
	load: () => Promise<WorkTrackingSystemConnection | null>,
	oauthService: IOAuthService = anOAuthServiceThatSignsIn(),
) => {
	const getConnectionSettings = vi.fn(load);
	await renderWithTerms(
		<MemoryRouter>
			<ModifyConnectionSettings
				title="Update Connection"
				getSupportedSystems={vi.fn().mockResolvedValue([])}
				getConnectionSettings={getConnectionSettings}
				saveConnectionSettings={vi.fn()}
				validateConnectionSettings={vi.fn()}
			/>
		</MemoryRouter>,
		{},
		{ oauthService },
	);
	return { getConnectionSettings, oauthService };
};

const givenPriyaOpensTheLoadedConnection = async (signIn: SignIn) => {
	const opened = await givenPriyaOpensTheConnection(() =>
		Promise.resolve(fabrikamWithWriteBack(signIn)),
	);
	await screen.findByDisplayValue(FABRIKAM);
	return opened;
};

const givenPriyaOpensTheConnectionsList = async (
	service: IWorkTrackingSystemService,
	oauthService: IOAuthService = anOAuthServiceThatSignsIn(),
) => {
	await renderWithTerms(
		<MemoryRouter>
			<ConnectionsSettings />
		</MemoryRouter>,
		{},
		{ workTrackingSystemService: service, oauthService },
	);
	return { service, oauthService };
};

const theWriteConsentWarnings = () =>
	screen
		.queryAllByRole("alert")
		.filter((alert) =>
			alert.textContent?.includes("Write-back needs more rights."),
		);

const theWriteConsentWarning = () => {
	const warnings = theWriteConsentWarnings();
	expect(
		warnings,
		"WHAT: expected exactly one write-consent warning on screen. WHY: a connection with write-back whose sign-in may only read must tell the admin it needs more rights. HOW: render WriteConsentAlert when needsWriteConsent is true.",
	).toHaveLength(1);
	return warnings[0];
};

const whenPriyaSignsInAgain = async (warning: HTMLElement) => {
	await userEvent
		.setup()
		.click(within(warning).getByRole("button", { name: /^Sign in again$/ }));
};

const thenTheWarningSaysWhatIsMissingAndHowToFixIt = (warning: HTMLElement) => {
	expect(
		warning,
		"WHAT: the warning copy differs from the approved sketch. WHY: it tells the admin why write-back is not written and what to do. HOW: use the sketch copy word for word.",
	).toHaveTextContent(WRITE_CONSENT_COPY);
	expect(
		warning.className,
		"WHAT: the write-consent message is not a warning Alert. WHY: it must read like the Reconnect banner. HOW: use an MUI Alert with severity warning.",
	).toMatch(/MuiAlert-colorWarning/);
};

const thenTheSignInStartedWithoutDisconnecting = async (
	oauthService: IOAuthService,
) => {
	await waitFor(() =>
		expect(
			openOAuthPopupMock,
			"WHAT: Sign in again did not open the sign-in popup at the address the server gave. HOW: start the OAuth flow and open its authorization URL.",
		).toHaveBeenCalledWith(AUTHORIZATION_URL),
	);
	expect(
		oauthService.initiateConnect,
		"WHAT: Sign in again did not start the sign-in for this connection. HOW: call initiateConnect with the connection's method key and id.",
	).toHaveBeenCalledWith("ado.oauth", 7);
	expect(
		oauthService.disconnect,
		"WHAT: Sign in again disconnected the connection first. WHY: if the admin cancels, reads must keep working. HOW: unlike Reconnect, never call disconnect.",
	).not.toHaveBeenCalled();
};

const thenItAppearsAbove = (warning: HTMLElement, later: HTMLElement) => {
	expect(
		warning.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING,
		"WHAT: the warning is not above the element expected after it. HOW: render the warning directly above the write-back mappings.",
	).toBeTruthy();
};

const theHeading = (text: string) => screen.getByText(text, { exact: true });

describe("A connection whose sign-in may only read asks for write consent once write-back is set up", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
		openOAuthPopupMock.mockResolvedValue({ status: "success" });
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("the connection page shows the read-only rights and a warning above the write-back mappings, and signing in again reloads the connection", async () => {
		const { getConnectionSettings, oauthService } =
			await givenPriyaOpensTheLoadedConnection(MAY_ONLY_READ);

		const warning = theWriteConsentWarning();
		expect(
			screen.getByText(READ_ONLY_RIGHTS),
			"WHAT: no 'Rights: Read work items' line. WHY: the admin must see which rights the sign-in holds. HOW: show the rights wording in the Authentication section when grantedRights is set.",
		).toBeInTheDocument();
		thenTheWarningSaysWhatIsMissingAndHowToFixIt(warning);
		thenItAppearsAbove(theHeading("Authentication"), warning);
		thenItAppearsAbove(warning, theHeading("Sync with Source"));

		await whenPriyaSignsInAgain(warning);

		await thenTheSignInStartedWithoutDisconnecting(oauthService);
		await waitFor(() =>
			expect(
				getConnectionSettings,
				"WHAT: after a successful sign-in the connection was not loaded again. WHY: the new rights must replace the warning. HOW: reload the connection on success.",
			).toHaveBeenCalledTimes(2),
		);
	});

	it("the connection page shows read and write rights and no warning once write consent is given", async () => {
		await givenPriyaOpensTheLoadedConnection(MAY_READ_AND_WRITE);

		expect(
			screen.getByText(READ_AND_WRITE_RIGHTS),
			"WHAT: no 'Rights: Read and write work items' line. HOW: word ReadAndWrite as 'Read and write work items'.",
		).toBeInTheDocument();
		expect(
			theWriteConsentWarnings(),
			"WHAT: a write-consent warning shows although the sign-in may write. HOW: render it only when needsWriteConsent is true.",
		).toHaveLength(0);
	});

	it("while the connection loads, neither the rights nor the warning show", async () => {
		await givenPriyaOpensTheConnection(() => new Promise(() => {}));

		expect(
			screen.queryByText(/^Rights: /),
			"WHAT: a rights line shows before the connection has loaded. WHY: no guessed rights may be shown. HOW: read rights only from the loaded connection.",
		).not.toBeInTheDocument();
		expect(theWriteConsentWarnings()).toHaveLength(0);
	});

	it("the connections list shows the warning only beside the connection that needs write consent, and signing in again reloads the list", async () => {
		const { service, oauthService } = await givenPriyaOpensTheConnectionsList(
			aListOf(
				fabrikamWithWriteBack(MAY_ONLY_READ),
				fabrikamWithWriteBack(MAY_READ_AND_WRITE, 8, "Contoso ADO"),
			),
		);
		await screen.findByText(FABRIKAM);

		const warning = theWriteConsentWarning();
		thenTheWarningSaysWhatIsMissingAndHowToFixIt(warning);

		await whenPriyaSignsInAgain(warning);

		await thenTheSignInStartedWithoutDisconnecting(oauthService);
		await waitFor(() =>
			expect(
				service.getConfiguredWorkTrackingSystems,
				"WHAT: after a successful sign-in the connections list was not loaded again. HOW: reload the list on success, as Reconnect does.",
			).toHaveBeenCalledTimes(2),
		);
	});

	it("when the connections list cannot be loaded, no warning shows", async () => {
		const service = createMockWorkTrackingSystemService();
		vi.mocked(service.getConfiguredWorkTrackingSystems).mockRejectedValue(
			new Error("offline"),
		);
		await givenPriyaOpensTheConnectionsList(service);

		await waitFor(() =>
			expect(service.getConfiguredWorkTrackingSystems).toHaveBeenCalled(),
		);
		expect(theWriteConsentWarnings()).toHaveLength(0);
	});
});

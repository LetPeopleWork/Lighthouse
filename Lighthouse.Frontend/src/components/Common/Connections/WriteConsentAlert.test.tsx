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

const needingReconnect = (connection: WorkTrackingSystemConnection) =>
	Object.assign(connection, { requiresReconnect: true });

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

const givenPriyaOpensTheConnectionWhoseReloadFails = async (signIn: SignIn) => {
	let calls = 0;
	const opened = await givenPriyaOpensTheConnection(() => {
		calls += 1;
		return calls === 1
			? Promise.resolve(fabrikamWithWriteBack(signIn))
			: Promise.reject(new Error("offline"));
	});
	await screen.findByDisplayValue(FABRIKAM);
	return opened;
};

const whileWatchingForUnhandledFailures = () => {
	const unhandled: unknown[] = [];
	const remember = (reason: unknown) => {
		unhandled.push(reason);
	};
	process.on("unhandledRejection", remember);
	return {
		unhandled,
		stopWatching: () => process.off("unhandledRejection", remember),
	};
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

const theReconnectBanners = () =>
	screen
		.queryAllByRole("alert")
		.filter(
			(alert) =>
				within(alert).queryByRole("button", { name: /^Reconnect$/ }) !== null,
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

const thenItAppearsAbove = (
	earlier: HTMLElement,
	later: HTMLElement,
	what: string,
) => {
	expect(
		earlier.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING,
		`WHAT: ${what}. WHY: the admin reads each part where the approved sketch puts it. HOW: keep the order of the sketch.`,
	).toBeTruthy();
};

const theHeading = (text: string) => screen.getByText(text, { exact: true });

const thenTheRightsLineSitsInTheAuthenticationSection = (
	rightsLine: HTMLElement,
) => {
	thenItAppearsAbove(
		theHeading("Authentication"),
		rightsLine,
		"the Rights line is not below the Authentication heading",
	);
	thenItAppearsAbove(
		rightsLine,
		theHeading("Additional Fields"),
		"the Rights line is not inside the Authentication section, before the Additional Fields section",
	);
};

const theRightsLine = (text: string) => {
	const rightsLine = screen.queryByText(text, { exact: true });
	expect(
		rightsLine,
		`WHAT: no '${text}' line. WHY: the admin must see which rights the sign-in holds. HOW: show the rights wording in the Authentication section when grantedRights is set.`,
	).not.toBeNull();
	return rightsLine as HTMLElement;
};

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
		thenTheRightsLineSitsInTheAuthenticationSection(
			theRightsLine(READ_ONLY_RIGHTS),
		);
		thenTheWarningSaysWhatIsMissingAndHowToFixIt(warning);
		thenItAppearsAbove(
			theHeading("Authentication"),
			warning,
			"the warning is not below the Authentication section",
		);
		thenItAppearsAbove(
			warning,
			theHeading("Sync with Source"),
			"the warning is not directly above the write-back mappings",
		);

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

		thenTheRightsLineSitsInTheAuthenticationSection(
			theRightsLine(READ_AND_WRITE_RIGHTS),
		);
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
				needingReconnect(
					fabrikamWithWriteBack(MAY_READ_AND_WRITE, 8, "Contoso ADO"),
				),
				fabrikamWithWriteBack(MAY_ONLY_READ),
				needingReconnect(
					fabrikamWithWriteBack(MAY_READ_AND_WRITE, 9, "Northwind ADO"),
				),
			),
		);
		await screen.findByText(FABRIKAM);

		const warning = theWriteConsentWarning();
		thenTheWarningSaysWhatIsMissingAndHowToFixIt(warning);
		const [contosoReconnect, northwindReconnect] = theReconnectBanners();
		thenItAppearsAbove(
			contosoReconnect,
			warning,
			"the warning is not after the banner of the connection listed before Fabrikam ADO",
		);
		thenItAppearsAbove(
			warning,
			northwindReconnect,
			"the warning is not before the banner of the connection listed after Fabrikam ADO",
		);

		await whenPriyaSignsInAgain(warning);

		await thenTheSignInStartedWithoutDisconnecting(oauthService);
		await waitFor(() =>
			expect(
				service.getConfiguredWorkTrackingSystems,
				"WHAT: after a successful sign-in the connections list was not loaded again. HOW: reload the list on success, as Reconnect does.",
			).toHaveBeenCalledTimes(2),
		);
	});

	it("when the connections list cannot be loaded again after signing in, the earlier warning is not shown as current", async () => {
		let failTheReload: (reason: Error) => void = () => {};
		const service = aListOf(fabrikamWithWriteBack(MAY_ONLY_READ));
		vi.mocked(service.getConfiguredWorkTrackingSystems)
			.mockResolvedValueOnce([fabrikamWithWriteBack(MAY_ONLY_READ)])
			.mockReturnValueOnce(
				new Promise((_, reject) => {
					failTheReload = reject;
				}),
			);
		await givenPriyaOpensTheConnectionsList(service);
		await screen.findByText(FABRIKAM);

		await whenPriyaSignsInAgain(theWriteConsentWarning());

		await waitFor(() =>
			expect(
				theWriteConsentWarnings(),
				"WHAT: the warning from before the sign-in still shows while the list loads again. WHY: it may no longer be true and must not look current. HOW: drop the remembered list when the sign-in succeeds.",
			).toHaveLength(0),
		);
		expect(
			screen.getByRole("progressbar", { name: /^Loading the .+$/ }),
			"WHAT: no loading look while the list loads again. WHY: an empty area would look like no connections. HOW: show the list's spinner while it loads.",
		).toBeInTheDocument();
		failTheReload(new Error("offline"));
		await screen.findByText(/^Could not load the .+\.$/);
		expect(
			theWriteConsentWarnings(),
			"WHAT: a write-consent warning shows beside the could-not-load message. WHY: a failed list says nothing about the sign-in's rights. HOW: show only the could-not-load look when the list fails.",
		).toHaveLength(0);
	});
	it("when the connection page cannot load the connection again after signing in, the earlier rights and warning are not shown as current", async () => {
		const watch = whileWatchingForUnhandledFailures();
		try {
			const { getConnectionSettings } =
				await givenPriyaOpensTheConnectionWhoseReloadFails(MAY_ONLY_READ);

			await whenPriyaSignsInAgain(theWriteConsentWarning());

			await waitFor(() =>
				expect(
					getConnectionSettings,
					"WHAT: after a successful sign-in the connection was not loaded again. HOW: reload the connection on success.",
				).toHaveBeenCalledTimes(2),
			);
			await waitFor(() =>
				expect(
					theWriteConsentWarnings(),
					"WHAT: the warning from before the sign-in still shows after the connection could not be loaded again. WHY: the sign-in may have given write consent, so the warning may no longer be true and must not look current. HOW: when the reload fails, drop the remembered rights and warning or show the page's could-not-load look.",
				).toHaveLength(0),
			);
			expect(
				screen.queryByText(/^Rights: /),
				"WHAT: the Rights line from before the sign-in still shows after the connection could not be loaded again. WHY: the sign-in may have changed the rights, so the old line must not look current. HOW: when the reload fails, drop the remembered rights or show the page's could-not-load look.",
			).not.toBeInTheDocument();
			expect(
				watch.unhandled,
				"WHAT: the failed reload surfaced as an unhandled promise rejection. WHY: a failure nobody handles shows the admin nothing. HOW: catch the failed reload on the connection page and turn it into what the page shows.",
			).toHaveLength(0);
		} finally {
			watch.stopWatching();
		}
	});
});

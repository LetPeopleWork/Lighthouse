import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	type EncryptionKeyState,
	KEY_CUSTODY_VALUES,
	type KeyCustody,
} from "../../../models/Encryption/EncryptionKeyState";
import type { SecretReadabilityReport } from "../../../models/Encryption/SecretReadabilityReport";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockEncryptionService,
} from "../../../tests/MockApiServiceProvider";
import EncryptionPanel from "./EncryptionPanel";

const ownKey: EncryptionKeyState = {
	custody: "GeneratedForThisInstance",
	canMint: true,
	activeKeyId: "k-2026-08-16-01",
	keyIds: ["k-2026-08-16-01", "k-2025-11-02-01"],
	keyStorePath: "/app/data/keys",
	legacyDefaultPresent: false,
	secretsUnderPublishedKey: 0,
	readableSecretsNotOnTheActiveKey: 3,
	allowsStartWithUnreadableSecrets: false,
	keySuppliedThrough: null,
};

const startedPastTheRefusal: EncryptionKeyState = {
	...ownKey,
	allowsStartWithUnreadableSecrets: true,
};

// An upgraded instance whose credentials were written before the envelope format existed. They carry no
// key id, so nothing names the key that wrote them and the key list holds only the key in force - but
// they are readable, they are not on that key, and moving them is the whole point of the screen.
const writtenBeforeTheEnvelope: EncryptionKeyState = {
	...ownKey,
	keyIds: ["k-2026-08-16-01"],
	legacyDefaultPresent: true,
	secretsUnderPublishedKey: 2,
	readableSecretsNotOnTheActiveKey: 2,
};

// An instance that has rotated and has nothing left under the earlier key: the panel lists only the
// keys something is stored under, so the key in force is the only one there is.
const nothingLeftToMove: EncryptionKeyState = {
	...ownKey,
	keyIds: ["k-2026-08-16-01"],
	readableSecretsNotOnTheActiveKey: 0,
};

const justUpgraded: EncryptionKeyState = {
	...ownKey,
	legacyDefaultPresent: true,
	secretsUnderPublishedKey: 12,
	readableSecretsNotOnTheActiveKey: 12,
};

type OperatorOwnedCustody = Exclude<KeyCustody, "GeneratedForThisInstance">;

const operatorOwned: Record<OperatorOwnedCustody, EncryptionKeyState> = {
	SuppliedByConfiguration: {
		...ownKey,
		custody: "SuppliedByConfiguration",
		canMint: false,
		keySuppliedThrough: "Encryption__Key",
	},
	SuppliedByExternalSecret: {
		...ownKey,
		custody: "SuppliedByExternalSecret",
		canMint: false,
		keySuppliedThrough: "Encryption__KeysFile",
	},
	NoDurableStore: {
		...ownKey,
		custody: "NoDurableStore",
		canMint: false,
		activeKeyId: "k-legacy-default",
		keyIds: ["k-legacy-default"],
		legacyDefaultPresent: true,
	},
};

// The population that upgraded while relying on the setting name this release retired. Their key works,
// and the panel is the only place most of them will ever be told it is going away.
const readFromTheRetiredSetting: EncryptionKeyState = {
	...ownKey,
	custody: "SuppliedByConfiguration",
	canMint: false,
	keySuppliedThrough: "EncryptionSettings__EncryptionKey",
};

// Spelled out here rather than read from the wording the panel uses, so that changing a phrasing has to
// be a decision taken twice. Both maps are keyed by custody, so a custody added to the model without a
// phrasing or a state to render it from is a compile error rather than a case that quietly goes untested.
const CUSTODY_ON_SCREEN: Record<KeyCustody, string> = {
	NoDurableStore: "the key published with the product",
	GeneratedForThisInstance: "generated for this instance",
	SuppliedByConfiguration: "supplied by configuration",
	SuppliedByExternalSecret: "supplied by a mounted secret file",
};

const STATE_FOR_CUSTODY: Record<KeyCustody, EncryptionKeyState> = {
	NoDurableStore: operatorOwned.NoDurableStore,
	GeneratedForThisInstance: ownKey,
	SuppliedByConfiguration: operatorOwned.SuppliedByConfiguration,
	SuppliedByExternalSecret: operatorOwned.SuppliedByExternalSecret,
};

const aReportNamingWhatCouldNotBeRead: SecretReadabilityReport = {
	activeKeyId: "k-2026-08-16-02",
	keysChangedWhileItRan: false,
	movedCount: 46,
	unreadableCount: 1,
	onActiveKeyCount: 46,
	onRetiredKeyCount: 0,
	plaintextCount: 0,
	secrets: [
		{
			connectionId: 7,
			connectionName: "Contoso Board",
			field: "ClientSecret",
			keyId: "k-lost-forever",
			state: "Unreadable",
			outcome: "CouldNotBeRead",
		},
	],
	byConnection: [
		{
			connectionId: 7,
			connectionName: "Contoso Board",
			movedCount: 46,
			unreadableCount: 1,
		},
	],
};

const renderPanelOn = (
	keyState: EncryptionKeyState,
	report?: SecretReadabilityReport,
) => {
	const encryptionService = createMockEncryptionService();
	vi.mocked(encryptionService.getKeyState).mockResolvedValue(keyState);

	if (report) {
		vi.mocked(encryptionService.rotateKey).mockResolvedValue(report);
		vi.mocked(encryptionService.reEncryptSecrets).mockResolvedValue(report);
		vi.mocked(encryptionService.checkSecrets).mockResolvedValue(report);
	}

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ encryptionService })}
		>
			<EncryptionPanel />
		</ApiServiceContext.Provider>,
	);

	return encryptionService;
};

describe("EncryptionPanel", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("offers to rotate where Lighthouse made the key itself", async () => {
		renderPanelOn(ownKey);

		expect(await screen.findByTestId("rotate-key-button")).toBeInTheDocument();
		expect(screen.getByTestId("reencrypt-button")).toBeInTheDocument();
	});

	it.each(Object.keys(operatorOwned) as OperatorOwnedCustody[])(
		"draws no rotate control at all where the key is %s",
		async (custody) => {
			renderPanelOn(operatorOwned[custody]);

			// Checking is the one action offered in every state, so it is what says the panel has drawn.
			// The move is not: an instance whose key in force is the published key has nowhere to move to.
			await screen.findByTestId("check-secrets-button");

			expect(screen.queryByTestId("rotate-key-button")).not.toBeInTheDocument();
			expect(
				screen.getByTestId("encryption-custody-explanation"),
			).toHaveTextContent(/belongs to|nowhere to keep/);
		},
	);

	it("lists the keys the instance holds, by name", async () => {
		const encryptionService = renderPanelOn(ownKey);

		expect(
			await screen.findByTestId("encryption-key-k-2026-08-16-01"),
		).toBeInTheDocument();

		// One read per mount. The effect depends on the service from context, so a context value rebuilt
		// per render would turn this screen into an unbounded fetch loop against a System-Admin-guarded
		// endpoint, and every other assertion here would still pass.
		expect(encryptionService.getKeyState).toHaveBeenCalledTimes(1);
		expect(
			screen.getByTestId("encryption-key-k-2025-11-02-01"),
		).toBeInTheDocument();
		expect(screen.getByTestId("encryption-active-key-id")).toHaveTextContent(
			"k-2026-08-16-01",
		);
	});

	it.each([...KEY_CUSTODY_VALUES])(
		"should describe custody %s in words rather than as an enum name",
		async (custody) => {
			renderPanelOn(STATE_FOR_CUSTODY[custody]);

			await waitFor(() => {
				expect(screen.getByTestId("encryption-custody")).toHaveTextContent(
					CUSTODY_ON_SCREEN[custody],
				);
			});
			expect(
				screen.queryByText(custody, { exact: false }),
			).not.toBeInTheDocument();
		},
	);

	it("names each secret that could not be read by its Connection and field", async () => {
		renderPanelOn(ownKey, aReportNamingWhatCouldNotBeRead);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent(
			"Made key k-2026-08-16-02 and put it in force",
		);
		expect(report).toHaveTextContent("46 stored secrets moved onto it");
		expect(report).toHaveTextContent("1 stored secret could not be read");
		expect(screen.getByTestId("encryption-report-secrets")).toHaveTextContent(
			"Contoso Board",
		);
		expect(screen.getByTestId("encryption-report-secrets")).toHaveTextContent(
			"ClientSecret",
		);
	});

	it("lists only the secrets that need somebody to do something", async () => {
		const mixed: SecretReadabilityReport = {
			activeKeyId: "k-2026-08-16-02",
			keysChangedWhileItRan: false,
			movedCount: 2,
			unreadableCount: 1,
			onActiveKeyCount: 2,
			onRetiredKeyCount: 1,
			plaintextCount: 1,
			secrets: [
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "PersonalAccessToken",
					keyId: "k-2026-08-16-02",
					state: "Envelope",
					outcome: "Moved",
				},
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "AccessToken",
					keyId: "k-2026-08-16-02",
					state: "Envelope",
					outcome: "MovedByAnotherWriter",
				},
				{
					connectionId: 9,
					connectionName: "Fabrikam Tracker",
					field: "ClientSecret",
					keyId: "k-lost-forever",
					state: "Unreadable",
					outcome: "CouldNotBeRead",
				},
				{
					connectionId: 9,
					connectionName: "Fabrikam Tracker",
					field: "ApiToken",
					keyId: null,
					state: "LegacyPlaintext",
					outcome: "NotEncrypted",
				},
				{
					connectionId: 9,
					connectionName: "Fabrikam Tracker",
					field: "RefreshToken",
					keyId: "k-2025-11-02-01",
					state: "Envelope",
					outcome: "CouldNotBeWritten",
				},
			],
			byConnection: [],
		};

		renderPanelOn(ownKey, mixed);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		const table = await screen.findByTestId("encryption-report-secrets");

		// A secret that moved needs no row: the counts already say so, and burying the two that need
		// action under the forty-six that do not is how an operator misses them.
		expect(table).toHaveTextContent("ClientSecret");
		expect(table).toHaveTextContent("ApiToken");
		expect(table).not.toHaveTextContent("PersonalAccessToken");
		expect(table).not.toHaveTextContent("AccessToken");
		expect(table).toHaveTextContent("could not be read");
		expect(table).toHaveTextContent("was not encrypted");

		// A database that would not take the write is a pass to run again, not a token to reissue, and
		// the row has to say which of the two it is.
		expect(table).toHaveTextContent("RefreshToken");
		expect(table).toHaveTextContent("run this again");

		expect(screen.getByTestId("encryption-report-summary").className).toMatch(
			/Warning/,
		);
	});

	it("does not show a disturbed move as completed, and says to run it again", async () => {
		const disturbed: SecretReadabilityReport = {
			...aReportNamingWhatCouldNotBeRead,
			keysChangedWhileItRan: true,
		};

		renderPanelOn(ownKey, disturbed);

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent("keys changed while this was running");
		expect(report).toHaveTextContent("run it again");

		// The counts describe a rotation nobody finished, and an operator who reads one of them stops
		// there instead of doing the one thing that is actually left to do.
		expect(report).not.toHaveTextContent("Moved 46 stored secrets onto key");

		expect(screen.getByTestId("encryption-report-summary").className).toMatch(
			/Warning/,
		);
	});

	it("says nothing about the keys having changed when they did not", async () => {
		renderPanelOn(ownKey, aReportNamingWhatCouldNotBeRead);

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent("Moved 46 stored secrets onto key");
		expect(report).not.toHaveTextContent("keys changed while this was running");
	});

	it("names no key material when it says the keys changed", async () => {
		const disturbed: SecretReadabilityReport = {
			...aReportNamingWhatCouldNotBeRead,
			keysChangedWhileItRan: true,
		};

		renderPanelOn(ownKey, disturbed);

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		const said =
			(await screen.findByTestId("encryption-report")).textContent ?? "";

		// Key identifiers are the only thing about a key an operator is ever shown. Anything base64-shaped
		// in a sentence about keys is the one thing that must never reach a browser or a log.
		expect(said).not.toMatch(/[A-Za-z0-9+/]{40,}={0,2}/);
	});

	it("shows no table at all when nothing was left behind", async () => {
		const clean: SecretReadabilityReport = {
			activeKeyId: "k-2026-08-16-02",
			keysChangedWhileItRan: false,
			movedCount: 3,
			unreadableCount: 0,
			onActiveKeyCount: 3,
			onRetiredKeyCount: 0,
			plaintextCount: 0,
			secrets: [
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "PersonalAccessToken",
					keyId: "k-2026-08-16-02",
					state: "Envelope",
					outcome: "Moved",
				},
			],
			byConnection: [],
		};

		renderPanelOn(ownKey, clean);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent(
			"Made key k-2026-08-16-02 and put it in force",
		);
		expect(report).toHaveTextContent("3 stored secrets moved onto it");
		expect(report).not.toHaveTextContent("could not be read");
		expect(
			screen.queryByTestId("encryption-report-secrets"),
		).not.toBeInTheDocument();

		// Nothing was left behind, so the result must not be dressed as a warning - an operator who is
		// warned when there is nothing to do stops reading the ones that matter.
		expect(screen.getByTestId("encryption-report-summary").className).toMatch(
			/Success/,
		);
	});

	it("tells an administrator who owns the key in every custody mode", async () => {
		renderPanelOn(ownKey);

		expect(
			await screen.findByTestId("encryption-custody-explanation"),
		).toHaveTextContent("Lighthouse made this key and keeps it");
	});

	it("names the key store and the key source", async () => {
		renderPanelOn(ownKey);

		const ring = await screen.findByTestId("encryption-key-ring");

		expect(ring).toHaveTextContent("Key source");
		expect(ring).toHaveTextContent("generated for this instance");
		expect(screen.getByTestId("encryption-key-store-path")).toHaveTextContent(
			"/app/data/keys",
		);
	});

	it("moves the stored secrets without making a key where an operator owns it", async () => {
		const encryptionService = renderPanelOn(
			operatorOwned.SuppliedByConfiguration,
			aReportNamingWhatCouldNotBeRead,
		);

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		await waitFor(() => {
			expect(encryptionService.reEncryptSecrets).toHaveBeenCalled();
		});
		expect(encryptionService.rotateKey).not.toHaveBeenCalled();
	});

	it("says so when the move was refused, rather than reporting a rotation that did not happen", async () => {
		const encryptionService = createMockEncryptionService();
		vi.mocked(encryptionService.getKeyState).mockResolvedValue(ownKey);
		vi.mocked(encryptionService.rotateKey).mockRejectedValue(
			new Error("This instance cannot make a new encryption key"),
		);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ encryptionService })}
			>
				<EncryptionPanel />
			</ApiServiceContext.Provider>,
		);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		expect(await screen.findByTestId("encryption-failure")).toHaveTextContent(
			"cannot make a new encryption key",
		);
		expect(screen.queryByTestId("encryption-report")).not.toBeInTheDocument();
	});

	it("shows nothing at all when the key state cannot be read", async () => {
		const encryptionService = createMockEncryptionService();
		vi.mocked(encryptionService.getKeyState).mockRejectedValue(
			new Error("forbidden"),
		);

		const { container } = render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ encryptionService })}
			>
				<EncryptionPanel />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(encryptionService.getKeyState).toHaveBeenCalled();
		});
		expect(container).toBeEmptyDOMElement();
	});

	it("says something useful when the failure carries no message", async () => {
		const encryptionService = createMockEncryptionService();
		vi.mocked(encryptionService.getKeyState).mockResolvedValue(ownKey);
		vi.mocked(encryptionService.rotateKey).mockRejectedValue("not an error");

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ encryptionService })}
			>
				<EncryptionPanel />
			</ApiServiceContext.Provider>,
		);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		expect(await screen.findByTestId("encryption-failure")).toHaveTextContent(
			"The stored secrets could not be moved.",
		);
	});

	it("reports nothing as failed when the move succeeded", async () => {
		renderPanelOn(ownKey, aReportNamingWhatCouldNotBeRead);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));
		await screen.findByTestId("encryption-report");

		expect(screen.queryByTestId("encryption-failure")).not.toBeInTheDocument();
	});

	it("refuses to be asked twice while a pass is still running", async () => {
		const encryptionService = createMockEncryptionService();
		vi.mocked(encryptionService.getKeyState).mockResolvedValue(ownKey);

		let finish: (report: SecretReadabilityReport) => void = () => {};
		vi.mocked(encryptionService.rotateKey).mockReturnValue(
			new Promise((resolve) => {
				finish = resolve;
			}),
		);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ encryptionService })}
			>
				<EncryptionPanel />
			</ApiServiceContext.Provider>,
		);

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		// Rewriting every stored credential is not something to start a second time by double-clicking.
		expect(await screen.findByTestId("rotate-key-button")).toBeDisabled();
		expect(screen.getByTestId("reencrypt-button")).toBeDisabled();

		finish(aReportNamingWhatCouldNotBeRead);

		await waitFor(() => {
			expect(screen.getByTestId("rotate-key-button")).toBeEnabled();
		});
	});

	it("says when the instance was started past the refusal", async () => {
		renderPanelOn(startedPastTheRefusal);

		const notice = await screen.findByTestId("started-past-the-refusal-notice");

		// Whoever finds this months later is rarely the person who set it, so the notice has to say what
		// is still owed rather than only that something happened.
		expect(notice).toHaveTextContent(
			"Encryption__StartEvenIfNothingStoredCanBeRead",
		);
		expect(notice).toHaveTextContent("enter those credentials again");
		expect(notice).toHaveTextContent("remove the setting");
	});

	it("still offers the check that names what has to be re-entered", async () => {
		renderPanelOn(startedPastTheRefusal);

		await screen.findByTestId("started-past-the-refusal-notice");

		expect(screen.getByTestId("check-secrets-button")).toBeEnabled();
	});

	it("says nothing about a hatch an instance never opened", async () => {
		renderPanelOn(ownKey);

		await screen.findByTestId("reencrypt-button");

		expect(
			screen.queryByTestId("started-past-the-refusal-notice"),
		).not.toBeInTheDocument();
	});

	it("tells an administrator who has just upgraded, without their having asked", async () => {
		const encryptionService = renderPanelOn(justUpgraded);

		const notice = await screen.findByTestId("published-key-notice");

		expect(notice).toHaveTextContent("12 stored credentials are");
		expect(notice).toHaveTextContent("key published with Lighthouse");
		expect(encryptionService.checkSecrets).not.toHaveBeenCalled();
	});

	// The situation, then the action. Why that key is no protection is a paragraph, and it belongs on the
	// page the header links to rather than in front of somebody who has to decide what to press.
	it("states the situation and then the action, and leaves the why to the docs", async () => {
		renderPanelOn(justUpgraded);

		const notice = await screen.findByTestId("published-key-notice");

		expect(notice).toHaveTextContent(
			"still encrypted with the key published with Lighthouse",
		);
		expect(notice).toHaveTextContent("nothing has to be re-entered");
		expect(notice).not.toHaveTextContent("anyone who has a copy");
	});

	it("writes one exposed credential in the singular", async () => {
		renderPanelOn({ ...justUpgraded, secretsUnderPublishedKey: 1 });

		const notice = await screen.findByTestId("published-key-notice");

		expect(notice).toHaveTextContent("1 stored credential is");
		expect(notice).toHaveTextContent("Move it onto this instance's own key");
	});

	// Everything an operator has to type. The observed instruction named Encryption__Keys to an operator
	// who had set Encryption__Key, never said both existed or which won, and gave no separator - so a
	// guess at a newline earned a startup refusal.
	it("gives a rotation instruction that can be followed", async () => {
		renderPanelOn(operatorOwned.SuppliedByConfiguration);

		const custody = await screen.findByTestId("encryption-custody-explanation");

		expect(custody).toHaveTextContent("Encryption__Key,");
		expect(custody).toHaveTextContent("comma-separated list");
		expect(custody).toHaveTextContent("name:base64");
		expect(custody).toHaveTextContent(
			"the first entry is the one new secrets are written under",
		);
		expect(custody).toHaveTextContent("the plural wins if you set both");
	});

	it("names the setting the operator actually set", async () => {
		renderPanelOn(operatorOwned.SuppliedByExternalSecret);

		const custody = await screen.findByTestId("encryption-custody-explanation");

		expect(custody).toHaveTextContent("Encryption__KeysFile");
		expect(custody).toHaveTextContent("restart the pod");
	});

	it("tells nobody to edit a setting where Lighthouse keeps the key itself", async () => {
		renderPanelOn(ownKey);

		const custody = await screen.findByTestId("encryption-custody-explanation");

		expect(custody).not.toHaveTextContent("To replace it");
		expect(custody).toHaveTextContent("it can make a new one for you");
	});

	// Read cold, the screen used to open on a table headed "Key source" with nothing saying what any of
	// it was about. A maintainer reading it as a first-time user: "I would genuinely not understand what
	// I'm seeing."
	it("says what the screen is about before it says anything else", async () => {
		renderPanelOn(ownKey);

		const subject = await screen.findByTestId("encryption-subject");

		expect(subject).toHaveTextContent(
			"credentials stored in your Connections are encrypted at rest",
		);
		expect(screen.getByTestId("encryption-docs-link")).toHaveAttribute(
			"href",
			expect.stringContaining("settings/encryption"),
		);
	});

	// The alert used to carry its own copy of the move while the button row carried another, both calling
	// the same thing under two names — and the only emphasised control on the screen was Rotate, which
	// mints a third key and is not what an upgraded instance needs.
	it("names the action instead of carrying its own copy of it", async () => {
		renderPanelOn(justUpgraded, aReportNamingWhatCouldNotBeRead);

		const notice = await screen.findByTestId("published-key-notice");

		expect(within(notice).queryByRole("button")).not.toBeInTheDocument();
		expect(screen.getByTestId("reencrypt-button")).toBeInTheDocument();
	});

	it("offers the one action that fixes it, once", async () => {
		const encryptionService = renderPanelOn(
			justUpgraded,
			aReportNamingWhatCouldNotBeRead,
		);

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		await waitFor(() => {
			expect(encryptionService.reEncryptSecrets).toHaveBeenCalledTimes(1);
		});
		expect(encryptionService.rotateKey).not.toHaveBeenCalled();
	});

	it("emphasises the move when something is still on the published key", async () => {
		renderPanelOn(justUpgraded);

		const move = await screen.findByTestId("reencrypt-button");

		expect(move.className).toContain("MuiButton-contained");
		expect(screen.getByTestId("rotate-key-button").className).not.toContain(
			"MuiButton-contained",
		);
	});

	it("emphasises nothing on an instance with nothing wrong", async () => {
		renderPanelOn(nothingLeftToMove);

		const rotate = await screen.findByTestId("rotate-key-button");

		expect(rotate.className).not.toContain("MuiButton-contained");
		expect(screen.getByTestId("check-secrets-button")).toBeInTheDocument();
	});

	// Observed on a real deployment, 2026-08-17: a Postgres instance given a key store, holding two
	// credentials from before the envelope format. The panel showed one key, no move, and a banner telling
	// the operator to move them. The gate was reading key ids off the stored values, and a value written
	// before the envelope has none to read.
	it("offers the move for credentials that name no key but are not on the key in force", async () => {
		renderPanelOn(writtenBeforeTheEnvelope);

		expect(await screen.findByTestId("reencrypt-button")).toBeInTheDocument();
	});

	// The severe half of the same defect. Where the operator supplies the key nothing can be minted, so a
	// hidden move leaves that instance with no way at all to get its credentials off the published key.
	it("offers the move on a supplied key, which cannot rotate its way out", async () => {
		renderPanelOn({
			...writtenBeforeTheEnvelope,
			custody: "SuppliedByConfiguration",
			canMint: false,
			keySuppliedThrough: "Encryption__Key",
		});

		expect(await screen.findByTestId("reencrypt-button")).toBeInTheDocument();
		expect(screen.queryByTestId("rotate-key-button")).not.toBeInTheDocument();
	});

	it("does not offer a move when there is nothing to move", async () => {
		renderPanelOn(nothingLeftToMove);

		await screen.findByTestId("check-secrets-button");

		expect(screen.queryByTestId("reencrypt-button")).not.toBeInTheDocument();
	});

	// The move would re-encrypt the published key onto itself, change nothing, and leave the warning
	// standing. What fixes this instance is a key of its own, which the custody sentence already says.
	it("does not offer a move where the key in force is the published key", async () => {
		renderPanelOn(operatorOwned.NoDurableStore);

		await screen.findByTestId("check-secrets-button");

		expect(screen.queryByTestId("reencrypt-button")).not.toBeInTheDocument();
		expect(screen.queryByTestId("rotate-key-button")).not.toBeInTheDocument();
	});

	// The strongest signal of the verification session, and a rule rather than a rewrite: a count of zero
	// is not information. Four categories of nothing competed with the one number that mattered.
	it("says only the states that have something in them", async () => {
		renderPanelOn(ownKey, {
			activeKeyId: "k-2026-08-16-01",
			keysChangedWhileItRan: false,
			movedCount: 0,
			unreadableCount: 0,
			onActiveKeyCount: 1,
			onRetiredKeyCount: 0,
			plaintextCount: 0,
			secrets: [
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "ClientSecret",
					keyId: "k-2026-08-16-01",
					state: "Envelope",
					outcome: "Unmoved",
				},
			],
			byConnection: [],
		});

		await userEvent.click(await screen.findByTestId("check-secrets-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent("1 on the key in force");
		expect(report).not.toHaveTextContent("0 ");
	});

	// One Connection with one secret field is the smallest real instance there is, and it is what a
	// first-time operator has.
	it("writes one secret in the singular", async () => {
		renderPanelOn(ownKey, {
			activeKeyId: "k-2026-08-16-01",
			keysChangedWhileItRan: false,
			movedCount: 0,
			unreadableCount: 0,
			onActiveKeyCount: 1,
			onRetiredKeyCount: 0,
			plaintextCount: 0,
			secrets: [
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "ClientSecret",
					keyId: "k-2026-08-16-01",
					state: "Envelope",
					outcome: "Unmoved",
				},
			],
			byConnection: [],
		});

		await userEvent.click(await screen.findByTestId("check-secrets-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent("Checked 1 stored secret:");
		expect(report).not.toHaveTextContent("1 stored secrets");
	});

	it("says a key was made when a rotation had nothing to move", async () => {
		renderPanelOn(ownKey, {
			activeKeyId: "k-2026-08-16-02",
			keysChangedWhileItRan: false,
			movedCount: 0,
			unreadableCount: 0,
			onActiveKeyCount: 0,
			onRetiredKeyCount: 0,
			plaintextCount: 0,
			secrets: [],
			byConnection: [],
		});

		await userEvent.click(await screen.findByTestId("rotate-key-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent(
			"Made key k-2026-08-16-02 and put it in force",
		);
		expect(report).not.toHaveTextContent("moved");
	});

	it("says nothing needed moving rather than that nothing moved", async () => {
		const encryptionService = renderPanelOn(justUpgraded, {
			activeKeyId: "k-2026-08-16-01",
			keysChangedWhileItRan: false,
			movedCount: 0,
			unreadableCount: 0,
			onActiveKeyCount: 4,
			onRetiredKeyCount: 0,
			plaintextCount: 0,
			secrets: [],
			byConnection: [],
		});

		await userEvent.click(await screen.findByTestId("reencrypt-button"));

		await waitFor(() => {
			expect(encryptionService.reEncryptSecrets).toHaveBeenCalled();
		});

		const report = await screen.findByTestId("encryption-report");

		expect(report).toHaveTextContent("Nothing needed moving");
		expect(report).not.toHaveTextContent("Moved 0");
	});

	it("says nothing about the published key once nothing is left under it", async () => {
		renderPanelOn(ownKey);

		await screen.findByTestId("reencrypt-button");

		expect(
			screen.queryByTestId("published-key-notice"),
		).not.toBeInTheDocument();
	});

	it("says what every stored secret is on, and never that anything was moved", async () => {
		const checked: SecretReadabilityReport = {
			activeKeyId: "k-2026-08-16-01",
			keysChangedWhileItRan: false,
			movedCount: 0,
			unreadableCount: 1,
			onActiveKeyCount: 45,
			onRetiredKeyCount: 2,
			plaintextCount: 0,
			secrets: [
				{
					connectionId: 7,
					connectionName: "Contoso Board",
					field: "ClientSecret",
					keyId: "k-lost-forever",
					state: "Unreadable",
					outcome: "CouldNotBeRead",
				},
			],
			byConnection: [],
		};

		const encryptionService = renderPanelOn(ownKey, checked);

		await userEvent.click(await screen.findByTestId("check-secrets-button"));

		const report = await screen.findByTestId("encryption-report");

		expect(encryptionService.checkSecrets).toHaveBeenCalled();
		expect(report).toHaveTextContent("45 on the key in force");
		expect(report).toHaveTextContent("2 on an earlier key");
		expect(report).toHaveTextContent("1 could not be read");

		// A count of zero is not information. Nothing here was ever unencrypted, and saying so would put
		// a category of nothing beside the one number an operator has to act on.
		expect(report).not.toHaveTextContent("never encrypted");

		// Nothing was moved, because nothing was asked to be. Reusing the rotation's wording would greet
		// an operator with "Moved 0 stored secrets" on an instance where nothing at all is wrong.
		expect(report).not.toHaveTextContent("Moved");
		expect(encryptionService.reEncryptSecrets).not.toHaveBeenCalled();
		expect(encryptionService.rotateKey).not.toHaveBeenCalled();
	});

	it("names the Connection and the field of anything a check could not read", async () => {
		renderPanelOn(ownKey, aReportNamingWhatCouldNotBeRead);

		await userEvent.click(await screen.findByTestId("check-secrets-button"));

		const secrets = await screen.findByTestId("encryption-report-secrets");

		expect(secrets).toHaveTextContent("Contoso Board");
		expect(secrets).toHaveTextContent("ClientSecret");
	});

	// F-30. The key store directory exists in every custody and holds the data protection key and the
	// OAuth state secret, so it looks exactly like somewhere an encryption key would be. Naming it where
	// the key came from a setting is how an operator ends up backing up a folder that cannot restore
	// their credentials.
	it("names the key store directory only where Lighthouse keeps the key", async () => {
		renderPanelOn(ownKey);

		expect(
			await screen.findByTestId("encryption-key-store-path"),
		).toHaveTextContent("/app/data/keys");
	});

	it.each([
		["SuppliedByConfiguration", "Encryption__Key"],
		["SuppliedByExternalSecret", "Encryption__KeysFile"],
	] as Array<[OperatorOwnedCustody, string]>)(
		"names the setting rather than a directory where the key is %s",
		async (custody, setting) => {
			renderPanelOn(operatorOwned[custody]);

			const where = await screen.findByTestId("encryption-key-store-path");

			expect(where).toHaveTextContent(setting);
			expect(where).not.toHaveTextContent("/app/data/keys");
		},
	);

	it("says the key is nowhere where the instance has no key of its own", async () => {
		renderPanelOn(operatorOwned.NoDurableStore);

		expect(
			await screen.findByTestId("encryption-key-store-path"),
		).toHaveTextContent("nowhere");
	});

	// F-29. This is the one state where the published key IS the key in force, so there is nothing to move
	// anything onto - and the panel offers no move. An alert telling the operator to press it anyway sends
	// them looking for a button that is not there.
	it("does not tell an instance on the published key to move anything", async () => {
		// Postgres with no key store configured, holding credentials: the key in force is the published
		// key, so every stored credential is under it and moving them would re-encrypt it onto itself.
		renderPanelOn({
			...operatorOwned.NoDurableStore,
			secretsUnderPublishedKey: 4,
			readableSecretsNotOnTheActiveKey: 4,
		});

		const notice = await screen.findByTestId("published-key-notice");

		expect(notice).toHaveTextContent("no key of its own");
		expect(notice).not.toHaveTextContent(
			"Move them onto this instance's own key",
		);
		expect(screen.queryByTestId("reencrypt-button")).not.toBeInTheDocument();
	});

	// F-31. A move offered with nothing on screen explaining it: the report says the credentials are on the
	// key in force, because they are - what differs is the format they were written in.
	it("says why a move is offered when nothing is on the published key", async () => {
		renderPanelOn(ownKey);

		const notice = await screen.findByTestId("not-on-the-active-key-notice");

		expect(notice).toHaveTextContent("3 stored credentials are");
		expect(notice).toHaveTextContent("not on the key in force");
		expect(notice).toHaveTextContent("Nothing has to be re-entered");
	});

	it("says nothing about keys in force when there is nothing to move", async () => {
		renderPanelOn(nothingLeftToMove);

		await screen.findByTestId("encryption-key-ring");

		expect(
			screen.queryByTestId("not-on-the-active-key-notice"),
		).not.toBeInTheDocument();
	});

	// F-32. The instruction echoed whichever setting answered. Under the retired name that told the
	// operator to keep using the setting the startup banner says is going away - and a service or a
	// container never shows anybody a startup banner.
	it("sends a retired-setting instance to the setting that has a future", async () => {
		renderPanelOn(readFromTheRetiredSetting);

		const explanation = await screen.findByTestId(
			"encryption-custody-explanation",
		);

		expect(explanation).toHaveTextContent("this release retired");
		expect(explanation).toHaveTextContent(
			"put the new key first in Encryption__Key",
		);
		expect(explanation).not.toHaveTextContent(
			"put the new key first in EncryptionSettings__EncryptionKey",
		);
	});

	it("says nothing about a retirement where the setting is current", async () => {
		renderPanelOn(operatorOwned.SuppliedByConfiguration);

		expect(
			await screen.findByTestId("encryption-custody-explanation"),
		).not.toHaveTextContent("this release retired");
	});

	it("shows no key material of any kind", async () => {
		renderPanelOn(ownKey);

		const panel = await screen.findByTestId("encryption-key-ring");

		expect(panel.textContent).not.toMatch(/[A-Za-z0-9+/]{40,}={0,2}/);
	});
});

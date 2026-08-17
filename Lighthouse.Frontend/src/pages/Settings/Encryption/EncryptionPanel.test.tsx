import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EncryptionKeyState } from "../../../models/Encryption/EncryptionKeyState";
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
	allowsStartWithUnreadableSecrets: false,
	keySuppliedThrough: null,
};

const startedPastTheRefusal: EncryptionKeyState = {
	...ownKey,
	allowsStartWithUnreadableSecrets: true,
};

// An instance that has rotated and has nothing left under the earlier key: the panel lists only the
// keys something is stored under, so the key in force is the only one there is.
const nothingLeftToMove: EncryptionKeyState = {
	...ownKey,
	keyIds: ["k-2026-08-16-01"],
};

const justUpgraded: EncryptionKeyState = {
	...ownKey,
	legacyDefaultPresent: true,
	secretsUnderPublishedKey: 12,
};

const operatorOwned: Record<string, EncryptionKeyState> = {
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

	it.each(Object.keys(operatorOwned))(
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
		renderPanelOn(ownKey);

		expect(
			await screen.findByTestId("encryption-key-k-2026-08-16-01"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("encryption-key-k-2025-11-02-01"),
		).toBeInTheDocument();
		expect(screen.getByTestId("encryption-active-key-id")).toHaveTextContent(
			"k-2026-08-16-01",
		);
	});

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

		expect(screen.getByRole("alert").className).toMatch(/Warning/);
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

		expect(screen.getByRole("alert").className).toMatch(/Warning/);
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
		expect(screen.getByRole("alert").className).toMatch(/Success/);
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

	it("shows no key material of any kind", async () => {
		renderPanelOn(ownKey);

		const panel = await screen.findByTestId("encryption-key-ring");

		expect(panel.textContent).not.toMatch(/[A-Za-z0-9+/]{40,}={0,2}/);
	});
});

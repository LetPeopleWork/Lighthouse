import { render, screen, waitFor } from "@testing-library/react";
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
};

const startedPastTheRefusal: EncryptionKeyState = {
	...ownKey,
	allowsStartWithUnreadableSecrets: true,
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
	},
	SuppliedByExternalSecret: {
		...ownKey,
		custody: "SuppliedByExternalSecret",
		canMint: false,
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

			await screen.findByTestId("reencrypt-button");

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

		expect(report).toHaveTextContent("Moved 46 stored secrets");
		expect(report).toHaveTextContent("1 could not be read");
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

	it("shows no table at all when nothing was left behind", async () => {
		const clean: SecretReadabilityReport = {
			activeKeyId: "k-2026-08-16-02",
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

		expect(report).toHaveTextContent("Moved 3 stored secrets");
		expect(report).toHaveTextContent("0 could not be read");
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

		expect(notice).toHaveTextContent("12 stored credentials");
		expect(notice).toHaveTextContent("anyone who has a copy of Lighthouse");
		expect(encryptionService.checkSecrets).not.toHaveBeenCalled();
	});

	it("offers the one action that fixes it, beside the sentence saying so", async () => {
		const encryptionService = renderPanelOn(
			justUpgraded,
			aReportNamingWhatCouldNotBeRead,
		);

		await userEvent.click(
			await screen.findByTestId("published-key-notice-action"),
		);

		await waitFor(() => {
			expect(encryptionService.reEncryptSecrets).toHaveBeenCalled();
		});
		expect(encryptionService.rotateKey).not.toHaveBeenCalled();
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
		expect(report).toHaveTextContent("45 on the active key");
		expect(report).toHaveTextContent("2 on an earlier key");
		expect(report).toHaveTextContent("1 could not be read");

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

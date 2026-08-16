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

	it("shows no key material of any kind", async () => {
		renderPanelOn(ownKey);

		const panel = await screen.findByTestId("encryption-key-ring");

		expect(panel.textContent).not.toMatch(/[A-Za-z0-9+/]{40,}={0,2}/);
	});
});

import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "./ApiError";
import { EncryptionService } from "./EncryptionService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

const validKeyState = {
	custody: "SuppliedByConfiguration",
	canMint: false,
	activeKeyId: "operator-supplied",
	keyIds: ["operator-supplied", "lighthouse-default"],
	keyStorePath: "/app/data/keys",
	legacyDefaultPresent: true,
	secretsUnderPublishedKey: 2,
	allowsStartWithUnreadableSecrets: false,
	keySuppliedThrough: null,
};

const validReport = {
	activeKeyId: "k-2026-08-16-01",
	movedCount: 47,
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
	byConnection: [
		{
			connectionId: 7,
			connectionName: "Contoso Board",
			movedCount: 47,
			unreadableCount: 1,
		},
	],
};

describe("EncryptionService", () => {
	let encryptionService: EncryptionService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		encryptionService = new EncryptionService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("reads key state from the System Administrator only encryption endpoint", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: validKeyState });

		const keyState = await encryptionService.getKeyState();

		expect(mockedAxios.get).toHaveBeenCalledWith("/encryption");
		expect(keyState).toEqual(validKeyState);
	});

	it("checks the stored secrets by reading, not by asking for anything to be done", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: validReport });

		const report = await encryptionService.checkSecrets();

		expect(mockedAxios.get).toHaveBeenCalledWith("/encryption/secrets");
		expect(mockedAxios.post).not.toHaveBeenCalled();
		expect(report).toEqual(validReport);
	});

	it("rejects a check whose counts are not counts", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { ...validReport, onRetiredKeyCount: "a couple" },
		});

		const error = await encryptionService
			.checkSecrets()
			.catch((caught: unknown) => caught);

		expect(error).toBeInstanceOf(ApiError);
		expect((error as ApiError).code).toBe("INVALID_RESPONSE");
		expect((error as ApiError).technicalDetails).toContain("onRetiredKeyCount");
	});

	it("asks for a rotation and reads back what it moved", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: validReport });

		const report = await encryptionService.rotateKey();

		expect(mockedAxios.post).toHaveBeenCalledWith("/encryption/rotate");
		expect(report).toEqual(validReport);
	});

	it("asks to move the stored secrets onto the key already in force", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: validReport });

		const report = await encryptionService.reEncryptSecrets();

		expect(mockedAxios.post).toHaveBeenCalledWith("/encryption/reencrypt");
		expect(report).toEqual(validReport);
	});

	it("rejects a report that does not match the schema", async () => {
		mockedAxios.post.mockResolvedValueOnce({
			data: { ...validReport, movedCount: "several" },
		});

		const error = await encryptionService
			.rotateKey()
			.catch((caught: unknown) => caught);

		expect(error).toBeInstanceOf(ApiError);
		expect((error as ApiError).code).toBe("INVALID_RESPONSE");
	});

	it("rejects a key state response that does not match the schema", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { ...validKeyState, custody: "SomethingElse" },
		});

		const error = await encryptionService
			.getKeyState()
			.catch((caught: unknown) => caught);

		expect(error).toBeInstanceOf(ApiError);
		expect((error as ApiError).code).toBe("INVALID_RESPONSE");
		expect((error as ApiError).technicalDetails).toContain("custody");
	});
});

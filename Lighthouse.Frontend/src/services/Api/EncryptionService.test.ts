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

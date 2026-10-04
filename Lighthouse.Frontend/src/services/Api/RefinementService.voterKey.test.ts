import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RefinementService } from "./RefinementService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

/**
 * The Refinement tab's read tells the server which browser is asking, so it can mark the reader's own
 * votes. The key comes from what the browser keeps about its voter; a browser that keeps nothing usable
 * sends no header.
 */

const VOTER_STORAGE_KEY = "lighthouse:refinement:voter";
const VOTER_KEY_HEADER = "X-Lighthouse-Voter-Key";
const THE_KEY = "v".repeat(64);

const theKeySentWithRead = (call: number) => {
	const options = mockedAxios.get.mock.calls[call]?.[1];
	return options?.headers?.[VOTER_KEY_HEADER];
};

describe("RefinementService and the browser's voter key", () => {
	let refinementService: RefinementService;

	beforeEach(() => {
		localStorage.clear();
		mockedAxios.create.mockReturnThis();
		mockedAxios.get.mockResolvedValue({
			data: { refinementConfigured: true, workItems: [] },
		});
		refinementService = new RefinementService();
	});

	afterEach(() => {
		vi.resetAllMocks();
		localStorage.clear();
	});

	// @us-11 @slice-11 @driving_port @contract-shape:pure-function
	it("reads the tab with the key this browser keeps for its voter, and with none before it keeps one", async () => {
		await refinementService.getRefinement(7);
		localStorage.setItem(
			VOTER_STORAGE_KEY,
			JSON.stringify({ name: "Jonas Weber", key: THE_KEY }),
		);
		await refinementService.getRefinement(7);

		expect(mockedAxios.get).toHaveBeenNthCalledWith(
			2,
			"/teams/7/refinement",
			expect.objectContaining({ headers: { [VOTER_KEY_HEADER]: THE_KEY } }),
		);
		expect(theKeySentWithRead(0)).toBeUndefined();
	});

	// @us-11 @slice-11 @error @contract-shape:pure-function
	it("sends no key while what the browser keeps cannot be read, and the key once it can", async () => {
		localStorage.setItem(VOTER_STORAGE_KEY, "not json");
		await refinementService.getRefinement(7);
		localStorage.setItem(
			VOTER_STORAGE_KEY,
			JSON.stringify({ name: "Jonas Weber", key: THE_KEY }),
		);
		await refinementService.getRefinement(7);

		expect(theKeySentWithRead(0)).toBeUndefined();
		expect(theKeySentWithRead(1)).toBe(THE_KEY);
	});

	it.each([
		["nothing kept", null, undefined],
		["not JSON", "not json", undefined],
		["JSON that is not a voter", JSON.stringify(["Jonas Weber"]), undefined],
		[
			"a name without a key",
			JSON.stringify({ name: "Jonas Weber" }),
			undefined,
		],
		[
			"an empty key",
			JSON.stringify({ name: "Jonas Weber", key: "" }),
			undefined,
		],
		[
			"a name and a key",
			JSON.stringify({ name: "Jonas Weber", key: THE_KEY }),
			THE_KEY,
		],
	])(
		"with %s kept, the tab read sends a key only when it is usable",
		async (_kept, stored, expectedKey) => {
			if (stored !== null) {
				localStorage.setItem(VOTER_STORAGE_KEY, stored);
			}

			await refinementService.getRefinement(7);

			expect(theKeySentWithRead(0)).toBe(expectedKey);
		},
	);
});

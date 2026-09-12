import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataService } from "./UsageDataService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

const CONSENT_TOKEN_HEADER = "X-Lighthouse-UsageData-Token";

describe("UsageDataService", () => {
	let service: UsageDataService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		service = new UsageDataService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("asks for the state without a token when this browser holds none", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { sending: false, decision: null, willAskAgain: true },
		});

		await service.getState(null);

		expect(mockedAxios.get).toHaveBeenCalledWith("/usagedata/state", undefined);
	});

	// The header is how the server recognises this browser at all. Send the wrong name, or none,
	// and every browser looks undecided forever - it would still be told the instance's state, so
	// nothing visibly breaks while consent silently stops being remembered.
	it("names this browser by its token when it has one", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { sending: true, decision: "Granted", willAskAgain: false },
		});

		await service.getState("this-browsers-token");

		expect(mockedAxios.get).toHaveBeenCalledWith("/usagedata/state", {
			headers: { [CONSENT_TOKEN_HEADER]: "this-browsers-token" },
		});
	});

	it("returns what the server said about the instance and this browser", async () => {
		const state = { sending: true, decision: "Granted", willAskAgain: false };
		mockedAxios.get.mockResolvedValueOnce({ data: state });

		expect(await service.getState(null)).toEqual(state);
	});

	it("sends the decision that was actually made", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: { token: "minted" } });

		await service.recordDecision("declined");

		expect(mockedAxios.post).toHaveBeenCalledWith("/usagedata/consent", {
			decision: "declined",
		});
	});

	it("hands back the minted token, because it is the only copy the browser will ever get", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: { token: "only-copy" } });

		expect(await service.recordDecision("granted")).toBe("only-copy");
	});

	it("withdraws by naming the token, not by asking politely", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await service.revoke("this-browsers-token");

		expect(mockedAxios.delete).toHaveBeenCalledWith("/usagedata/consent", {
			headers: { [CONSENT_TOKEN_HEADER]: "this-browsers-token" },
		});
	});
});

import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataRouteKey } from "../../models/UsageData/UsageData";
import { UsageDataEventName, UsageDataService } from "./UsageDataService";

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
			data: { sending: false, decision: null },
		});

		await service.getState(null);

		expect(mockedAxios.get).toHaveBeenCalledWith("/usagedata/state", undefined);
	});

	// The header is how the server recognises this browser at all. Send the wrong name, or none,
	// and every browser looks undecided forever - it would still be told the instance's state, so
	// nothing visibly breaks while consent silently stops being remembered.
	it("names this browser by its token when it has one", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { sending: true, decision: "Granted" },
		});

		await service.getState("this-browsers-token");

		expect(mockedAxios.get).toHaveBeenCalledWith("/usagedata/state", {
			headers: { [CONSENT_TOKEN_HEADER]: "this-browsers-token" },
		});
	});

	it("returns what the server said about the instance and this browser", async () => {
		const state = { sending: true, decision: "Granted" };
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

	// Taking the token as an argument, exactly as the other two do, is what keeps this class from
	// knowing where the token is kept. One module owns that, and a service that reached into
	// storage itself would be a second owner of it with no way to tell them apart later.
	it("hands in a batch under the token it was given, not one it went looking for", async () => {
		mockedAxios.post.mockResolvedValueOnce({});

		await service.postEvents("this-browsers-token", [
			{
				name: "TeamTabOpened",
				route: "TeamDetail_Metrics",
				offsetMs: 1200,
				sequence: 0,
			},
		]);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/usagedata/events",
			{
				events: [
					{
						name: "TeamTabOpened",
						route: "TeamDetail_Metrics",
						offsetMs: 1200,
						sequence: 0,
					},
				],
			},
			{ headers: { [CONSENT_TOKEN_HEADER]: "this-browsers-token" } },
		);
	});

	// Both choices travel as the words the server answers with. A numbered mirror of either list
	// would still typecheck here and would still be accepted, but every event would arrive naming
	// whichever member happens to sit at that position - silently, and only for some of them.
	it("names both choices in words, because that is how the server reads them", async () => {
		mockedAxios.post.mockResolvedValueOnce({});

		await service.postEvents("this-browsers-token", [
			{
				name: UsageDataEventName.TeamTabOpened,
				route: UsageDataRouteKey.PortfolioDetail_Deliveries,
				offsetMs: 0,
				sequence: 0,
			},
		]);

		const body = mockedAxios.post.mock.calls[0][1] as {
			events: { name: unknown; route: unknown }[];
		};
		expect(body.events[0].name).toBe("TeamTabOpened");
		expect(body.events[0].route).toBe("PortfolioDetail_Deliveries");
	});
});

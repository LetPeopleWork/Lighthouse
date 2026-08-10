import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "./ApiError";
import { PortfolioService } from "./PortfolioService";
import { TeamService } from "./TeamService";
import { VersionService } from "./VersionService";
import { WorkTrackingSystemService } from "./WorkTrackingSystemService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

// Bug #5732: the SPA fallback answered API calls with index.html and a 200. axios leaves a
// body it cannot parse as a raw string, so `response.data.map(...)` threw a bare TypeError
// that no caller was prepared for — and one of them killed the whole render.
const SPA_SHELL_BODY =
	'<!doctype html><html lang="en"><body><div id="root"></div></body></html>';

describe("list endpoints given a response that is not an array", () => {
	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	const listCalls: ReadonlyArray<[string, () => Promise<unknown>]> = [
		["TeamService.getTeams", () => new TeamService().getTeams()],
		[
			"PortfolioService.getPortfolios",
			() => new PortfolioService().getPortfolios(),
		],
		[
			"WorkTrackingSystemService.getWorkTrackingSystems",
			() => new WorkTrackingSystemService().getWorkTrackingSystems(),
		],
		[
			"WorkTrackingSystemService.getConfiguredWorkTrackingSystems",
			() => new WorkTrackingSystemService().getConfiguredWorkTrackingSystems(),
		],
		[
			"VersionService.getNewReleases",
			() => new VersionService().getNewReleases(),
		],
	];

	it.each(listCalls)(
		"%s reports an ApiError, not a TypeError",
		async (_name, call) => {
			mockedAxios.get.mockResolvedValueOnce({ data: SPA_SHELL_BODY });

			await expect(call()).rejects.toBeInstanceOf(ApiError);
		},
	);

	it.each(listCalls)(
		"%s tags the failure as an invalid response",
		async (_name, call) => {
			mockedAxios.get.mockResolvedValueOnce({ data: SPA_SHELL_BODY });

			await expect(call()).rejects.toMatchObject({ code: "INVALID_RESPONSE" });
		},
	);

	it("also rejects a bare object where a list was promised", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: { message: "nope" } });

		await expect(new TeamService().getTeams()).rejects.toBeInstanceOf(ApiError);
	});
});

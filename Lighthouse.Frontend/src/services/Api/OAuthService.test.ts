import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OAuthService } from "./OAuthService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("OAuthService", () => {
	let oauthService: OAuthService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		oauthService = new OAuthService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	describe("initiateConnect", () => {
		it("posts to provider connect endpoint with connectionId and returns authorizationUrl", async () => {
			const mockResponse = {
				authorizationUrl: "https://auth.atlassian.com/authorize?state=abc",
			};
			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await oauthService.initiateConnect("jira.oauth", 42);

			expect(result.authorizationUrl).toEqual(
				"https://auth.atlassian.com/authorize?state=abc",
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				"/oauth/jira.oauth/connect",
				{ connectionId: 42 },
				expect.objectContaining({ baseURL: expect.stringContaining("/api") }),
			);
		});
	});

	describe("disconnect", () => {
		it("posts to provider disconnect endpoint with connectionId", async () => {
			mockedAxios.post.mockResolvedValueOnce({ data: undefined });

			await oauthService.disconnect("jira.oauth", 42);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				"/oauth/jira.oauth/disconnect",
				{ connectionId: 42 },
				expect.objectContaining({ baseURL: expect.stringContaining("/api") }),
			);
		});
	});

	describe("getHealth", () => {
		it("gets the OAuth health endpoint and surfaces unavailable KPIs as nulls", async () => {
			const mockResponse = {
				setupSuccessRate30d: {
					value: null,
					unavailableReason: "event_store_pending",
				},
				refreshSuccessRate7d: {
					value: 0.97,
					unavailableReason: null,
				},
				staleRefreshFailedCount24h: 2,
				staleRefreshFailedCount7d: 4,
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await oauthService.getHealth();

			expect(result.setupSuccessRate30d.value).toBeNull();
			expect(result.setupSuccessRate30d.unavailableReason).toBe(
				"event_store_pending",
			);
			expect(result.refreshSuccessRate7d.value).toBe(0.97);
			expect(result.staleRefreshFailedCount24h).toBe(2);
			expect(result.staleRefreshFailedCount7d).toBe(4);
			expect(mockedAxios.get).toHaveBeenCalledWith(
				"/oauth/health",
				expect.objectContaining({ baseURL: expect.stringContaining("/api") }),
			);
		});
	});
});

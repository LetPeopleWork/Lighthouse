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
});

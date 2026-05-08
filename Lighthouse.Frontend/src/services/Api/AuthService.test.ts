import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	AuthSessionStatus,
	CurrentUserProfileStatus,
	RuntimeAuthStatus,
} from "../../models/Auth/AuthModels";
import { AuthMode } from "../../models/Auth/AuthModels";
import { AuthService } from "./AuthService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("AuthService", () => {
	let authService: AuthService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		authService = new AuthService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	describe("getRuntimeAuthStatus", () => {
		it("should return disabled status", async () => {
			const mockResponse: RuntimeAuthStatus = {
				mode: AuthMode.Disabled,
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getRuntimeAuthStatus();

			expect(result.mode).toEqual(AuthMode.Disabled);
			expect(mockedAxios.get).toHaveBeenCalledWith("/auth/mode");
		});

		it("should return enabled status", async () => {
			const mockResponse: RuntimeAuthStatus = {
				mode: AuthMode.Enabled,
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getRuntimeAuthStatus();

			expect(result.mode).toEqual(AuthMode.Enabled);
		});

		it("should return misconfigured status with message", async () => {
			const mockResponse: RuntimeAuthStatus = {
				mode: AuthMode.Misconfigured,
				misconfigurationMessage: "Authority is required",
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getRuntimeAuthStatus();

			expect(result.mode).toEqual(AuthMode.Misconfigured);
			expect(result.misconfigurationMessage).toEqual("Authority is required");
		});

		it("should return blocked status", async () => {
			const mockResponse: RuntimeAuthStatus = {
				mode: AuthMode.Blocked,
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getRuntimeAuthStatus();

			expect(result.mode).toEqual(AuthMode.Blocked);
		});
	});

	describe("getSession", () => {
		it("should return authenticated session", async () => {
			const mockResponse: AuthSessionStatus = {
				isAuthenticated: true,
				displayName: "Test User",
				email: "test@example.com",
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getSession();

			expect(result.isAuthenticated).toBe(true);
			expect(result.displayName).toEqual("Test User");
			expect(result.email).toEqual("test@example.com");
			expect(mockedAxios.get).toHaveBeenCalledWith("/auth/session");
		});

		it("should return unauthenticated session", async () => {
			const mockResponse: AuthSessionStatus = {
				isAuthenticated: false,
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getSession();

			expect(result.isAuthenticated).toBe(false);
			expect(result.displayName).toBeUndefined();
			expect(result.email).toBeUndefined();
		});
	});

	describe("getCurrentUserProfile", () => {
		it("should return current user profile", async () => {
			const mockResponse: CurrentUserProfileStatus = {
				subject: "auth0|story-user",
				displayName: "Story User",
				email: "story.user@example.com",
			};
			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const result = await authService.getCurrentUserProfile();

			expect(result.subject).toEqual("auth0|story-user");
			expect(result.displayName).toEqual("Story User");
			expect(result.email).toEqual("story.user@example.com");
			expect(mockedAxios.get).toHaveBeenCalledWith("/auth/me");
		});
	});

	describe("getLoginUrl", () => {
		it("should return the login URL based on base URL", () => {
			const loginUrl = authService.getLoginUrl();

			expect(loginUrl).toContain("/auth/login");
		});
	});

	describe("logout", () => {
		it("should post to logout endpoint", async () => {
			mockedAxios.post.mockResolvedValueOnce({});

			await authService.logout();

			expect(mockedAxios.post).toHaveBeenCalledWith("/auth/logout");
		});
	});
});

import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AuthMode } from "../models/Auth/AuthModels";
import type { IAuthService } from "../services/Api/AuthService";
import { useAuthGuard } from "./useAuthGuard";

const createMockAuthService = (
	overrides: Partial<IAuthService> = {},
): IAuthService => ({
	getRuntimeAuthStatus: vi.fn().mockResolvedValue({ mode: AuthMode.Disabled }),
	getSession: vi.fn().mockResolvedValue({ isAuthenticated: false }),
	getLoginUrl: vi.fn().mockReturnValue("/api/auth/login"),
	logout: vi.fn().mockResolvedValue(undefined),
	...overrides,
});

describe("useAuthGuard", () => {
	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("should start in loading state", () => {
		const mockService = createMockAuthService();
		const { result } = renderHook(() => useAuthGuard(mockService));

		expect(result.current.shell).toBe("loading");
	});

	it("should resolve to anonymous when auth is disabled", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockResolvedValue({ mode: AuthMode.Disabled }),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("anonymous");
		});
	});

	it("should resolve to misconfigured when auth is misconfigured", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi.fn().mockResolvedValue({
				mode: AuthMode.Misconfigured,
				misconfigurationMessage: "Authority is required",
			}),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("misconfigured");
			expect(result.current.misconfigurationMessage).toBe(
				"Authority is required",
			);
		});
	});

	it("should resolve to login when auth is enabled and not authenticated", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockResolvedValue({ mode: AuthMode.Enabled }),
			getSession: vi.fn().mockResolvedValue({ isAuthenticated: false }),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("login");
		});
	});

	it("should resolve to authenticated when auth is enabled and session is valid", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockResolvedValue({ mode: AuthMode.Enabled }),
			getSession: vi.fn().mockResolvedValue({
				isAuthenticated: true,
				displayName: "Test User",
				email: "test@example.com",
			}),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("authenticated");
			expect(result.current.session?.displayName).toBe("Test User");
		});
	});

	it("should resolve to authenticated when auth is blocked and session is valid", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockResolvedValue({ mode: AuthMode.Blocked }),
			getSession: vi.fn().mockResolvedValue({
				isAuthenticated: true,
				displayName: "Blocked User",
			}),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("authenticated");
		});
	});

	it("should resolve to misconfigured when auth mode fetch fails", async () => {
		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockRejectedValue(new Error("Network error")),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("misconfigured");
			expect(result.current.misconfigurationMessage).toBe(
				"Unable to reach the authentication service. Please verify the server is running and the authentication configuration is correct.",
			);
		});
	});

	it("should detect session expiry on periodic check", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });

		const getSession = vi
			.fn()
			.mockResolvedValueOnce({
				isAuthenticated: true,
				displayName: "User",
			})
			.mockResolvedValueOnce({ isAuthenticated: false });

		const mockService = createMockAuthService({
			getRuntimeAuthStatus: vi
				.fn()
				.mockResolvedValue({ mode: AuthMode.Enabled }),
			getSession,
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		await waitFor(() => {
			expect(result.current.shell).toBe("authenticated");
		});

		await act(async () => {
			vi.advanceTimersByTime(60_000);
		});

		await waitFor(() => {
			expect(result.current.shell).toBe("session-expired");
		});

		vi.useRealTimers();
	});

	it("should provide login URL from auth service", async () => {
		const mockService = createMockAuthService({
			getLoginUrl: vi.fn().mockReturnValue("/api/auth/login"),
		});

		const { result } = renderHook(() => useAuthGuard(mockService));

		expect(result.current.loginUrl).toBe("/api/auth/login");
	});
});

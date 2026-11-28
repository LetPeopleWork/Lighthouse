import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../tests/MockApiServiceProvider";
import { useRbacGate } from "./useRbacGate";

const rbacService = createMockRbacService();
const mockGetAuthorizationSummary = vi.mocked(
	rbacService.getAuthorizationSummary,
);
const services = createMockApiServiceContext({ rbacService });

const renderGate = <T>(hook: () => T) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(
			QueryClientProvider,
			{ client: queryClient },
			createElement(ApiServiceContext.Provider, { value: services }, children),
		);
	return renderHook(hook, { wrapper });
};

const makeSummary = (
	overrides?: Partial<UserAuthorizationSummary>,
): UserAuthorizationSummary => ({
	isRbacEnabled: true,
	isSystemAdmin: false,
	canCreateTeam: false,
	canCreatePortfolio: false,
	adminTeamIds: [],
	adminPortfolioIds: [],
	...overrides,
});

describe("useRbacGate", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("allows system admin requirement when user is system admin", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const { result } = renderGate(() => useRbacGate({ kind: "systemAdmin" }));
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.allowed).toBe(true);
		expect(result.current.isLoading).toBe(false);
	});

	it("denies system admin requirement when user is not system admin", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: false }),
		);
		const { result } = renderGate(() => useRbacGate({ kind: "systemAdmin" }));
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.allowed).toBe(false);
		expect(result.current.isLoading).toBe(false);
	});

	it.each([
		{ adminTeamIds: [42], expected: true },
		{ adminTeamIds: [1, 2, 3], expected: false },
	])(
		"team admin gate follows rbac.isTeamAdmin(42) — adminTeamIds=$adminTeamIds expects allowed=$expected",
		async ({ adminTeamIds, expected }) => {
			mockGetAuthorizationSummary.mockResolvedValue(
				makeSummary({ isSystemAdmin: false, adminTeamIds }),
			);
			const { result } = renderGate(() =>
				useRbacGate({ kind: "teamAdmin", teamId: 42 }),
			);
			await waitFor(() => expect(result.current.isLoading).toBe(false));

			expect(result.current.allowed).toBe(expected);
		},
	);

	it.each([
		{ adminPortfolioIds: [7], expected: true },
		{ adminPortfolioIds: [1, 2, 3], expected: false },
	])(
		"portfolio admin gate follows rbac.isPortfolioAdmin(7) — adminPortfolioIds=$adminPortfolioIds expects allowed=$expected",
		async ({ adminPortfolioIds, expected }) => {
			mockGetAuthorizationSummary.mockResolvedValue(
				makeSummary({ isSystemAdmin: false, adminPortfolioIds }),
			);
			const { result } = renderGate(() =>
				useRbacGate({ kind: "portfolioAdmin", portfolioId: 7 }),
			);
			await waitFor(() => expect(result.current.isLoading).toBe(false));

			expect(result.current.allowed).toBe(expected);
		},
	);

	it("returns isLoading true while rbac is loading", () => {
		mockGetAuthorizationSummary.mockReturnValue(new Promise(() => {}));
		const { result } = renderGate(() => useRbacGate({ kind: "systemAdmin" }));

		expect(result.current.isLoading).toBe(true);
	});

	it("systemAdmin requirement reads isSystemAdmin (not isTeamAdmin) — pins case body against switch-fallthrough mutants", async () => {
		// PERMISSIVE_SUMMARY behaviour: when isRbacEnabled is false, every is*Admin
		// returns true. We intentionally set isSystemAdmin to false here so the two
		// outcomes diverge: the systemAdmin case must read isSystemAdmin (false), not
		// fall through to isTeamAdmin which would short-circuit to true on !isRbacEnabled.
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isRbacEnabled: false, isSystemAdmin: false }),
		);
		const { result } = renderGate(() => useRbacGate({ kind: "systemAdmin" }));
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.allowed).toBe(false);
	});
});

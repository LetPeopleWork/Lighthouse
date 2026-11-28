import { act, waitFor } from "@testing-library/react";
import { createElement } from "react";
import { describe, expect, it, vi } from "vitest";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../tests/renderWithQueryClient";
import { type RbacState, useRbac } from "./useRbac";

const rbacService = createMockRbacService();
const mockGetAuthorizationSummary = vi.mocked(
	rbacService.getAuthorizationSummary,
);
const services = createMockApiServiceContext({ rbacService });

const renderUseRbac = (): { readonly current: RbacState } => {
	const seen: { current?: RbacState } = {};
	const Reader = () => {
		seen.current = useRbac();
		return null;
	};
	renderWithQueryClient(
		createElement(
			ApiServiceContext.Provider,
			{ value: services },
			createElement(Reader),
		),
	);
	return {
		get current() {
			if (!seen.current) throw new Error("useRbac was never rendered");
			return seen.current;
		},
	};
};

describe("useRbac", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	const makeSummary = (
		overrides?: Partial<UserAuthorizationSummary>,
	): UserAuthorizationSummary => ({
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: false,
		canCreatePortfolio: false,
		...overrides,
	});

	it("starts with permissive defaults while loading", () => {
		mockGetAuthorizationSummary.mockReturnValue(new Promise(() => {})); // never resolves
		const result = renderUseRbac();

		expect(result.current.isLoading).toBe(true);
		expect(result.current.isSystemAdmin).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(true);
	});

	it("exposes isSystemAdmin from summary", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isSystemAdmin).toBe(true);
	});

	it("isTeamAdmin returns true for system admin regardless of team id", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(42)).toBe(true);
	});

	it("isTeamAdmin returns true when team is in adminTeamIds", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: false, adminTeamIds: [1, 5, 10] }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(5)).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(false);
	});

	it("isTeamAdmin returns true when RBAC is disabled", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isRbacEnabled: false }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(99)).toBe(true);
	});

	it("isPortfolioAdmin returns true for system admin regardless of portfolio id", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(7)).toBe(true);
	});

	it("isPortfolioAdmin returns true when portfolio is in adminPortfolioIds", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: false, adminPortfolioIds: [3, 8] }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(8)).toBe(true);
		expect(result.current.isPortfolioAdmin(1)).toBe(false);
	});

	it("isPortfolioAdmin returns true when RBAC is disabled", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isRbacEnabled: false }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(99)).toBe(true);
	});

	it("defaults to permissive on fetch failure", async () => {
		mockGetAuthorizationSummary.mockRejectedValue(new Error("network error"));
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isSystemAdmin).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(true);
		expect(result.current.isPortfolioAdmin(99)).toBe(true);
	});

	it("exposes canCreateTeam and canCreatePortfolio", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ canCreateTeam: true, canCreatePortfolio: false }),
		);
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.canCreateTeam).toBe(true);
		expect(result.current.canCreatePortfolio).toBe(false);
	});

	it("has not failed to load while loading or after a good answer", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(makeSummary());
		const result = renderUseRbac();

		expect(result.current.loadFailed).toBe(false);
		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.loadFailed).toBe(false);
	});

	it("says the load failed while still answering permissively", async () => {
		mockGetAuthorizationSummary.mockRejectedValue(new Error("network error"));
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.loadFailed).toBe(true);
		expect(result.current.isSystemAdmin).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(true);
	});

	it("retry loads again and clears the failure with the real answer", async () => {
		mockGetAuthorizationSummary
			.mockRejectedValueOnce(new Error("network error"))
			.mockResolvedValue(makeSummary({ adminTeamIds: [3] }));
		const result = renderUseRbac();
		await waitFor(() => expect(result.current.loadFailed).toBe(true));

		act(() => result.current.retry());

		expect(result.current.isLoading).toBe(true);
		expect(result.current.loadFailed).toBe(false);
		await waitFor(() => expect(result.current.isLoading).toBe(false));
		expect(result.current.loadFailed).toBe(false);
		expect(result.current.isTeamAdmin(3)).toBe(true);
		expect(result.current.isTeamAdmin(4)).toBe(false);
		expect(mockGetAuthorizationSummary).toHaveBeenCalledTimes(2);
	});
});

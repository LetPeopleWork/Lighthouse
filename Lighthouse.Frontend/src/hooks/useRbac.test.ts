import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { useRbac } from "./useRbac";

// Mock useContext to return our controlled context
const mockGetAuthorizationSummary = vi.fn();

const mockApiServiceContext = createMockApiServiceContext({
	rbacService: {
		getStatus: vi.fn(),
		getUsers: vi.fn(),
		getAuthorizationSummary: mockGetAuthorizationSummary,
		bootstrapCurrentUserAsSystemAdmin: vi.fn(),
		grantSystemAdmin: vi.fn(),
		revokeSystemAdmin: vi.fn(),
		getTeamMembers: vi.fn().mockResolvedValue([]),
		upsertTeamMember: vi.fn(),
		removeTeamMember: vi.fn(),
		getPortfolioMembers: vi.fn().mockResolvedValue([]),
		upsertPortfolioMember: vi.fn(),
		removePortfolioMember: vi.fn(),
		getGroupMappings: vi.fn().mockResolvedValue([]),
		createGroupMapping: vi.fn(),
		removeGroupMapping: vi.fn(),
		deleteUser: vi.fn(),
	},
});

vi.mock("react", async () => {
	const actual = await vi.importActual("react");
	return {
		...actual,
		useContext: () => mockApiServiceContext,
	};
});

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
		const { result } = renderHook(() => useRbac());

		expect(result.current.isLoading).toBe(true);
	});

	it("exposes isSystemAdmin from summary", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isSystemAdmin).toBe(true);
	});

	it("isTeamAdmin returns true for system admin regardless of team id", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(42)).toBe(true);
	});

	it("isTeamAdmin returns true when team is in adminTeamIds", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: false, adminTeamIds: [1, 5, 10] }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(5)).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(false);
	});

	it("isTeamAdmin returns true when RBAC is disabled", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isRbacEnabled: false }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isTeamAdmin(99)).toBe(true);
	});

	it("isPortfolioAdmin returns true for system admin regardless of portfolio id", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: true }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(7)).toBe(true);
	});

	it("isPortfolioAdmin returns true when portfolio is in adminPortfolioIds", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isSystemAdmin: false, adminPortfolioIds: [3, 8] }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(8)).toBe(true);
		expect(result.current.isPortfolioAdmin(1)).toBe(false);
	});

	it("isPortfolioAdmin returns true when RBAC is disabled", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ isRbacEnabled: false }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isPortfolioAdmin(99)).toBe(true);
	});

	it("defaults to permissive on fetch failure", async () => {
		mockGetAuthorizationSummary.mockRejectedValue(new Error("network error"));
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.isSystemAdmin).toBe(true);
		expect(result.current.isTeamAdmin(99)).toBe(true);
		expect(result.current.isPortfolioAdmin(99)).toBe(true);
	});

	it("exposes canCreateTeam and canCreatePortfolio", async () => {
		mockGetAuthorizationSummary.mockResolvedValue(
			makeSummary({ canCreateTeam: true, canCreatePortfolio: false }),
		);
		const { result } = renderHook(() => useRbac());
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.canCreateTeam).toBe(true);
		expect(result.current.canCreatePortfolio).toBe(false);
	});
});

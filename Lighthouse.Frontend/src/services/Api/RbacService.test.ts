import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	RbacGroupMapping,
	RbacScopedMemberSummary,
	RbacStatus,
	RbacUser,
	UserAuthorizationSummary,
} from "../../models/Authorization/RbacModels";
import { RbacService } from "./RbacService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("RbacService", () => {
	let subject: RbacService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		subject = new RbacService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should fetch RBAC status", async () => {
		const mockStatus: RbacStatus = {
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockStatus });

		const result = await subject.getStatus();

		expect(result).toEqual(mockStatus);
		expect(mockedAxios.get).toHaveBeenCalledWith("/authorization/status");
	});

	it("should fetch RBAC users", async () => {
		const mockUsers: RbacUser[] = [
			{
				id: 1,
				subject: "auth0|admin",
				displayName: "Admin User",
				email: "admin@example.com",
				isSystemAdmin: true,
			},
			{
				id: 2,
				subject: "auth0|viewer",
				displayName: "Viewer User",
				email: "viewer@example.com",
				isSystemAdmin: false,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockUsers });

		const result = await subject.getUsers();

		expect(result).toEqual(mockUsers);
		expect(mockedAxios.get).toHaveBeenCalledWith("/authorization/users");
	});

	it("should bootstrap current user as first system admin", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: {} });

		await subject.bootstrapCurrentUserAsSystemAdmin();

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/authorization/bootstrap/system-admin",
		);
	});

	it("should grant system admin role to user", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: {} });

		await subject.grantSystemAdmin(7);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/authorization/system-admins/7",
		);
	});

	it("should revoke system admin role from user", async () => {
		mockedAxios.delete.mockResolvedValueOnce({ data: {} });

		await subject.revokeSystemAdmin(7);

		expect(mockedAxios.delete).toHaveBeenCalledWith(
			"/authorization/system-admins/7",
		);
	});

	it("should fetch authorization summary for current user", async () => {
		const mockSummary: UserAuthorizationSummary = {
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: false,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockSummary });

		const result = await subject.getAuthorizationSummary();

		expect(result).toEqual(mockSummary);
		expect(mockedAxios.get).toHaveBeenCalledWith("/authorization/my-summary");
	});

	it("should fetch team members", async () => {
		const mockMembers: RbacScopedMemberSummary[] = [
			{
				userProfileId: 1,
				subject: "auth0|team-admin",
				displayName: "Team Admin",
				email: "team-admin@example.com",
				role: "TeamAdmin",
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockMembers });

		const result = await subject.getTeamMembers(12);

		expect(result).toEqual(mockMembers);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/authorization/teams/12/members",
		);
	});

	it("should upsert team member role", async () => {
		mockedAxios.put.mockResolvedValueOnce({ data: {} });

		await subject.upsertTeamMember(12, 7, "Viewer");

		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/authorization/teams/12/members/7",
			{ role: "Viewer" },
		);
	});

	it("should remove team member", async () => {
		mockedAxios.delete.mockResolvedValueOnce({ data: {} });

		await subject.removeTeamMember(12, 7);

		expect(mockedAxios.delete).toHaveBeenCalledWith(
			"/authorization/teams/12/members/7",
		);
	});

	it("should fetch portfolio members", async () => {
		const mockMembers: RbacScopedMemberSummary[] = [
			{
				userProfileId: 2,
				subject: "auth0|portfolio-admin",
				displayName: "Portfolio Admin",
				email: "portfolio-admin@example.com",
				role: "PortfolioAdmin",
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockMembers });

		const result = await subject.getPortfolioMembers(9);

		expect(result).toEqual(mockMembers);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/authorization/portfolios/9/members",
		);
	});

	it("should upsert portfolio member role", async () => {
		mockedAxios.put.mockResolvedValueOnce({ data: {} });

		await subject.upsertPortfolioMember(9, 11, "PortfolioAdmin");

		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/authorization/portfolios/9/members/11",
			{ role: "PortfolioAdmin" },
		);
	});

	it("should remove portfolio member", async () => {
		mockedAxios.delete.mockResolvedValueOnce({ data: {} });

		await subject.removePortfolioMember(9, 11);

		expect(mockedAxios.delete).toHaveBeenCalledWith(
			"/authorization/portfolios/9/members/11",
		);
	});

	it("should fetch RBAC group mappings", async () => {
		const mockMappings: RbacGroupMapping[] = [
			{
				id: 1,
				groupValue: "team-12-viewers",
				role: "Viewer",
				scopeType: "Team",
				scopeId: 12,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockMappings });

		const result = await subject.getGroupMappings();

		expect(result).toEqual(mockMappings);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/authorization/group-mappings",
		);
	});

	it("should fetch team-scoped group mappings", async () => {
		const mockMappings: RbacGroupMapping[] = [
			{
				id: 21,
				groupValue: "team-12-admins",
				role: "TeamAdmin",
				scopeType: "Team",
				scopeId: 12,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockMappings });

		const result = await subject.getTeamGroupMappings(12);

		expect(result).toEqual(mockMappings);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/authorization/teams/12/group-mappings",
		);
	});

	it("should fetch portfolio-scoped group mappings", async () => {
		const mockMappings: RbacGroupMapping[] = [
			{
				id: 31,
				groupValue: "portfolio-9-admins",
				role: "PortfolioAdmin",
				scopeType: "Portfolio",
				scopeId: 9,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockMappings });

		const result = await subject.getPortfolioGroupMappings(9);

		expect(result).toEqual(mockMappings);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/authorization/portfolios/9/group-mappings",
		);
	});

	it("should create RBAC group mapping", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: {} });

		await subject.createGroupMapping({
			groupValue: "portfolio-9-admins",
			role: "PortfolioAdmin",
			scopeType: "Portfolio",
			scopeId: 9,
		});

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/authorization/group-mappings",
			{
				groupValue: "portfolio-9-admins",
				role: "PortfolioAdmin",
				scopeType: "Portfolio",
				scopeId: 9,
			},
		);
	});

	it("should remove RBAC group mapping", async () => {
		mockedAxios.delete.mockResolvedValueOnce({ data: {} });

		await subject.removeGroupMapping(4);

		expect(mockedAxios.delete).toHaveBeenCalledWith(
			"/authorization/group-mappings/4",
		);
	});
});

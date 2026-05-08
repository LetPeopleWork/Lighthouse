import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	RbacStatus,
	RbacUser,
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
});

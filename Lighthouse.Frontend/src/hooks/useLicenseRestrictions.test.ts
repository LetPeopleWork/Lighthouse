import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Team } from "../models/Team/Team";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { useLicenseRestrictions } from "./useLicenseRestrictions";

// Mock the context
const mockLicensingService = {
	getLicenseStatus: vi.fn(),
	importLicense: vi.fn(),
};

const mockTeamService = {
	getTeams: vi.fn(),
	getTeam: vi.fn(),
	deleteTeam: vi.fn(),
	getTeamSettings: vi.fn(),
	validateTeamSettings: vi.fn(),
	updateTeam: vi.fn(),
	createTeam: vi.fn(),
	updateTeamData: vi.fn(),
	updateForecast: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	licensingService: mockLicensingService,
	teamService: mockTeamService,
});

// Mock useContext
vi.mock("react", async () => {
	const actual = await vi.importActual("react");
	return {
		...actual,
		useContext: () => mockApiServiceContext,
	};
});

describe("useLicenseRestrictions", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	const createTeams = (count: number): Team[] => {
		return Array.from({ length: count }, (_, i) => {
			const team = new Team();
			team.id = i + 1;
			team.name = `Team ${i + 1}`;
			return team;
		});
	};

	const neverResolvePromise = () => new Promise(() => {});

	describe("Premium License Users", () => {
		it("should allow all operations for premium users with any number of teams", async () => {
			const premiumLicense: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
			};

			const teams = createTeams(5);

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTeamService.getTeams.mockResolvedValue(teams);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.licenseStatus).toEqual(premiumLicense);
		});
	});

	describe("Non-Premium License Users", () => {
		const nonPremiumLicense: ILicenseStatus = {
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		};

		beforeEach(() => {
			mockLicensingService.getLicenseStatus.mockResolvedValue(
				nonPremiumLicense,
			);
		});

		it("should allow team creation when user has less than 3 teams", async () => {
			const teams = createTeams(2);
			mockTeamService.getTeams.mockResolvedValue(teams);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.teamCount).toBe(2);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
		});

		it("should block team creation when user has exactly 3 teams", async () => {
			const teams = createTeams(3);
			mockTeamService.getTeams.mockResolvedValue(teams);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.teamCount).toBe(3);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 3 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
		});

		it("should block all team operations when user has more than 3 teams", async () => {
			const teams = createTeams(5);
			mockTeamService.getTeams.mockResolvedValue(teams);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(false);
			expect(result.current.canUpdateTeamSettings).toBe(false);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 5 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe(
				"Free users can only update team data for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
			expect(result.current.updateTeamSettingsTooltip).toBe(
				"Free users can only update team settings for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
		});

		it("should handle API errors gracefully", async () => {
			mockLicensingService.getLicenseStatus.mockRejectedValue(
				new Error("API Error"),
			);
			mockTeamService.getTeams.mockRejectedValue(new Error("API Error"));

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.licenseStatus).toBe(null);
		});
	});

	describe("Loading State", () => {
		it("should start with loading state true", () => {
			mockLicensingService.getLicenseStatus.mockImplementation(
				neverResolvePromise,
			);
			mockTeamService.getTeams.mockImplementation(neverResolvePromise);

			const { result } = renderHook(() => useLicenseRestrictions());

			expect(result.current.isLoading).toBe(true);
		});

		it("should set loading to false after data is fetched", async () => {
			const license: ILicenseStatus = {
				hasLicense: false,
				isValid: false,
				canUsePremiumFeatures: false,
			};

			mockLicensingService.getLicenseStatus.mockResolvedValue(license);
			mockTeamService.getTeams.mockResolvedValue([]);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});
		});
	});
});

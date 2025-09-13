import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Project } from "../models/Project/Project";
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
	updateAllTeamData: vi.fn(),
	updateForecast: vi.fn(),
};

const mockProjectService = {
	getProjects: vi.fn(),
	getProject: vi.fn(),
	deleteProject: vi.fn(),
	getProjectSettings: vi.fn(),
	validateProjectSettings: vi.fn(),
	updateProject: vi.fn(),
	createProject: vi.fn(),
	updateProjectData: vi.fn(),
	refreshForecastsForProject: vi.fn(),
	refreshFeaturesForAllProjects: vi.fn(),
	refreshFeaturesForProject: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	licensingService: mockLicensingService,
	teamService: mockTeamService,
	projectService: mockProjectService,
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

	const createProjects = (count: number): Project[] => {
		return Array.from({ length: count }, (_, i) => {
			const project = new Project();
			project.id = i + 1;
			project.name = `Project ${i + 1}`;
			return project;
		});
	};

	const neverResolvePromise = () => new Promise(() => {});

	describe("Premium License Users", () => {
		it("should allow all operations for premium users with any number of teams and projects", async () => {
			const premiumLicense: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
			};

			const teams = createTeams(5);
			const projects = createProjects(2);

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(true);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(true);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.projectCount).toBe(2);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.createProjectTooltip).toBe("");
			expect(result.current.updateProjectDataTooltip).toBe("");
			expect(result.current.updateProjectSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe("");
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
			const projects = createProjects(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(true);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.teamCount).toBe(2);
			expect(result.current.projectCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.createProjectTooltip).toBe("");
			expect(result.current.updateProjectDataTooltip).toBe("");
			expect(result.current.updateProjectSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
		});

		it("should block team creation when user has exactly 3 teams", async () => {
			const teams = createTeams(3);
			const projects = createProjects(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(true);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.teamCount).toBe(3);
			expect(result.current.projectCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 3 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
		});

		it("should block all team operations when user has more than 3 teams", async () => {
			const teams = createTeams(5);
			const projects = createProjects(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(false);
			expect(result.current.canUpdateTeamSettings).toBe(false);
			expect(result.current.canCreateProject).toBe(true);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.projectCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 5 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe(
				"Free users can only update team data for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
			expect(result.current.updateTeamSettingsTooltip).toBe(
				"Free users can only update team settings for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
		});

		it("should block project creation when user has exactly 1 project", async () => {
			const teams = createTeams(0);
			const projects = createProjects(1);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(false);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.projectCount).toBe(1);
			expect(result.current.createProjectTooltip).toBe(
				"Free users can only create up to 1 project. You currently have 1 project. Please obtain a premium license to create more projects.",
			);
			expect(result.current.updateProjectDataTooltip).toBe("");
			expect(result.current.updateProjectSettingsTooltip).toBe("");
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
		});

		it("should block all project operations when user has more than 1 project", async () => {
			const teams = createTeams(0);
			const projects = createProjects(2);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockProjectService.getProjects.mockResolvedValue(projects);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(false);
			expect(result.current.canUpdateProjectData).toBe(false);
			expect(result.current.canUpdateProjectSettings).toBe(false);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.projectCount).toBe(2);
			expect(result.current.createProjectTooltip).toBe(
				"Free users can only create up to 1 project. You currently have 2 projects. Please obtain a premium license to create more projects.",
			);
			expect(result.current.updateProjectDataTooltip).toBe(
				"Free users can only update project data for up to 1 project. You currently have 2 projects. Please delete some projects or obtain a premium license.",
			);
			expect(result.current.updateProjectSettingsTooltip).toBe(
				"Free users can only update project settings for up to 1 project. You currently have 2 projects. Please delete some projects or obtain a premium license.",
			);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
		});

		it("should handle API errors gracefully", async () => {
			mockLicensingService.getLicenseStatus.mockRejectedValue(
				new Error("API Error"),
			);
			mockTeamService.getTeams.mockRejectedValue(new Error("API Error"));
			mockProjectService.getProjects.mockRejectedValue(new Error("API Error"));

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreateProject).toBe(true);
			expect(result.current.canUpdateProjectData).toBe(true);
			expect(result.current.canUpdateProjectSettings).toBe(true);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.projectCount).toBe(0);
			expect(result.current.licenseStatus).toBe(null);
			expect(result.current.canUseNewItemForecaster).toBe(true);
			expect(result.current.newItemForecasterTooltip).toBe("");
		});
	});

	describe("Loading State", () => {
		it("should start with loading state true", () => {
			mockLicensingService.getLicenseStatus.mockImplementation(
				neverResolvePromise,
			);
			mockTeamService.getTeams.mockImplementation(neverResolvePromise);
			mockProjectService.getProjects.mockImplementation(neverResolvePromise);

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
			mockProjectService.getProjects.mockResolvedValue([]);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});
		});
	});
});

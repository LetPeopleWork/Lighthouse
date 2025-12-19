import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Portfolio } from "../models/Portfolio/Portfolio";
import { Team } from "../models/Team/Team";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { useLicenseRestrictions } from "./useLicenseRestrictions";

// Mock the context
const mockLicensingService = {
	getLicenseStatus: vi.fn(),
	importLicense: vi.fn(),
	clearLicense: vi.fn(),
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

const mockPortfolioService = {
	getPortfolios: vi.fn(),
	getPortfolio: vi.fn(),
	deletePortfolio: vi.fn(),
	getPortfolioSettings: vi.fn(),
	validatePortfolioSettings: vi.fn(),
	updatePortfolio: vi.fn(),
	createPortfolio: vi.fn(),
	updatePortfolioData: vi.fn(),
	refreshForecastsForPortfolio: vi.fn(),
	refreshFeaturesForAllPortfolios: vi.fn(),
	refreshFeaturesForPortfolio: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	licensingService: mockLicensingService,
	teamService: mockTeamService,
	portfolioService: mockPortfolioService,
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

	const createPortfolios = (count: number): Portfolio[] => {
		return Array.from({ length: count }, (_, i) => {
			const portfolio = new Portfolio();
			portfolio.id = i + 1;
			portfolio.name = `Project ${i + 1}`;
			return portfolio;
		});
	};

	const neverResolvePromise = () => new Promise(() => {});

	describe("Premium License Users", () => {
		it("should allow all operations for premium users with any number of teams and Portfolios", async () => {
			const premiumLicense: ILicenseStatus = {
				hasLicense: true,
				isValid: true,
				canUsePremiumFeatures: true,
			};

			const teams = createTeams(5);
			const portfolios = createPortfolios(2);

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(true);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(true);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.portfolioCount).toBe(2);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.createPortfolioTooltip).toBe("");
			expect(result.current.updatePortfolioDataTooltip).toBe("");
			expect(result.current.updatePortfolioSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe("");
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe("");
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
			const portfolios = createPortfolios(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(false);
			expect(result.current.teamCount).toBe(2);
			expect(result.current.portfolioCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe("");
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.createPortfolioTooltip).toBe("");
			expect(result.current.updatePortfolioDataTooltip).toBe("");
			expect(result.current.updatePortfolioSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to update all teams and portfolios.",
			);
		});

		it("should block team creation when user has exactly 3 teams", async () => {
			const teams = createTeams(3);
			const portfolios = createPortfolios(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(false);
			expect(result.current.teamCount).toBe(3);
			expect(result.current.portfolioCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 3 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe("");
			expect(result.current.updateTeamSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to update all teams and portfolios.",
			);
		});

		it("should block all team operations when user has more than 3 teams", async () => {
			const teams = createTeams(5);
			const portfolios = createPortfolios(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(false);
			expect(result.current.canUpdateTeamSettings).toBe(false);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(false);
			expect(result.current.teamCount).toBe(5);
			expect(result.current.portfolioCount).toBe(0);
			expect(result.current.createTeamTooltip).toBe(
				"Free users can only create up to 3 teams. You currently have 5 teams. Please obtain a premium license to create more teams.",
			);
			expect(result.current.updateTeamDataTooltip).toBe(
				"Free users can only update team data for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
			expect(result.current.updateTeamSettingsTooltip).toBe(
				"Free users can only update team settings for up to 3 teams. You currently have 5 teams. Please delete some teams or obtain a premium license.",
			);
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to update all teams and portfolios.",
			);
		});

		it("should block Portfolio creation when user has exactly 1 Portfolio", async () => {
			const teams = createTeams(0);
			const portfolios = createPortfolios(1);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(false);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(false);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.portfolioCount).toBe(1);
			expect(result.current.createPortfolioTooltip).toBe(
				"Free users can only create up to 1 portfolio. You currently have 1 portfolio. Please obtain a premium license to create more portfolios.",
			);
			expect(result.current.updatePortfolioDataTooltip).toBe("");
			expect(result.current.updatePortfolioSettingsTooltip).toBe("");
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to update all teams and portfolios.",
			);
		});

		it("should block all portfolio operations when user has more than 1 portfolios", async () => {
			const teams = createTeams(0);
			const portfolios = createPortfolios(2);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(false);
			expect(result.current.canUpdatePortfolioData).toBe(false);
			expect(result.current.canUpdatePortfolioSettings).toBe(false);
			expect(result.current.canUseNewItemForecaster).toBe(false);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(false);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.portfolioCount).toBe(2);
			expect(result.current.createPortfolioTooltip).toBe(
				"Free users can only create up to 1 portfolio. You currently have 2 portfolio. Please obtain a premium license to create more portfolios.",
			);
			expect(result.current.updatePortfolioDataTooltip).toBe(
				"Free users can only update portfolio data for up to 1 portfolio. You currently have 2 portfolios. Please delete some portfolios or obtain a premium license.",
			);
			expect(result.current.updatePortfolioSettingsTooltip).toBe(
				"Free users can only update portfolio settings for up to 1 portfolio. You currently have 2 portfolios. Please delete some portfolios or obtain a premium license.",
			);
			expect(result.current.newItemForecasterTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to use new item forecasting.",
			);
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe(
				"This feature requires a premium license. Please obtain a premium license to update all teams and portfolios.",
			);
		});

		it("should handle API errors gracefully", async () => {
			mockLicensingService.getLicenseStatus.mockRejectedValue(
				new Error("API Error"),
			);
			mockTeamService.getTeams.mockRejectedValue(new Error("API Error"));
			mockPortfolioService.getPortfolios.mockRejectedValue(
				new Error("API Error"),
			);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canUpdateTeamSettings).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.canUpdatePortfolioSettings).toBe(true);
			expect(result.current.canUseNewItemForecaster).toBe(true);
			expect(result.current.canUpdateAllTeamsAndPortfolios).toBe(true);
			expect(result.current.teamCount).toBe(0);
			expect(result.current.portfolioCount).toBe(0);
			expect(result.current.licenseStatus).toBe(null);
			expect(result.current.newItemForecasterTooltip).toBe("");
			expect(result.current.updateAllTeamsAndPortfoliosTooltip).toBe("");
		});
	});

	describe("Loading State", () => {
		it("should start with loading state true", () => {
			mockLicensingService.getLicenseStatus.mockImplementation(
				neverResolvePromise,
			);
			mockTeamService.getTeams.mockImplementation(neverResolvePromise);
			mockPortfolioService.getPortfolios.mockImplementation(
				neverResolvePromise,
			);

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
			mockPortfolioService.getPortfolios.mockResolvedValue([]);

			const { result } = renderHook(() => useLicenseRestrictions());

			await waitFor(() => {
				expect(result.current.isLoading).toBe(false);
			});
		});
	});
});

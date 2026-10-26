import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Portfolio } from "../models/Portfolio/Portfolio";
import { Team } from "../models/Team/Team";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
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
	updateForecastsForTeamPortfolios: vi.fn(),
	getForecastFilterSchema: vi.fn(),
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
	getSize: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	licensingService: mockLicensingService,
	teamService: mockTeamService,
	portfolioService: mockPortfolioService,
});

const aWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	return ({ children }: { children: ReactNode }) =>
		createElement(
			QueryClientProvider,
			{ client: queryClient },
			createElement(
				ApiServiceContext.Provider,
				{ value: mockApiServiceContext },
				children,
			),
		);
};

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

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});

			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
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

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
		});

		it("should block team creation when user has exactly 3 teams", async () => {
			const teams = createTeams(3);
			const portfolios = createPortfolios(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
		});

		it("should block all team operations when user has more than 3 teams", async () => {
			const teams = createTeams(5);
			const portfolios = createPortfolios(0);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(false);
			expect(result.current.canCreatePortfolio).toBe(true);
			expect(result.current.canUpdatePortfolioData).toBe(true);
		});

		it("should block Portfolio creation when user has exactly 1 Portfolio", async () => {
			const teams = createTeams(0);
			const portfolios = createPortfolios(1);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(false);
			expect(result.current.canUpdatePortfolioData).toBe(true);
		});

		it("should block all portfolio operations when user has more than 1 portfolios", async () => {
			const teams = createTeams(0);
			const portfolios = createPortfolios(2);
			mockTeamService.getTeams.mockResolvedValue(teams);
			mockPortfolioService.getPortfolios.mockResolvedValue(portfolios);

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("ready");
			});

			expect(result.current.canCreateTeam).toBe(true);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(false);
			expect(result.current.canUpdatePortfolioData).toBe(false);
		});

		it("should refuse creation and keep editing allowed when the license cannot be read", async () => {
			mockLicensingService.getLicenseStatus.mockRejectedValue(
				new Error("API Error"),
			);
			mockTeamService.getTeams.mockRejectedValue(new Error("API Error"));
			mockPortfolioService.getPortfolios.mockRejectedValue(
				new Error("API Error"),
			);

			const { result } = renderHook(() => useLicenseRestrictions(), {
				wrapper: aWrapper(),
			});
			await waitFor(() => {
				expect(result.current.status).toBe("failed");
			});

			expect(result.current.canCreateTeam).toBe(false);
			expect(result.current.canUpdateTeamData).toBe(true);
			expect(result.current.canCreatePortfolio).toBe(false);
			expect(result.current.canUpdatePortfolioData).toBe(true);
			expect(result.current.licenseStatus).toBe(null);
		});
	});
});

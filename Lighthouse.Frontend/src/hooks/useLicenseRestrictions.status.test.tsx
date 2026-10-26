import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { Portfolio } from "../models/Portfolio/Portfolio";
import { Team } from "../models/Team/Team";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { ILicensingService } from "../services/Api/LicensingService";
import { deferred } from "../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockTeamService,
} from "../tests/MockApiServiceProvider";
import { useLicenseRestrictions } from "./useLicenseRestrictions";

const aFreeLicence = { canUsePremiumFeatures: false } as ILicenseStatus;
const aPremiumLicence = { canUsePremiumFeatures: true } as ILicenseStatus;

const teams = (count: number): Team[] =>
	Array.from({ length: count }, (_, index) => {
		const team = new Team();
		team.id = index + 1;
		team.name = `Team ${index + 1}`;
		return team;
	});

const portfolios = (count: number): Portfolio[] =>
	Array.from({ length: count }, (_, index) => {
		const portfolio = new Portfolio();
		portfolio.id = index + 1;
		portfolio.name = `Portfolio ${index + 1}`;
		return portfolio;
	});

/**
 * The instance as the hook reads it: the licence answer, and the Teams and Portfolios it counts. Each answer is a
 * promise the scenario chooses, so "still on its way" and "could not be read" are both expressible.
 */
const anInstance = ({
	licence = Promise.resolve(aFreeLicence),
	teamList = Promise.resolve(teams(1)),
	portfolioList = Promise.resolve(portfolios(0)),
}: {
	licence?: Promise<ILicenseStatus>;
	teamList?: Promise<Team[]>;
	portfolioList?: Promise<Portfolio[]>;
} = {}) => {
	// A refusal chosen up front is not a stray one: the hook reads it when it asks.
	for (const answer of [licence, teamList, portfolioList]) {
		answer.catch(() => undefined);
	}
	const licensingService: ILicensingService = {
		getLicenseStatus: vi.fn(() => licence),
		importLicense: vi.fn(),
		clearLicense: vi.fn(),
	};
	const teamService = {
		...createMockTeamService(),
		getTeams: vi.fn(() => teamList),
	};
	const portfolioService = {
		...createMockPortfolioService(),
		getPortfolios: vi.fn(() => portfolioList),
	};
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					licensingService,
					teamService,
					portfolioService,
				})}
			>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);

	return {
		licensingService,
		teamService,
		portfolioService,
		queryClient,
		wrapper,
	};
};

/** Several parts of one page asking at once, the way the header, the Overview and every grid do. */
const useThreeAskers = () => [
	useLicenseRestrictions(),
	useLicenseRestrictions(),
	useLicenseRestrictions(),
];

describe("Story 6406 (slice 02): creating a Team or Portfolio waits for the licence check", () => {
	it("while the licence is still on its way, Add Team and Add Portfolio are refused and editing stays allowed", () => {
		const { wrapper } = anInstance({
			licence: deferred<ILicenseStatus>().promise,
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		expect(result.current.status).toBe("loading");
		expect(result.current.canCreateTeam).toBe(false);
		expect(result.current.canCreatePortfolio).toBe(false);
		expect(result.current.canUpdateTeamData).toBe(true);
		expect(result.current.canUpdatePortfolioData).toBe(true);
	});

	it("when the licence cannot be read, Add Team and Add Portfolio are refused and editing stays allowed", async () => {
		const { wrapper } = anInstance({
			licence: Promise.reject(new Error("the network dropped")),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("failed"));
		expect(result.current.canCreateTeam).toBe(false);
		expect(result.current.canCreatePortfolio).toBe(false);
		expect(result.current.canUpdateTeamData).toBe(true);
		expect(result.current.canUpdatePortfolioData).toBe(true);
	});

	it("when the Teams cannot be counted on a free licence, creating is refused as when the licence cannot be read", async () => {
		const { wrapper } = anInstance({
			teamList: Promise.reject(new Error("the network dropped")),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("failed"));
		expect(result.current.canCreateTeam).toBe(false);
		expect(result.current.canUpdateTeamData).toBe(true);
	});

	it("once the licence and the counts are in, the check says it is ready", async () => {
		const { wrapper } = anInstance();

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("ready"));
	});

	// "Allowed" is also what the hook answers before anything has arrived, and today nothing it exposes says the counts
	// have arrived, so this waits for the check to say it is ready, which slice 02 adds.
	it("on a free licence with room left, Add Team and Add Portfolio are allowed as today", async () => {
		const { wrapper } = anInstance({
			teamList: Promise.resolve(teams(2)),
			portfolioList: Promise.resolve(portfolios(0)),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("ready"));
		expect(result.current.canCreateTeam).toBe(true);
		expect(result.current.canCreatePortfolio).toBe(true);
	});

	it("on a free licence at the limit, Add Team and Add Portfolio are refused and editing stays allowed, as today", async () => {
		const { wrapper } = anInstance({
			teamList: Promise.resolve(teams(3)),
			portfolioList: Promise.resolve(portfolios(1)),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.canCreateTeam).toBe(false));
		expect(result.current.canCreatePortfolio).toBe(false);
		expect(result.current.canUpdateTeamData).toBe(true);
		expect(result.current.canUpdatePortfolioData).toBe(true);
	});

	it("on a Premium licence everything is allowed, as today", async () => {
		const { wrapper } = anInstance({
			licence: Promise.resolve(aPremiumLicence),
			teamList: Promise.resolve(teams(12)),
			portfolioList: Promise.resolve(portfolios(5)),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.licenseStatus).not.toBeNull());
		expect(result.current.canCreateTeam).toBe(true);
		expect(result.current.canCreatePortfolio).toBe(true);
	});

	it("three parts of one page asking at once share one licence read and one count of each list", async () => {
		const { wrapper, licensingService, teamService, portfolioService } =
			anInstance();

		const { result } = renderHook(useThreeAskers, { wrapper });

		await waitFor(() =>
			expect(result.current.every((asker) => asker.status === "ready")).toBe(
				true,
			),
		);
		expect(licensingService.getLicenseStatus).toHaveBeenCalledTimes(1);
		expect(teamService.getTeams).toHaveBeenCalledTimes(1);
		expect(portfolioService.getPortfolios).toHaveBeenCalledTimes(1);
	});

	it("on a Premium licence the Teams and Portfolios are never fetched just to be counted", async () => {
		const { wrapper, teamService, portfolioService } = anInstance({
			licence: Promise.resolve(aPremiumLicence),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("ready"));
		expect(teamService.getTeams).not.toHaveBeenCalled();
		expect(portfolioService.getPortfolios).not.toHaveBeenCalled();
	});

	it("a refused read of the Teams or Portfolios list counts as an empty list, as the Overview treats it", async () => {
		const forbidden = new ApiError(403, "Forbidden");
		const { wrapper } = anInstance({
			teamList: Promise.reject(forbidden),
			portfolioList: Promise.reject(forbidden),
		});

		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });

		await waitFor(() => expect(result.current.status).toBe("ready"));
		expect(result.current.canCreateTeam).toBe(true);
		expect(result.current.canCreatePortfolio).toBe(true);
	});

	it("a background refresh that fails keeps answering from the counts already in hand", async () => {
		const { wrapper, teamService, queryClient } = anInstance({
			teamList: Promise.resolve(teams(3)),
		});
		const { result } = renderHook(() => useLicenseRestrictions(), { wrapper });
		await waitFor(() => expect(result.current.status).toBe("ready"));

		vi.mocked(teamService.getTeams).mockRejectedValueOnce(
			new Error("the network dropped"),
		);
		await act(async () => {
			await queryClient.refetchQueries({ queryKey: ["teams"] });
			// TanStack tells its observers on the next tick, so the page sees the failed refresh only after this.
			await new Promise((resolve) => setTimeout(resolve));
		});
		expect(queryClient.getQueryState(["teams"])?.status).toBe("error");
		expect(result.current.status).toBe("ready");
		expect(result.current.canCreateTeam).toBe(false);
	});
});

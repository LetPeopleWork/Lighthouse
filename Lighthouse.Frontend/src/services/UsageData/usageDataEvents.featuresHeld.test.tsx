import { act, render, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../hooks/useUsageDataConsent";
import type { IEntityReference } from "../../models/EntityReference";
import { Portfolio } from "../../models/Portfolio/Portfolio";
import { Team } from "../../models/Team/Team";
import type { IUsageDataState } from "../../models/UsageData/UsageData";
import PortfolioDetail from "../../pages/Portfolios/Detail/PortfolioDetail";
import TeamDetail from "../../pages/Teams/Detail/TeamDetail";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../tests/MockApiServiceProvider";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import type { IUsageDataService } from "../Api/UsageDataService";
import {
	DWELL_BEFORE_A_PAGE_COUNTS_MS,
	FLUSH_INTERVAL_MS,
	useUsageDataEventDetector,
} from "./usageDataEvents";
import { featuresHeldOnPage } from "./usageDataFeaturesHeld";

vi.mock("../../pages/Portfolios/Detail/PortfolioForecastView", () => ({
	default: () => null,
}));
vi.mock("../../pages/Portfolios/Detail/PortfolioMetricsView", () => ({
	default: () => null,
}));
vi.mock("../../pages/Portfolios/Detail/PortfolioDeliveryView", () => ({
	default: () => null,
}));
vi.mock("../../pages/Teams/Detail/TeamFeaturesView", () => ({
	default: () => null,
}));
vi.mock("../../pages/Teams/Detail/TeamForecastView", () => ({
	default: () => null,
}));
vi.mock("../../pages/Teams/Detail/TeamMetricsView", () => ({
	default: () => null,
}));

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const granted: IUsageDataState = {
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
	administratorDisabled: false,
};

const featuresNumbering = (count: number): IEntityReference[] =>
	Array.from({ length: count }, (_, index) => ({
		id: index + 1,
		name: `Feature ${index + 1}`,
	}));

const portfolioHolding = (count: number, id = 7): Portfolio => {
	const portfolio = new Portfolio();
	portfolio.id = id;
	portfolio.name = "Ocean Explorer";
	portfolio.features = featuresNumbering(count);
	return portfolio;
};

const teamHolding = (count: number, id = 3): Team => {
	const team = new Team();
	team.id = id;
	team.name = "Deep Divers";
	team.features = featuresNumbering(count);
	return team;
};

let navigateTo: (path: string) => void = () => {};

const Detector = () => {
	useUsageDataEventDetector();
	const navigate = useNavigate();
	navigateTo = (path) => {
		void navigate(path);
	};
	return null;
};

interface Owners {
	getPortfolio?: (id: number) => Promise<Portfolio | null>;
	getTeam?: (id: number) => Promise<Team | null>;
}

const renderTheApplicationAt = (path: string, owners: Owners) => {
	const usageDataService: IUsageDataService = {
		getState: vi.fn().mockResolvedValue(granted),
		recordDecision: vi.fn().mockResolvedValue("freshly-minted-token"),
		revoke: vi.fn().mockResolvedValue(undefined),
		acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
		postEvents: vi.fn().mockResolvedValue(undefined),
	};

	const portfolioService = createMockPortfolioService();
	portfolioService.getPortfolio = vi.fn(
		owners.getPortfolio ?? (() => Promise.resolve(null)),
	);
	portfolioService.getPortfolioSettings = vi.fn().mockResolvedValue({
		id: 7,
		name: "Ocean Explorer",
	});

	const teamService = createMockTeamService();
	teamService.getTeam = vi.fn(owners.getTeam ?? (() => Promise.resolve(null)));

	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getUpdateStatus = vi.fn().mockResolvedValue(null);

	const context = createMockApiServiceContext({
		usageDataService,
		portfolioService,
		teamService,
		updateSubscriptionService,
	});

	const Application = ({ children }: { children?: ReactNode }) => (
		<MemoryRouter initialEntries={[path]}>
			<ApiServiceContext.Provider value={context}>
				<UsageDataConsentProvider>
					<Detector />
					<Routes>
						<Route path="/portfolios/:id/:tab?" element={<PortfolioDetail />} />
						<Route path="/teams/:id/:tab?" element={<TeamDetail />} />
						<Route path="/overview" element={<div>Overview</div>} />
					</Routes>
					{children}
				</UsageDataConsentProvider>
			</ApiServiceContext.Provider>
		</MemoryRouter>
	);

	render(<Application />);
	return usageDataService;
};

const stayFor = async (milliseconds: number) => {
	await act(async () => {
		await vi.advanceTimersByTimeAsync(milliseconds);
	});
};

const hideTheTab = () => {
	vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
	act(() => {
		document.dispatchEvent(new Event("visibilitychange"));
	});
};

const answerAboutThisBrowser = async (usageDataService: IUsageDataService) => {
	await waitFor(() => expect(usageDataService.getState).toHaveBeenCalled());
};

const handedIn = (usageDataService: IUsageDataService) =>
	vi
		.mocked(usageDataService.postEvents)
		.mock.calls.flatMap(([, events]) => events);

beforeEach(() => {
	vi.useFakeTimers({ shouldAdvanceTime: true });
	localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
});

afterEach(() => {
	vi.useRealTimers();
	localStorage.clear();
	sessionStorage.clear();
	vi.restoreAllMocks();
});

describe("a tab opening carries the band of the Features its page holds", () => {
	it("takes a Portfolio's band from the Portfolio its detail page loaded", async () => {
		const usageDataService = renderTheApplicationAt("/portfolios/7/metrics", {
			getPortfolio: () => Promise.resolve(portfolioHolding(120)),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "PortfolioTabOpened",
					route: "PortfolioDetail_Metrics",
					featuresHeld: "From101To399",
				}),
			]),
		);
	});

	it("takes a Team's band from the Team its detail page loaded", async () => {
		const usageDataService = renderTheApplicationAt("/teams/3/features", {
			getTeam: () => Promise.resolve(teamHolding(3)),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "TeamTabOpened",
					route: "TeamDetail_Features",
					featuresHeld: "UpTo20",
				}),
			]),
		);
	});

	it("sends nothing about the page but its band: never the count, never which Portfolio", async () => {
		const usageDataService = renderTheApplicationAt("/portfolios/7/features", {
			getPortfolio: () => Promise.resolve(portfolioHolding(920)),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() => expect(handedIn(usageDataService)).toHaveLength(1));
		const [opening] = handedIn(usageDataService);
		expect(new Set(Object.keys(opening))).toEqual(
			new Set(["featuresHeld", "name", "offsetMs", "route", "sequence"]),
		);
		expect(opening.featuresHeld).toBe("From400");
	});

	it("waits for a Portfolio still on its way while the person stays, then goes with its band", async () => {
		let arrive: (portfolio: Portfolio) => void = () => {};
		const usageDataService = renderTheApplicationAt("/portfolios/7/features", {
			getPortfolio: () =>
				new Promise<Portfolio>((resolve) => {
					arrive = resolve;
				}),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS + FLUSH_INTERVAL_MS);
		expect(handedIn(usageDataService)).toEqual([]);

		await act(async () => {
			arrive(portfolioHolding(45));
			await Promise.resolve();
		});
		await stayFor(FLUSH_INTERVAL_MS);

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "PortfolioTabOpened",
					featuresHeld: "From21To50",
				}),
			]),
		);
	});

	it("goes as Unknown when the person leaves before the Team arrives, and is not dropped", async () => {
		const usageDataService = renderTheApplicationAt("/teams/3/metrics", {
			getTeam: () => new Promise<Team>(() => {}),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		act(() => navigateTo("/overview"));
		await stayFor(FLUSH_INTERVAL_MS);

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "TeamTabOpened",
					route: "TeamDetail_Metrics",
					featuresHeld: "Unknown",
				}),
			]),
		);
	});

	it("goes as Unknown when the Portfolio fails to load", async () => {
		const usageDataService = renderTheApplicationAt("/portfolios/7/settings", {
			getPortfolio: () => Promise.reject(new Error("the server is down")),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "PortfolioTabOpened",
					route: "PortfolioDetail_Settings",
					featuresHeld: "Unknown",
				}),
			]),
		);
	});

	it("never hands a Team the band of the Team shown before it, while the new one is on its way", async () => {
		let arrive: (team: Team) => void = () => {};
		const usageDataService = renderTheApplicationAt("/teams/3/features", {
			getTeam: (id) =>
				id === 3
					? Promise.resolve(teamHolding(920, 3))
					: new Promise<Team>((resolve) => {
							arrive = resolve;
						}),
		});
		await answerAboutThisBrowser(usageDataService);
		await waitFor(() => expect(featuresHeldOnPage("/teams/3")).toBe("From400"));

		act(() => navigateTo("/teams/4/features"));
		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS + FLUSH_INTERVAL_MS);
		expect(handedIn(usageDataService)).toEqual([]);

		await act(async () => {
			arrive(teamHolding(45, 4));
			await Promise.resolve();
		});
		await stayFor(FLUSH_INTERVAL_MS);

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "TeamTabOpened",
					featuresHeld: "From21To50",
				}),
			]),
		);
	});

	it("never hands a Portfolio the band of a Portfolio left behind whose answer comes late", async () => {
		let arriveLate: (portfolio: Portfolio) => void = () => {};
		const usageDataService = renderTheApplicationAt("/portfolios/7/features", {
			getPortfolio: (id) =>
				new Promise<Portfolio>((resolve) => {
					if (id === 7) {
						arriveLate = resolve;
					}
				}),
		});
		await answerAboutThisBrowser(usageDataService);

		act(() => navigateTo("/portfolios/8/features"));
		await act(async () => {
			arriveLate(portfolioHolding(920, 7));
			await Promise.resolve();
		});
		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "PortfolioTabOpened",
					featuresHeld: "Unknown",
				}),
			]),
		);
	});

	it("goes as Unknown when the tab is hidden while the Team is still on its way", async () => {
		const usageDataService = renderTheApplicationAt("/teams/3/forecasts", {
			getTeam: () => new Promise<Team>(() => {}),
		});
		await answerAboutThisBrowser(usageDataService);

		await stayFor(DWELL_BEFORE_A_PAGE_COUNTS_MS);
		hideTheTab();

		await waitFor(() =>
			expect(handedIn(usageDataService)).toEqual([
				expect.objectContaining({
					name: "TeamTabOpened",
					featuresHeld: "Unknown",
				}),
			]),
		);
	});
});

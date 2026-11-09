import { fireEvent, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import PortfolioDetail from "./PortfolioDetail";

vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: ({
		children,
		isLoading,
	}: {
		children: React.ReactNode;
		isLoading: boolean;
	}) => <>{!isLoading && children}</>,
}));

vi.mock("./PortfolioFeatureList", () => ({ default: () => <div /> }));
vi.mock("./InvolvedTeamsList", () => ({ default: () => <div /> }));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				[TERMINOLOGY_KEYS.TEAM]: "Team",
				[TERMINOLOGY_KEYS.TEAMS]: "Teams",
				[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
				[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
			})[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const openTheAccessTabAs = async (isSystemAdmin: boolean) => {
	const portfolio = new Portfolio();
	portfolio.id = 1;
	portfolio.name = "Ocean Explorer";
	portfolio.features = [];
	const portfolioService = createMockPortfolioService();
	portfolioService.getPortfolio = vi.fn().mockResolvedValue(portfolio);
	portfolioService.getPortfolioSettings = vi
		.fn()
		.mockResolvedValue({ id: 1, name: "Ocean Explorer" });
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin,
		canCreateTeam: false,
		canCreatePortfolio: !isSystemAdmin,
		adminPortfolioIds: isSystemAdmin ? [] : [1],
	});
	rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([]);
	rbacService.getPortfolioGroupMappings = vi.fn().mockResolvedValue([]);
	Object.assign(rbacService, {
		getPortfolioSystemWideRules: vi
			.fn()
			.mockResolvedValue([{ role: "Viewer", holder: "team-readers" }]),
	});
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				portfolioService,
				teamService: createMockTeamService(),
				rbacService,
				updateSubscriptionService: createMockUpdateSubscriptionService(),
			})}
		>
			<MemoryRouter initialEntries={["/portfolios/1"]}>
				<Routes>
					<Route path="/portfolios/:id" element={<PortfolioDetail />} />
					<Route path="/portfolios/:id/:tab" element={<PortfolioDetail />} />
				</Routes>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
	fireEvent.click(await screen.findByRole("tab", { name: "Access" }));
};

describe("The Access tab of a Portfolio", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows a Portfolio Admin which rules for all Teams and Portfolios reach the Portfolio, and offers no form to add a group", async () => {
		await openTheAccessTabAs(false);

		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
		expect(screen.getByText(/^Viewer: group team-readers$/)).toBeInTheDocument();
		expect(screen.getByText("Portfolio Group Access")).toBeInTheDocument();
		expect(screen.queryByLabelText(/^Group value$/)).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Add Group Mapping$/ }),
		).not.toBeInTheDocument();
	});

	it("offers a System Admin the form to add a group, next to the same notice", async () => {
		await openTheAccessTabAs(true);

		expect(await screen.findByLabelText(/^Group value$/)).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /^Add Group Mapping$/ }),
		).toBeInTheDocument();
		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
	});
});

import { screen } from "@testing-library/react";
import { BrowserRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTeamService,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import TeamDetail from "./TeamDetail";

vi.mock("./TeamFeaturesView", () => ({ default: () => <div /> }));
vi.mock("./TeamForecastView", () => ({ default: () => <div /> }));
vi.mock("./TeamMetricsView", () => ({ default: () => <div /> }));

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

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUpdateTeamData: true,
		updateTeamDataTooltip: "",
		canUpdateTeamSettings: true,
		updateTeamSettingsTooltip: "",
	}),
}));

vi.mock("react-router", async () => ({
	...(await vi.importActual("react-router")),
	useParams: () => ({ id: "1", tab: "access" }),
	useNavigate: () => vi.fn(),
}));

const zenith = {
	id: 1,
	name: "Team Zenith",
	features: [],
	portfolios: [],
	tags: [],
	workItemTypes: [],
	lastUpdated: new Date(),
	serviceLevelExpectationProbability: 85,
	serviceLevelExpectationRange: 7,
	systemWIPLimit: 0,
	featureWip: 1,
	useFixedDatesForThroughput: false,
	throughputStartDate: new Date(),
	throughputEndDate: new Date(),
};

const openTheAccessTabAs = (isSystemAdmin: boolean) => {
	const teamService = createMockTeamService();
	teamService.getTeam = vi.fn().mockResolvedValue(zenith);
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin,
		canCreateTeam: false,
		canCreatePortfolio: false,
		adminTeamIds: isSystemAdmin ? [] : [1],
	});
	rbacService.getTeamMembers = vi.fn().mockResolvedValue([]);
	rbacService.getTeamGroupMappings = vi.fn().mockResolvedValue([]);
	Object.assign(rbacService, {
		getTeamSystemWideRules: vi
			.fn()
			.mockResolvedValue([{ role: "Viewer", holder: "team-readers" }]),
	});
	render(
		<BrowserRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					teamService,
					rbacService,
					updateSubscriptionService: createMockUpdateSubscriptionService(),
				})}
			>
				<TeamDetail />
			</ApiServiceContext.Provider>
		</BrowserRouter>,
	);
};

describe("The Access tab of a Team", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows a Team Admin which rules for all Teams and Portfolios reach the Team, and offers no form to add a group", async () => {
		openTheAccessTabAs(false);

		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
		expect(screen.getByText("Team Group Access")).toBeInTheDocument();
		expect(screen.queryByLabelText(/^Group value$/)).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Add Group Mapping$/ }),
		).not.toBeInTheDocument();
	});

	it("offers a System Admin the form to add a group, next to the same notice", async () => {
		openTheAccessTabAs(true);

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

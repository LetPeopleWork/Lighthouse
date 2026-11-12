import { fireEvent, screen, waitFor, within } from "@testing-library/react";
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

const terms = vi.hoisted(() => ({ portfolio: "Portfolio" }));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				[TERMINOLOGY_KEYS.TEAM]: "Team",
				[TERMINOLOGY_KEYS.TEAMS]: "Teams",
				[TERMINOLOGY_KEYS.PORTFOLIO]: terms.portfolio,
				[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
			})[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const openTheAccessTabAs = (
	isSystemAdmin: boolean,
	summaryAnswer?: Promise<unknown>,
	tune?: (rbacService: ReturnType<typeof createMockRbacService>) => void,
) => {
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
	rbacService.getAuthorizationSummary = vi.fn().mockReturnValue(
		summaryAnswer ??
			Promise.resolve({
				isRbacEnabled: true,
				isSystemAdmin,
				canCreateTeam: false,
				canCreatePortfolio: !isSystemAdmin,
				adminPortfolioIds: isSystemAdmin ? [] : [1],
			}),
	);
	rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([]);
	rbacService.getPortfolioGroupMappings = vi.fn().mockResolvedValue([
		{
			id: 5,
			groupValue: "zenith-leads",
			role: "PortfolioAdmin",
			scopeType: "Portfolio",
			scopeId: 1,
		},
	]);
	Object.assign(rbacService, {
		getPortfolioSystemWideRules: vi
			.fn()
			.mockResolvedValue([{ role: "Viewer", holder: "team-readers" }]),
	});
	tune?.(rbacService);
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				portfolioService,
				teamService: createMockTeamService(),
				rbacService,
				updateSubscriptionService: createMockUpdateSubscriptionService(),
			})}
		>
			<MemoryRouter initialEntries={["/portfolios/1/access"]}>
				<Routes>
					<Route path="/portfolios/:id" element={<PortfolioDetail />} />
					<Route path="/portfolios/:id/:tab" element={<PortfolioDetail />} />
				</Routes>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
};

describe("The Access tab of a Portfolio", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		terms.portfolio = "Portfolio";
	});

	it("shows a Portfolio Admin which rules for all Teams and Portfolios reach the Portfolio, and offers no form to add a group", async () => {
		openTheAccessTabAs(false);

		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
		expect(screen.getByText("Portfolio Group Access")).toBeInTheDocument();
		expect(screen.queryByLabelText(/^Group value$/)).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Add Group Mapping$/ }),
		).not.toBeInTheDocument();
	});

	it("offers a System Admin the form to add a group as Portfolio Admin or Viewer, next to the same notice", async () => {
		openTheAccessTabAs(true);

		expect(await screen.findByLabelText(/^Group value$/)).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /^Add Group Mapping$/ }),
		).toBeInTheDocument();
		fireEvent.mouseDown(
			within(screen.getByTestId("scoped-group-role-input")).getByRole(
				"combobox",
			),
		);
		expect(
			screen.getAllByRole("option").map((option) => option.textContent),
		).toEqual(["Portfolio Admin", "Viewer"]);
		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
	});

	it.each([
		["is still being read", () => new Promise<unknown>(() => undefined)],
		["could not be read", () => Promise.reject(new Error("down"))],
	])(
		"offers no way to add or remove a group while the person's own access is checked",
		async (_when, answer) => {
			openTheAccessTabAs(false, answer());

			await waitFor(() =>
				expect(screen.getByText("zenith-leads")).toBeInTheDocument(),
			);
			expect(screen.queryByLabelText(/^Group value$/)).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: /^Add Group Mapping$/ }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: /^Remove$/ }),
			).not.toBeInTheDocument();
		},
	);

	describe("members and group mappings as they behave today", () => {
		const viewer = {
			userProfileId: 4,
			subject: "auth0|viewer",
			displayName: "Viewer User",
			email: "viewer@example.com",
			role: "Viewer" as const,
		};

		it("assigns a role to a member and reads the members again", async () => {
			let rbac: ReturnType<typeof createMockRbacService> | undefined;
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbac = rbacService;
				rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([viewer]);
				rbacService.upsertPortfolioMember = vi
					.fn()
					.mockResolvedValue(undefined);
			});

			fireEvent.click(await screen.findByTestId("assign-PortfolioAdmin-4"));

			await waitFor(() =>
				expect(rbac?.getPortfolioMembers).toHaveBeenCalledTimes(2),
			);
			expect(rbac?.upsertPortfolioMember).toHaveBeenCalledWith(
				1,
				4,
				"PortfolioAdmin",
			);
		});

		it("removes a member and reads the members again", async () => {
			let rbac: ReturnType<typeof createMockRbacService> | undefined;
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbac = rbacService;
				rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([viewer]);
				rbacService.removePortfolioMember = vi
					.fn()
					.mockResolvedValue(undefined);
			});

			fireEvent.click(await screen.findByTestId("remove-member-4"));

			await waitFor(() =>
				expect(rbac?.getPortfolioMembers).toHaveBeenCalledTimes(2),
			);
			expect(rbac?.removePortfolioMember).toHaveBeenCalledWith(1, 4);
		});

		it('shows "Failed to load portfolio members." when the members could not be loaded', async () => {
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbacService.getPortfolioMembers = vi
					.fn()
					.mockRejectedValue(new Error("down"));
			});

			expect(
				await screen.findByText("Failed to load portfolio members."),
			).toBeInTheDocument();
		});

		it('shows "Failed to load program members." when the Portfolio is called Program and the members could not be loaded', async () => {
			terms.portfolio = "Program";
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbacService.getPortfolioMembers = vi
					.fn()
					.mockRejectedValue(new Error("down"));
			});

			expect(
				await screen.findByText("Failed to load program members."),
			).toBeInTheDocument();
		});

		it('shows "Failed to update portfolio member role." when assigning a role fails, and leaves the members as they were', async () => {
			let rbac: ReturnType<typeof createMockRbacService> | undefined;
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbac = rbacService;
				rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([viewer]);
				rbacService.upsertPortfolioMember = vi
					.fn()
					.mockRejectedValue(new Error("down"));
			});

			fireEvent.click(await screen.findByTestId("assign-PortfolioAdmin-4"));

			expect(
				await screen.findByText("Failed to update portfolio member role."),
			).toBeInTheDocument();
			expect(rbac?.getPortfolioMembers).toHaveBeenCalledTimes(1);
		});

		it('shows "Failed to remove portfolio member." when removing a member fails, and leaves the members as they were', async () => {
			let rbac: ReturnType<typeof createMockRbacService> | undefined;
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbac = rbacService;
				rbacService.getPortfolioMembers = vi.fn().mockResolvedValue([viewer]);
				rbacService.removePortfolioMember = vi
					.fn()
					.mockRejectedValue(new Error("down"));
			});

			fireEvent.click(await screen.findByTestId("remove-member-4"));

			expect(
				await screen.findByText("Failed to remove portfolio member."),
			).toBeInTheDocument();
			expect(rbac?.getPortfolioMembers).toHaveBeenCalledTimes(1);
		});

		it("adds and removes a group mapping for this Portfolio", async () => {
			let rbac: ReturnType<typeof createMockRbacService> | undefined;
			openTheAccessTabAs(true, undefined, (rbacService) => {
				rbac = rbacService;
				rbacService.createGroupMapping = vi.fn().mockResolvedValue(undefined);
				rbacService.removeGroupMapping = vi.fn().mockResolvedValue(undefined);
			});

			fireEvent.change(await screen.findByLabelText(/^Group value$/), {
				target: { value: " new-group " },
			});
			fireEvent.click(
				screen.getByRole("button", { name: /^Add Group Mapping$/ }),
			);
			await waitFor(() =>
				expect(rbac?.createGroupMapping).toHaveBeenCalledWith({
					groupValue: "new-group",
					role: "PortfolioAdmin",
					scopeType: "Portfolio",
					scopeId: 1,
				}),
			);

			fireEvent.click(await screen.findByTestId("scoped-group-remove-5"));
			await waitFor(() =>
				expect(rbac?.removeGroupMapping).toHaveBeenCalledWith(5),
			);
		});
	});
});

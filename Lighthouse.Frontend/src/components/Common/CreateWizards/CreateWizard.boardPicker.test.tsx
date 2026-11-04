import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IWizardService } from "../../../services/Api/WizardService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import CreatePortfolioWizard from "./CreatePortfolioWizard";
import CreateTeamWizard from "./CreateTeamWizard";

const NO_BOARDS_FOR_THIS_SIGN_IN =
	"Jira does not offer boards to scoped API tokens or Jira Cloud (OAuth). Enter the query by hand.";

const CONNECTION_NAME = "Northwind Jira";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		status: "ready",
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: null,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

type CreateFlow = "Team" | "Portfolio";

const theConnection = (authenticationMethodKey: string) =>
	new WorkTrackingSystemConnection({
		id: 1,
		name: CONNECTION_NAME,
		workTrackingSystem: "Jira",
		options: [],
		authenticationMethodKey,
	});

const theCreateFlow = (flow: CreateFlow, authenticationMethodKey: string) => {
	const getConnections = vi
		.fn()
		.mockResolvedValue([theConnection(authenticationMethodKey)]);

	return flow === "Team" ? (
		<CreateTeamWizard
			getConnections={getConnections}
			validateTeamSettings={vi.fn().mockResolvedValue(true)}
			saveTeamSettings={vi.fn().mockResolvedValue(undefined)}
			onCancel={vi.fn()}
		/>
	) : (
		<CreatePortfolioWizard
			getConnections={getConnections}
			validatePortfolioSettings={vi.fn().mockResolvedValue(true)}
			savePortfolioSettings={vi.fn().mockResolvedValue(undefined)}
			onCancel={vi.fn()}
		/>
	);
};

const withTheApp = (
	createFlow: ReactNode,
	getBoards: IWizardService["getBoards"],
) => (
	<QueryClientProvider client={new QueryClient()}>
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					wizardService: { getBoards, getBoardInformation: vi.fn() },
				})}
			>
				<TerminologyProvider>{createFlow}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</MemoryRouter>
	</QueryClientProvider>
);

const anAdministratorChoosesTheConnection = async (
	flow: CreateFlow,
	authenticationMethodKey: string,
) => {
	const user = userEvent.setup();
	const getBoards = vi.fn().mockResolvedValue([]);
	render(withTheApp(theCreateFlow(flow, authenticationMethodKey), getBoards));

	await user.click(
		await screen.findByRole("button", { name: CONNECTION_NAME }),
	);

	return { user, getBoards };
};

describe("creating on a Jira connection whose sign-in Jira offers no boards to", () => {
	it.skip.each([
		["Team", "jira.scopedtoken"],
		["Team", "jira.oauth"],
		["Portfolio", "jira.scopedtoken"],
		["Portfolio", "jira.oauth"],
	] as const)(
		"a %s on %s goes straight to Configure, with the picker disabled and the reason on it",
		async (flow, authenticationMethodKey) => {
			const { user, getBoards } = await anAdministratorChoosesTheConnection(
				flow,
				authenticationMethodKey,
			);

			expect(
				await screen.findByRole("button", { name: "Next" }),
			).toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "Configure Manually" }),
			).not.toBeInTheDocument();

			const picker = screen.getByRole("button", { name: "Select Jira Board" });
			expect(picker).toBeDisabled();

			await user.hover(picker.parentElement ?? picker);
			expect((await screen.findByRole("tooltip")).textContent).toBe(
				NO_BOARDS_FOR_THIS_SIGN_IN,
			);

			await userEvent.click(picker, { pointerEventsCheck: 0 });
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			expect(getBoards).not.toHaveBeenCalled();
		},
	);

	it.skip.each(["Team", "Portfolio"] as const)(
		"going back from Configure on a %s returns to choosing the connection",
		async (flow) => {
			const { user } = await anAdministratorChoosesTheConnection(
				flow,
				"jira.scopedtoken",
			);

			await screen.findByRole("button", { name: "Next" });
			await user.click(screen.getByRole("button", { name: "Back" }));

			expect(
				await screen.findByRole("button", { name: CONNECTION_NAME }),
			).toBeInTheDocument();
		},
	);

	it.skip.each([
		["Team", "jira.cloud"],
		["Team", "jira.datacenter"],
		["Portfolio", "jira.cloud"],
		["Portfolio", "jira.datacenter"],
	] as const)(
		"a %s on %s still offers the picker on the Load Data step",
		async (flow, authenticationMethodKey) => {
			const { user, getBoards } = await anAdministratorChoosesTheConnection(
				flow,
				authenticationMethodKey,
			);

			expect(
				await screen.findByRole("button", { name: "Configure Manually" }),
			).toBeInTheDocument();

			await user.click(
				screen.getByRole("button", { name: "Select Jira Board" }),
			);

			await waitFor(() => expect(getBoards).toHaveBeenCalledOnce());
		},
	);
});

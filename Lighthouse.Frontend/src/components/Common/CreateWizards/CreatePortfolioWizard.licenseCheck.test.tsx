import { createTheme, ThemeProvider } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import type { DataRetrievalWizardProps } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { getWizardsForSystem } from "../../DataRetrievalWizards";
import CreatePortfolioWizard from "./CreatePortfolioWizard";

const COULD_NOT_CHECK =
	"Couldn't check your license. Reload the page to try again.";

/** What the licence check answers in a scenario; the hook's own tests cover how it gets there. */
const theCheck = vi.hoisted(() => ({
	current: {
		status: "ready" as LicenseCheckStatus,
		canCreatePortfolio: true,
	},
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		...theCheck.current,
		canCreateTeam: true,
		canUpdateTeamData: true,
		canUpdatePortfolioData: true,
		licenseStatus: null,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("../../DataRetrievalWizards", () => ({
	getWizardsForSystem: vi.fn(),
}));

/** A board-picking helper that hands back a complete board at once, which takes the wizard straight to its last step. */
const PickBoard: React.FC<DataRetrievalWizardProps> = ({ onComplete }) => (
	<button
		type="button"
		onClick={() =>
			onComplete({
				dataRetrievalValue: "project = NW",
				workItemTypes: ["Story"],
				toDoStates: ["New"],
				doingStates: ["Active"],
				doneStates: ["Closed"],
			})
		}
	>
		Finish picking
	</button>
);

// Tooltips open the moment they are hovered, so "nothing opened" is read at once rather than after a guessed wait.
const tooltipsOpenAtOnce = createTheme({
	components: {
		MuiTooltip: {
			defaultProps: {
				enterDelay: 0,
				enterNextDelay: 0,
				leaveDelay: 0,
				slotProps: { transition: { timeout: 0 } },
			},
		},
	},
});

const givenTheLicenceCheck = (
	status: LicenseCheckStatus,
	createAllowed: boolean,
) => {
	theCheck.current = { status, canCreatePortfolio: createAllowed };
};

/** An administrator who has picked the only connection, skipped the helper and reached the last step, with a name filled in. */
const anAdminReachesTheLastStep = async (): Promise<HTMLElement> => {
	const user = userEvent.setup();
	vi.mocked(getWizardsForSystem).mockReturnValue([
		{
			id: "test.board",
			name: "Pick board",
			applicableSystemTypes: ["Jira"],
			applicableSettingsContexts: ["portfolio"],
			component: PickBoard,
		},
	]);
	const connection = new WorkTrackingSystemConnection({
		id: 1,
		name: "Northwind Jira",
		workTrackingSystem: "Jira",
		options: [],
	});

	render(
		<QueryClientProvider client={new QueryClient()}>
			<ThemeProvider theme={tooltipsOpenAtOnce}>
				<MemoryRouter>
					<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
						<TerminologyProvider>
							<CreatePortfolioWizard
								getConnections={vi.fn().mockResolvedValue([connection])}
								validatePortfolioSettings={vi.fn().mockResolvedValue(true)}
								savePortfolioSettings={vi.fn().mockResolvedValue(undefined)}
								onCancel={vi.fn()}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</MemoryRouter>
			</ThemeProvider>
		</QueryClientProvider>,
	);

	await user.click(
		await screen.findByRole("button", { name: "Northwind Jira" }),
	);
	await user.click(await screen.findByRole("button", { name: "Pick board" }));
	await user.click(
		await screen.findByRole("button", { name: "Finish picking" }),
	);
	await screen.findByLabelText("Portfolio Name");
	return screen.findByRole("button", { name: "Create" });
};

const hoveringShowsNothing = async (action: HTMLElement) => {
	const user = userEvent.setup();
	await user.hover(action.parentElement ?? action);
	expect(screen.queryByRole("tooltip")).toBeNull();
	await user.unhover(action.parentElement ?? action);
};

const hoveringShows = async (action: HTMLElement, reason: string) => {
	const user = userEvent.setup();
	await user.hover(action.parentElement ?? action);
	expect(screen.getByRole("tooltip")).toHaveTextContent(reason);
	await user.unhover(action.parentElement ?? action);
	await waitFor(() => expect(screen.queryByRole("tooltip")).toBeNull());
};

describe("Story 6406 (slice 02): the create-Portfolio wizard's Create follows the licence check", () => {
	it("while the licence is checked, Create is disabled and says nothing", async () => {
		givenTheLicenceCheck("loading", false);

		const create = await anAdminReachesTheLastStep();

		expect(create).toBeDisabled();
		await hoveringShowsNothing(create);
	});

	it("when the licence cannot be checked, Create is disabled and says why", async () => {
		givenTheLicenceCheck("failed", false);

		const create = await anAdminReachesTheLastStep();

		expect(create).toBeDisabled();
		await hoveringShows(create, COULD_NOT_CHECK);
	});

	it("once the licence is checked and there is room, Create is enabled", async () => {
		givenTheLicenceCheck("ready", true);

		const create = await anAdminReachesTheLastStep();

		expect(create).toBeEnabled();
	});

	it("at the free Portfolio limit, Create is disabled and says why", async () => {
		givenTheLicenceCheck("ready", false);

		const create = await anAdminReachesTheLastStep();

		expect(create).toBeDisabled();
		await hoveringShows(create, "Free users can only create up to 1 portfolio");
	});
});

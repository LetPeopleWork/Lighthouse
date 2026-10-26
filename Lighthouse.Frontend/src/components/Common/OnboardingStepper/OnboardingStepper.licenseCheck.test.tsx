import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import OnboardingStepper from "./OnboardingStepper";

const COULD_NOT_CHECK =
	"Couldn't check your license. Reload the page to try again.";

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

const aFailedCheckOnTheStep = (progress: {
	hasConnections: boolean;
	hasTeams: boolean;
}) =>
	render(
		<ThemeProvider theme={tooltipsOpenAtOnce}>
			<MemoryRouter>
				<OnboardingStepper
					{...progress}
					hasPortfolios={false}
					canCreateTeam={false}
					canCreatePortfolio={false}
					licenseCheckStatus="failed"
					teamTerm="Team"
					portfolioTerm="Portfolio"
					connectionTerm="Connection"
				/>
			</MemoryRouter>
		</ThemeProvider>,
	);

const hoverTheCta = async () => {
	const cta = screen.getByTestId("onboarding-cta");
	await userEvent.setup().hover(cta.parentElement ?? cta);
};

describe("Story 6406 (slice 02): the Get Started panel blames the license check only where the license gates the step", () => {
	it("the always-open Add Connection step shows no license reason on a failed check", async () => {
		aFailedCheckOnTheStep({ hasConnections: false, hasTeams: false });

		await hoverTheCta();

		expect(screen.queryByRole("tooltip")).toBeNull();
	});

	it("the Add Team step says the license could not be checked", async () => {
		aFailedCheckOnTheStep({ hasConnections: true, hasTeams: false });

		await hoverTheCta();

		expect(screen.getByRole("tooltip")).toHaveTextContent(COULD_NOT_CHECK);
	});

	it("the Add Portfolio step says the license could not be checked", async () => {
		aFailedCheckOnTheStep({ hasConnections: true, hasTeams: true });

		await hoverTheCta();

		expect(screen.getByRole("tooltip")).toHaveTextContent(COULD_NOT_CHECK);
	});
});

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import OnboardingStepper from "./OnboardingStepper";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

const defaultProps = {
	hasConnections: false,
	hasTeams: false,
	hasPortfolios: false,
	canCreateTeam: true,
	canCreatePortfolio: true,
	teamTerm: "Team",
	portfolioTerm: "Portfolio",
};

describe("OnboardingStepper", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Visibility", () => {
		it("does not render when fully onboarded", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
						hasPortfolios={true}
					/>
				</MemoryRouter>,
			);
			expect(
				screen.queryByTestId("onboarding-stepper"),
			).not.toBeInTheDocument();
		});

		it("renders when no connections exist", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} />
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-stepper")).toBeInTheDocument();
		});

		it("renders when connections exist but no teams", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} hasConnections={true} />
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-stepper")).toBeInTheDocument();
		});

		it("renders when connections and teams exist but no portfolios", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-stepper")).toBeInTheDocument();
		});

		it("shows Get Started header when onboarding is incomplete", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} />
				</MemoryRouter>,
			);
			expect(screen.getByText("Get Started")).toBeInTheDocument();
		});
	});

	describe("CTA label", () => {
		it("shows Add Connection when no connections exist", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} />
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toHaveTextContent(
				"Add Connection",
			);
		});

		it("shows Add Team CTA when connections exist but no teams", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} hasConnections={true} />
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toHaveTextContent(
				"Add Team",
			);
		});

		it("shows Add Portfolio CTA when connections and teams exist but no portfolios", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toHaveTextContent(
				"Add Portfolio",
			);
		});

		it("uses custom teamTerm in CTA label", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						teamTerm="Squad"
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toHaveTextContent(
				"Add Squad",
			);
		});

		it("uses custom portfolioTerm in CTA label", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
						portfolioTerm="Program"
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toHaveTextContent(
				"Add Program",
			);
		});
	});

	describe("CTA disabled state", () => {
		it("Add Connection CTA is always enabled", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} />
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).not.toBeDisabled();
		});

		it("Add Team CTA is disabled when canCreateTeam is false", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						canCreateTeam={false}
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toBeDisabled();
		});

		it("Add Portfolio CTA is disabled when canCreatePortfolio is false", () => {
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
						canCreatePortfolio={false}
					/>
				</MemoryRouter>,
			);
			expect(screen.getByTestId("onboarding-cta")).toBeDisabled();
		});
	});

	describe("Navigation", () => {
		it("navigates to /connections/new when Add Connection CTA is clicked", async () => {
			const user = userEvent.setup();
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} />
				</MemoryRouter>,
			);
			await user.click(screen.getByTestId("onboarding-cta"));
			expect(mockNavigate).toHaveBeenCalledWith("/connections/new");
		});

		it("navigates to /teams/new when Add Team CTA is clicked", async () => {
			const user = userEvent.setup();
			render(
				<MemoryRouter>
					<OnboardingStepper {...defaultProps} hasConnections={true} />
				</MemoryRouter>,
			);
			await user.click(screen.getByTestId("onboarding-cta"));
			expect(mockNavigate).toHaveBeenCalledWith("/teams/new");
		});

		it("navigates to /portfolios/new when Add Portfolio CTA is clicked", async () => {
			const user = userEvent.setup();
			render(
				<MemoryRouter>
					<OnboardingStepper
						{...defaultProps}
						hasConnections={true}
						hasTeams={true}
					/>
				</MemoryRouter>,
			);
			await user.click(screen.getByTestId("onboarding-cta"));
			expect(mockNavigate).toHaveBeenCalledWith("/portfolios/new");
		});
	});
});

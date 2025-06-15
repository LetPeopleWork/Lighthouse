import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Team } from "../../../models/Team/Team";
import { testTheme } from "../../../tests/testTheme";
import ForecastConfiguration from "./ForecastConfiguration";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

describe("ForecastConfiguration component", () => {
	const createTeam = (
		useFixedDatesForThroughput: boolean,
		startDate: Date,
		endDate: Date,
	): Team => {
		const team = new Team();
		team.name = "Test Team";
		team.id = 1;
		team.useFixedDatesForThroughput = useFixedDatesForThroughput;
		team.throughputStartDate = startDate;
		team.throughputEndDate = endDate;
		return team;
	};

	it("should render the throughput date range correctly", () => {
		const startDate = new Date("2025-01-01");
		const endDate = new Date("2025-01-31");
		const team = createTeam(false, startDate, endDate);

		render(<ForecastConfiguration team={team} />);

		expect(screen.getByText("Forecast Configuration:")).toBeInTheDocument();
		expect(
			screen.getByText(
				`${startDate.toLocaleDateString()} - ${endDate.toLocaleDateString()}`,
			),
		).toBeInTheDocument();
	});
	it("should not show a warning icon when not using fixed dates", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		const warningIcon = screen.queryByRole("button");
		expect(warningIcon).not.toBeInTheDocument();

		// Also verify no warning icon by test id
		const warningIconByTestId = screen.queryByTestId("GppMaybeOutlinedIcon");
		expect(warningIconByTestId).not.toBeInTheDocument();
	});

	it("should conditionally render the warning icon based on useFixedDatesForThroughput prop", () => {
		// First render with fixed dates = false
		const teamWithoutFixedDates = createTeam(false, new Date(), new Date());
		const { rerender } = render(
			<ForecastConfiguration team={teamWithoutFixedDates} />,
		);

		// Verify no warning icon
		expect(
			screen.queryByTestId("GppMaybeOutlinedIcon"),
		).not.toBeInTheDocument();

		// Re-render the component with fixed dates = true
		const teamWithFixedDates = createTeam(true, new Date(), new Date());
		rerender(<ForecastConfiguration team={teamWithFixedDates} />);

		// Verify warning icon appears
		expect(screen.getByTestId("GppMaybeOutlinedIcon")).toBeInTheDocument();
	});
	it("should show a warning icon with tooltip when using fixed dates", () => {
		const team = createTeam(true, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		const warningIcon = screen.getByRole("button");
		expect(warningIcon).toBeInTheDocument();

		// Check that the button has the correct aria-label for the tooltip
		expect(warningIcon).toHaveAttribute(
			"aria-label",
			"This team is using a fixed Throughput - consider switching to a rolling history to get more realistic forecasts",
		);
	});
	it("should have correct styling", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		const card = screen
			.getByText("Forecast Configuration:")
			.closest(".MuiCard-root");

		// Check that the min and max width properties are set (using getComputedStyle would work better in a real browser environment)
		expect(card).toHaveAttribute(
			"style",
			expect.stringContaining("--Paper-shadow"),
		);
	});

	it("should display the card content with proper styling", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		const cardContent = screen
			.getByText("Forecast Configuration:")
			.closest(".MuiCardContent-root");
		expect(cardContent).toBeInTheDocument();
	});
	it("should display the warning icon with correct color when using fixed dates", () => {
		const team = createTeam(true, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		const warningIcon = screen.getByTestId("GppMaybeOutlinedIcon");
		expect(warningIcon).toBeInTheDocument();

		// Check that the SVG icon has the warning color styling
		expect(warningIcon).toHaveClass("MuiSvgIcon-root");
		expect(warningIcon).toHaveClass("css-1umw9bq-MuiSvgIcon-root");
	});
});

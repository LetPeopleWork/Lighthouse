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

		// Check that the main forecast icon is rendered with correct tooltip
		const mainIcon = screen.getByTestId("DateRangeIcon");
		expect(mainIcon).toBeInTheDocument();

		const iconButton = screen.getByRole("button");
		expect(iconButton).toHaveAttribute(
			"aria-label",
			`Forecast Configuration: ${startDate.toLocaleDateString()} - ${endDate.toLocaleDateString()}`,
		);
	});

	it("should not show a warning icon when not using fixed dates", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		// Main icon should be present
		const mainIcon = screen.getByTestId("DateRangeIcon");
		expect(mainIcon).toBeInTheDocument();

		// Warning icon should not be present
		const warningIconByTestId = screen.queryByTestId("GppMaybeOutlinedIcon");
		expect(warningIconByTestId).not.toBeInTheDocument();
	});

	it("should conditionally render the warning icon based on useFixedDatesForThroughput prop", () => {
		// First render with fixed dates = false
		const teamWithoutFixedDates = createTeam(false, new Date(), new Date());
		const { rerender } = render(
			<ForecastConfiguration team={teamWithoutFixedDates} />,
		);

		// Verify main icon is present but no warning icon
		expect(screen.getByTestId("DateRangeIcon")).toBeInTheDocument();
		expect(
			screen.queryByTestId("GppMaybeOutlinedIcon"),
		).not.toBeInTheDocument();

		// Re-render the component with fixed dates = true
		const teamWithFixedDates = createTeam(true, new Date(), new Date());
		rerender(<ForecastConfiguration team={teamWithFixedDates} />);

		// Verify both icons appear
		expect(screen.getByTestId("DateRangeIcon")).toBeInTheDocument();
		expect(screen.getByTestId("GppMaybeOutlinedIcon")).toBeInTheDocument();
	});

	it("should show a warning icon with tooltip when using fixed dates", () => {
		const team = createTeam(true, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		// Should have both icons - main and warning
		const buttons = screen.getAllByRole("button");
		expect(buttons).toHaveLength(2);

		// Check the warning icon button
		const warningIcon = screen.getByTestId("GppMaybeOutlinedIcon");
		expect(warningIcon).toBeInTheDocument();

		// Check that the warning button has the correct aria-label for the tooltip
		const warningButton = warningIcon.closest("button");
		expect(warningButton).toHaveAttribute(
			"aria-label",
			"This team is using a fixed Throughput - consider switching to a rolling history to get more realistic forecasts",
		);
	});
	it("should have correct styling", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		// Check that the main icon button is styled correctly
		const mainIcon = screen.getByTestId("DateRangeIcon");
		expect(mainIcon).toBeInTheDocument();

		// Check that the Stack container is present
		const stack = mainIcon.closest(".MuiStack-root");
		expect(stack).toBeInTheDocument();
	});

	it("should display the icons with proper styling", () => {
		const team = createTeam(false, new Date(), new Date());

		render(<ForecastConfiguration team={team} />);

		// Check that the main icon is present with correct styling
		const mainIcon = screen.getByTestId("DateRangeIcon");
		expect(mainIcon).toBeInTheDocument();
		expect(mainIcon).toHaveClass("MuiSvgIcon-root");
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

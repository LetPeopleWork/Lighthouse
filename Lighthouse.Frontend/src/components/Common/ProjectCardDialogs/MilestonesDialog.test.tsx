import { ThemeProvider, createTheme } from "@mui/material/styles";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { type IMilestone, Milestone } from "../../../models/Project/Milestone";
import MilestonesDialog from "./MilestonesDialog";

// Create a proper theme for testing
const theme = createTheme({
	palette: {
		mode: "light",
		grey: {
			100: "#f5f5f5",
			800: "#424242",
			500: "#9e9e9e",
		},
		primary: {
			main: "#1976d2",
		},
		success: {
			main: "#4caf50",
		},
		warning: {
			main: "#ff9800",
		},
		error: {
			main: "#f44336",
		},
		text: {
			primary: "#000000",
			secondary: "#666666",
		},
	},
});

// Wrapper for providing theme
const renderWithTheme = (ui: React.ReactElement) => {
	return render(<ThemeProvider theme={theme}>{ui}</ThemeProvider>);
};

describe("MilestonesDialog component", () => {
	// Mock data
	const projectName = "Test Project";
	const mockMilestones: IMilestone[] = [
		(() => {
			const milestone = new Milestone();
			milestone.id = 1;
			milestone.name = "Milestone 1";
			milestone.date = new Date("2025-08-01");
			return milestone;
		})(),
		(() => {
			const milestone = new Milestone();
			milestone.id = 2;
			milestone.name = "Milestone 2";
			milestone.date = new Date("2025-05-01"); // Past date (current date is May 4, 2025)
			return milestone;
		})(),
	];

	const mockMilestoneLikelihoods = {
		1: 90, // High likelihood
		2: 75, // Medium likelihood
	};

	const mockOnClose = vi.fn();

	// Mock the LocalDateTimeDisplay component
	vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
		default: ({ utcDate }: { utcDate: Date }) => (
			<span data-testid="local-date-time-display">{utcDate.toISOString()}</span>
		),
	}));

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly when open", () => {
		renderWithTheme(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				milestoneLikelihoods={mockMilestoneLikelihoods}
			/>,
		);

		// Check if dialog title is rendered with correct project name and milestone count
		expect(
			screen.getByText(`${projectName}: Milestones (2)`),
		).toBeInTheDocument();

		// Check if both milestones are rendered
		expect(screen.getByText("Milestone 1")).toBeInTheDocument();
		expect(screen.getByText("Milestone 2")).toBeInTheDocument();

		// Check if likelihood chips are rendered
		expect(screen.getByText("90% Likely")).toBeInTheDocument();

		// Check if dates are rendered
		expect(screen.getAllByTestId("local-date-time-display")).toHaveLength(2);
	});

	it("doesn't render when not open", () => {
		renderWithTheme(
			<MilestonesDialog
				open={false}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				milestoneLikelihoods={mockMilestoneLikelihoods}
			/>,
		);

		// Dialog shouldn't be in the document
		expect(
			screen.queryByText(`${projectName}: Milestones (2)`),
		).not.toBeInTheDocument();
	});

	it("calls onClose when close button is clicked", () => {
		renderWithTheme(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				milestoneLikelihoods={mockMilestoneLikelihoods}
			/>,
		);

		// Find and click the close button
		const closeButton = screen.getByLabelText("close");
		fireEvent.click(closeButton);

		// Check if onClose was called
		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("renders message when no milestones are present", () => {
		renderWithTheme(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={[]}
				milestoneLikelihoods={{}}
			/>,
		);

		// Check if the no milestones message is displayed
		expect(screen.getByText("No milestones defined")).toBeInTheDocument();
	});

	it("applies different styling to past milestones", () => {
		// Create a controlled environment where we know one milestone is past
		const today = new Date("2025-05-04"); // May 4, 2025
		vi.setSystemTime(today);

		renderWithTheme(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				milestoneLikelihoods={mockMilestoneLikelihoods}
			/>,
		);

		// The second milestone (May 1, 2025) should be rendered with past due text
		expect(screen.getByText("Was due on:")).toBeInTheDocument();

		// The first milestone (Aug 1, 2025) should be rendered with future due text
		expect(screen.getByText("Due on:")).toBeInTheDocument();

		// Restore the real time
		vi.useRealTimers();
	});
});

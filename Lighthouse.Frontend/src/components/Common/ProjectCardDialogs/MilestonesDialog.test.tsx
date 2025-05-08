import { ThemeProvider, createTheme } from "@mui/material/styles";
import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
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

// Wrapper for providing theme and router
const renderWithProviders = (ui: React.ReactElement) => {
	return render(
		<BrowserRouter>
			<ThemeProvider theme={theme}>{ui}</ThemeProvider>
		</BrowserRouter>,
	);
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

	// Mock features with methods for milestone likelihoods
	const createMockFeature = (
		id: number,
		name: string,
		likelihoods: Record<number, number>,
		remainingWork: number,
	): IFeature => {
		const feature = new Feature();
		feature.id = id;
		feature.name = name;
		feature.workItemReference = `F-${id}`;
		feature.url = `/features/${id}`;
		feature.stateCategory = "Doing";

		// Mock the getMilestoneLikelihood method
		feature.getMilestoneLikelihood = (milestoneId: number) =>
			likelihoods[milestoneId] || 0;

		// Mock the getRemainingWorkForFeature method
		feature.getRemainingWorkForFeature = () => remainingWork;

		return feature;
	};

	const mockFeatures: IFeature[] = [
		createMockFeature(101, "Feature A", { 1: 90, 2: 70 }, 5),
		createMockFeature(102, "Feature B", { 1: 85, 2: 60 }, 3),
		createMockFeature(103, "Feature C", { 1: 95, 2: 75 }, 8),
	];

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

	it("renders correctly when open with no specific milestone selected", () => {
		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={null}
				features={mockFeatures}
			/>,
		);

		// Check if dialog title is rendered correctly
		expect(screen.getByText(`${projectName}: Milestones`)).toBeInTheDocument();

		// Check that "No milestone selected" message is displayed
		expect(screen.getByText("No milestone selected")).toBeInTheDocument();
	});

	it("renders specific milestone view when a milestone is selected", () => {
		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={1}
				features={mockFeatures}
			/>,
		);

		// Check if dialog title is rendered correctly for selected milestone
		expect(
			screen.getByText(`${projectName}: Milestone 1 Milestone`),
		).toBeInTheDocument();

		// Check if milestone name is displayed
		expect(screen.getByText("Milestone 1")).toBeInTheDocument();

		// Check if features for this milestone are shown
		expect(screen.getByText("Feature A")).toBeInTheDocument();
		expect(screen.getByText("Feature B")).toBeInTheDocument();
		expect(screen.getByText("Feature C")).toBeInTheDocument();

		// Check if likelihood chip is rendered for milestone
		expect(screen.getByText("85% Likely")).toBeInTheDocument();

		// Check if remaining work is displayed
		expect(screen.getByText("5 items remaining")).toBeInTheDocument();
		expect(screen.getByText("3 items remaining")).toBeInTheDocument();
		expect(screen.getByText("8 items remaining")).toBeInTheDocument();
	});

	it("doesn't render when not open", () => {
		renderWithProviders(
			<MilestonesDialog
				open={false}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={null}
				features={mockFeatures}
			/>,
		);

		// Dialog shouldn't be in the document
		expect(
			screen.queryByText(`${projectName}: Milestones`),
		).not.toBeInTheDocument();
	});

	it("calls onClose when close button is clicked", () => {
		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={null}
				features={mockFeatures}
			/>,
		);

		// Find and click the close button
		const closeButton = screen.getByLabelText("close");
		fireEvent.click(closeButton);

		// Check if onClose was called
		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("renders message when no features are associated with a milestone", () => {
		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={1}
				features={[]}
			/>,
		);

		// Check if the no features message is displayed
		expect(
			screen.getByText("No features associated with this milestone"),
		).toBeInTheDocument();
	});

	it("filters out 'Done' features from displaying", () => {
		// Create a feature with Done state
		const doneFeature = createMockFeature(104, "Done Feature", { 1: 100 }, 0);
		doneFeature.stateCategory = "Done";

		const featuresWithDone = [...mockFeatures, doneFeature];

		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={1}
				features={featuresWithDone}
			/>,
		);

		// The done feature should not be in the document
		expect(screen.queryByText("Done Feature")).not.toBeInTheDocument();
	});

	it("applies different styling to past milestones", () => {
		// Create a controlled environment where we know one milestone is past
		const today = new Date("2025-05-04"); // May 4, 2025
		vi.setSystemTime(today);

		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={2} // Past milestone
				features={mockFeatures}
			/>,
		);

		// The selected milestone (May 1, 2025) should be rendered with past due text
		expect(screen.getByText("Was due on:")).toBeInTheDocument();

		// Restore the real time
		vi.useRealTimers();
	});

	it("doesn't show likelihood for past milestones", () => {
		// Create a controlled environment where we know one milestone is past
		const today = new Date("2025-05-04"); // May 4, 2025
		vi.setSystemTime(today);

		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={2} // Past milestone
				features={mockFeatures}
			/>,
		);

		// No likelihood chip should be shown for past milestone
		expect(screen.queryByText(/\d+% Likely/)).not.toBeInTheDocument();

		// Restore the real time
		vi.useRealTimers();
	});

	it("renders feature likelihood in appropriate colors", () => {
		const customFeatures = [
			createMockFeature(201, "High Likelihood", { 1: 90 }, 2),
			createMockFeature(202, "Medium Likelihood", { 1: 70 }, 4),
			createMockFeature(203, "Low Likelihood", { 1: 40 }, 6),
		];

		renderWithProviders(
			<MilestonesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				milestones={mockMilestones}
				selectedMilestoneId={1}
				features={customFeatures}
			/>,
		);

		// We can't easily test the colors directly, but we can at least ensure
		// all likelihood percentages are displayed
		expect(screen.getByText("90%")).toBeInTheDocument();
		expect(screen.getByText("70%")).toBeInTheDocument();
		expect(screen.getByText("40%")).toBeInTheDocument();
	});
});

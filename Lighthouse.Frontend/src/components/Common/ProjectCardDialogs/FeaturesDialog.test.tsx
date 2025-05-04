import { ThemeProvider, createTheme } from "@mui/material/styles";
import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import FeaturesDialog from "./FeaturesDialog";

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
const renderWithThemeAndRouter = (ui: React.ReactElement) => {
	return render(
		<ThemeProvider theme={theme}>
			<BrowserRouter>{ui}</BrowserRouter>
		</ThemeProvider>,
	);
};

describe("FeaturesDialog component", () => {
	// Mock data
	const projectName = "Test Project";
	const mockFeatures = [
		(() => {
			const feature = new Feature();
			feature.id = 1;
			feature.name = "Feature 1";
			feature.workItemReference = "F-123";
			feature.state = "In Progress";
			feature.stateCategory = "Doing";
			feature.url = "/features/1";

			// Configure required methods for feature completion calculation
			feature.getTotalWorkForFeature = vi.fn().mockReturnValue(10);
			feature.getRemainingWorkForFeature = vi.fn().mockReturnValue(4);

			return feature;
		})(),
		(() => {
			const feature = new Feature();
			feature.id = 2;
			feature.name = "Feature 2";
			feature.workItemReference = "F-124";
			feature.state = "Done";
			feature.stateCategory = "Done";
			feature.url = "/features/2";

			// Configure required methods for feature completion calculation
			feature.getTotalWorkForFeature = vi.fn().mockReturnValue(8);
			feature.getRemainingWorkForFeature = vi.fn().mockReturnValue(0);

			return feature;
		})(),
		(() => {
			const feature = new Feature();
			feature.id = 3;
			feature.name = "Feature 3";
			feature.workItemReference = "F-125";
			feature.state = "New";
			feature.stateCategory = "ToDo";
			feature.url = "";

			// Configure required methods for feature completion calculation
			feature.getTotalWorkForFeature = vi.fn().mockReturnValue(5);
			feature.getRemainingWorkForFeature = vi.fn().mockReturnValue(5);

			return feature;
		})(),
	];

	const mockOnClose = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly when open", () => {
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				features={mockFeatures}
			/>,
		);

		// Check if dialog title is rendered with correct project name and feature count
		expect(
			screen.getByText(`${projectName}: Features (3)`),
		).toBeInTheDocument();

		// Check if all features are rendered
		expect(screen.getByText(/F-123 - Feature 1/)).toBeInTheDocument();
		expect(screen.getByText(/F-124 - Feature 2/)).toBeInTheDocument();
		expect(screen.getByText(/F-125 - Feature 3/)).toBeInTheDocument();

		// Check if state labels are present
		expect(screen.getByText("In Progress")).toBeInTheDocument();
		expect(screen.getByText("Done")).toBeInTheDocument();
		expect(screen.getByText("New")).toBeInTheDocument();

		// Check if progress information is displayed
		expect(
			screen.getByText("4 of 10 work items remaining"),
		).toBeInTheDocument();
		expect(screen.getByText("0 of 8 work items remaining")).toBeInTheDocument();
		expect(screen.getByText("5 of 5 work items remaining")).toBeInTheDocument();

		// Check if completion percentages are displayed
		expect(screen.getByText("60% Complete")).toBeInTheDocument();
		expect(screen.getByText("100% Complete")).toBeInTheDocument();
		expect(screen.getByText("0% Complete")).toBeInTheDocument();
	});

	it("doesn't render when not open", () => {
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={false}
				onClose={mockOnClose}
				projectName={projectName}
				features={mockFeatures}
			/>,
		);

		// Dialog shouldn't be in the document
		expect(
			screen.queryByText(`${projectName}: Features (3)`),
		).not.toBeInTheDocument();
	});

	it("calls onClose when close button is clicked", () => {
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				features={mockFeatures}
			/>,
		);

		// Find and click the close button
		const closeButton = screen.getByLabelText("close");
		fireEvent.click(closeButton);

		// Check if onClose was called
		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	it("renders message when no features are available", () => {
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				features={[]}
			/>,
		);

		// Check if the no features message is displayed
		expect(screen.getByText("No features available")).toBeInTheDocument();
	});

	it("renders links for features", () => {
		// After reviewing the component, we understand that all feature names are rendered as links
		// even if the feature.url is null. This is intentional behavior in the component.
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				features={mockFeatures}
			/>,
		);

		// Find all links to verify they exist
		const links = screen.getAllByRole("link");

		// Should have at least 3 links (one for each feature)
		expect(links.length).toBeGreaterThanOrEqual(3);

		// Verify content of links
		expect(
			screen.getByRole("link", { name: /F-123 - Feature 1/ }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("link", { name: /F-124 - Feature 2/ }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("link", { name: /F-125 - Feature 3/ }),
		).toBeInTheDocument();
	});

	it("displays feature completion information", () => {
		renderWithThemeAndRouter(
			<FeaturesDialog
				open={true}
				onClose={mockOnClose}
				projectName={projectName}
				features={mockFeatures}
			/>,
		);

		// Check for completion information
		expect(screen.getByText("60% Complete")).toBeInTheDocument();
		expect(screen.getByText("100% Complete")).toBeInTheDocument();
		expect(screen.getByText("0% Complete")).toBeInTheDocument();

		// Note: We're not testing for the actual progress bars since they
		// are rendered by MUI's LinearProgress component which may not
		// render with the expected classes in the test environment
	});
});

/* eslint-disable @typescript-eslint/no-explicit-any */
// @ts-nocheck
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockConfigurationService,
	createMockProjectService,
	createMockTeamService,
	createMockWorkTrackingSystemService,
} from "../../../../tests/MockApiServiceProvider";
import ImportConfigurationDialog from "./ImportConfigurationDialog";

// Mock the step components
vi.mock("./Steps/ImportSettingsStep", () => ({
	default: (props) => (
		<div data-testid="mock-import-settings-step">
			<button
				type="button"
				data-testid="import-settings-next-with-wts"
				onClick={() =>
					props.onNext(
						[{ id: 1 }],
						[],
						[],
						[],
						[],
						[],
						new Map(),
						new Map(),
						false,
					)
				}
			>
				Next with WTS
			</button>
			<button
				type="button"
				data-testid="import-settings-next"
				onClick={() =>
					props.onNext([], [], [], [], [], [], new Map(), new Map(), false)
				}
			>
				Next
			</button>
			<button
				type="button"
				data-testid="import-settings-close"
				onClick={props.onClose}
			>
				Close
			</button>
		</div>
	),
}));

vi.mock("./Steps/WorkTrackingSystemConfigurationStep", () => ({
	default: (props) => (
		<div data-testid="mock-work-tracking-system-config-step">
			<button
				type="button"
				data-testid="work-tracking-system-next"
				onClick={() => props.onNext(props.newWorkTrackingSystems)}
			>
				Next
			</button>
			<button
				type="button"
				data-testid="work-tracking-system-cancel"
				onClick={props.onCancel}
			>
				Cancel
			</button>
		</div>
	),
}));

vi.mock("./Steps/ImportStep", () => ({
	default: (props) => (
		<div data-testid="mock-import-step">
			<button
				type="button"
				data-testid="import-next"
				onClick={() =>
					props.onNext({ workTrackingSystems: [], teams: [], projects: [] })
				}
			>
				Import
			</button>
			<button
				type="button"
				data-testid="import-cancel"
				onClick={props.onCancel}
			>
				Cancel
			</button>
		</div>
	),
}));

vi.mock("./Steps/ImportSummaryStep", () => ({
	default: (props) => (
		<div data-testid="mock-import-summary-step">
			<div data-testid="import-results">
				{JSON.stringify(props.importResults)}
			</div>
			<button
				type="button"
				data-testid="import-summary-close"
				onClick={props.onClose}
			>
				Close
			</button>
		</div>
	),
}));

// Mock services
const mockConfigurationService = createMockConfigurationService();
const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
const mockTeamService = createMockTeamService();
const mockProjectService = createMockProjectService();

// Helper function for rendering with mocked services
const renderWithMockApiProvider = (open = true, onClose = vi.fn()) => {
	const mockContext = createMockApiServiceContext({
		configurationService: mockConfigurationService,
		workTrackingSystemService: mockWorkTrackingSystemService,
		teamService: mockTeamService,
		projectService: mockProjectService,
	});

	return render(
		<ApiServiceContext.Provider value={mockContext}>
			<ImportConfigurationDialog open={open} onClose={onClose} />
		</ApiServiceContext.Provider>,
	);
};

describe("ImportConfigurationDialog Component", () => {
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("should not render the dialog when open is false", () => {
		renderWithMockApiProvider(false);
		expect(
			screen.queryByTestId("import-configuration-dialog"),
		).not.toBeInTheDocument();
	});

	it("should render the dialog when open is true", () => {
		renderWithMockApiProvider(true);
		expect(
			screen.getByTestId("import-configuration-dialog"),
		).toBeInTheDocument();
		expect(screen.getByText("Import Configuration")).toBeInTheDocument();
	});
	it("should render the stepper with all steps", () => {
		renderWithMockApiProvider();
		const steps = [
			"File Selection",
			"Secrets Configuration",
			"Import",
			"Summary",
		];
		for (const step of steps) {
			expect(screen.getByText(step)).toBeInTheDocument();
		}
	});

	it("should start with the ImportSettingsStep visible", () => {
		renderWithMockApiProvider();
		expect(screen.getByTestId("mock-import-settings-step")).toBeInTheDocument();
	});

	it("should close the dialog when onClose is called from the ImportSettingsStep", () => {
		const mockOnClose = vi.fn();
		renderWithMockApiProvider(true, mockOnClose);

		fireEvent.click(screen.getByTestId("import-settings-close"));

		expect(mockOnClose).toHaveBeenCalled();
	});

	it("should advance to WorkTrackingSystemConfigurationStep when ImportSettingsStep completes with newWorkTrackingSystems", async () => {
		renderWithMockApiProvider();

		fireEvent.click(screen.getByTestId("import-settings-next-with-wts"));

		await waitFor(() => {
			expect(
				screen.getByTestId("mock-work-tracking-system-config-step"),
			).toBeInTheDocument();
		});
	});

	it("should skip WorkTrackingSystemConfigurationStep when ImportSettingsStep completes with no newWorkTrackingSystems", async () => {
		renderWithMockApiProvider();

		fireEvent.click(screen.getByTestId("import-settings-next"));

		// It should skip to the Import step
		await waitFor(() => {
			expect(screen.getByTestId("mock-import-step")).toBeInTheDocument();
		});
	});

	it("should advance to ImportStep when WorkTrackingSystemConfigurationStep completes", async () => {
		renderWithMockApiProvider();

		// Go to WorkTrackingSystemConfigurationStep first
		fireEvent.click(screen.getByTestId("import-settings-next-with-wts"));

		await waitFor(() => {
			expect(
				screen.getByTestId("work-tracking-system-next"),
			).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("work-tracking-system-next"));

		// It should advance to the Import step
		await waitFor(() => {
			expect(screen.getByTestId("mock-import-step")).toBeInTheDocument();
		});
	});

	it("should cancel and reset state when cancel is clicked on WorkTrackingSystemConfigurationStep", async () => {
		renderWithMockApiProvider();

		// Go to WorkTrackingSystemConfigurationStep first
		fireEvent.click(screen.getByTestId("import-settings-next-with-wts"));

		await waitFor(() => {
			expect(
				screen.getByTestId("work-tracking-system-cancel"),
			).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("work-tracking-system-cancel"));

		// It should go back to the first step
		await waitFor(() => {
			expect(
				screen.getByTestId("mock-import-settings-step"),
			).toBeInTheDocument();
		});
	});

	it("should advance to ImportSummaryStep when ImportStep completes", async () => {
		renderWithMockApiProvider();

		// Go to ImportStep by skipping the WorkTrackingSystemConfigurationStep
		fireEvent.click(screen.getByTestId("import-settings-next"));

		await waitFor(() => {
			expect(screen.getByTestId("mock-import-step")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("import-next"));

		// It should advance to the ImportSummaryStep
		await waitFor(() => {
			expect(
				screen.getByTestId("mock-import-summary-step"),
			).toBeInTheDocument();
		});
	});

	it("should cancel and reset state when cancel is clicked on ImportStep", async () => {
		renderWithMockApiProvider();

		// Go to ImportStep
		fireEvent.click(screen.getByTestId("import-settings-next"));

		await waitFor(() => {
			expect(screen.getByTestId("mock-import-step")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("import-cancel"));

		// It should go back to the first step
		await waitFor(() => {
			expect(
				screen.getByTestId("mock-import-settings-step"),
			).toBeInTheDocument();
		});
	});

	it("should close the dialog and reset state when close is clicked on ImportSummaryStep", async () => {
		const mockOnClose = vi.fn();
		renderWithMockApiProvider(true, mockOnClose);

		// Go to ImportStep
		fireEvent.click(screen.getByTestId("import-settings-next"));

		await waitFor(() => {
			expect(screen.getByTestId("mock-import-step")).toBeInTheDocument();
		});

		// Go to ImportSummaryStep
		fireEvent.click(screen.getByTestId("import-next"));

		await waitFor(() => {
			expect(
				screen.getByTestId("mock-import-summary-step"),
			).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("import-summary-close"));

		expect(mockOnClose).toHaveBeenCalled();
	});
});

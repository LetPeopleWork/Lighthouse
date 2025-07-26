import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../tests/MockApiServiceProvider";
import { TestProviders } from "../../../tests/TestProviders";
import WorkItemTypesComponent from "./WorkItemTypesComponent";

describe("WorkItemTypesComponent", () => {
	const mockOnAddWorkItemType = vi.fn();
	const mockOnRemoveWorkItemType = vi.fn();
	const workItemTypes = ["Bug", "Feature", "Task"];

	const mockSuggestionService = createMockSuggestionService();

	const mockGetWorkItemTypesForTeams = vi
		.fn()
		.mockResolvedValue(["User Story", "Bug", "Task"]);
	const mockGetWorkItemTypesForProjects = vi
		.fn()
		.mockResolvedValue(["Project Epic", "Project Feature", "Project Task"]);

	mockSuggestionService.getWorkItemTypesForTeams = mockGetWorkItemTypesForTeams;
	mockSuggestionService.getWorkItemTypesForProjects =
		mockGetWorkItemTypesForProjects;

	const mockApiContext = createMockApiServiceContext({
		suggestionService: mockSuggestionService,
	});

	const renderWithContext = (isForTeam = true) => {
		return render(
			<TestProviders apiServiceOverrides={mockApiContext}>
				<WorkItemTypesComponent
					workItemTypes={workItemTypes}
					onAddWorkItemType={mockOnAddWorkItemType}
					onRemoveWorkItemType={mockOnRemoveWorkItemType}
					isForTeam={isForTeam}
				/>
			</TestProviders>,
		);
	};

	beforeEach(() => {
		mockOnAddWorkItemType.mockClear();
		mockOnRemoveWorkItemType.mockClear();
		mockGetWorkItemTypesForTeams.mockClear();
		mockGetWorkItemTypesForProjects.mockClear();
	});

	it("renders correctly", async () => {
		renderWithContext();

		expect(screen.getByText("Work Item Types")).toBeInTheDocument();

		for (const type of workItemTypes) {
			expect(screen.getByText(type)).toBeInTheDocument();
		}

		expect(screen.getByLabelText("New Work Item Type")).toBeInTheDocument();
		// Check for the help text
		expect(
			screen.getByText(/Type a new work item type and press Enter to add/i),
		).toBeInTheDocument();
	});

	it("fetches team work item types when isForTeam is true", async () => {
		renderWithContext(true);

		await waitFor(() => {
			expect(
				mockSuggestionService.getWorkItemTypesForTeams,
			).toHaveBeenCalledTimes(1);
			expect(
				mockSuggestionService.getWorkItemTypesForProjects,
			).not.toHaveBeenCalled();
		});
	});

	it("fetches project work item types when isForTeam is false", async () => {
		renderWithContext(false);

		await waitFor(() => {
			expect(
				mockSuggestionService.getWorkItemTypesForProjects,
			).toHaveBeenCalledTimes(1);
			expect(
				mockSuggestionService.getWorkItemTypesForTeams,
			).not.toHaveBeenCalled();
		});
	});

	it("calls onAddWorkItemType when a new work item type is added", async () => {
		renderWithContext();

		// Wait for suggestions to load
		await waitFor(() => {
			expect(
				mockSuggestionService.getWorkItemTypesForTeams,
			).toHaveBeenCalledTimes(1);
		});

		const input = screen.getByLabelText("New Work Item Type");

		fireEvent.change(input, { target: { value: "Improvement" } });
		fireEvent.keyDown(input, { key: "Enter" });

		await waitFor(() => {
			expect(mockOnAddWorkItemType).toHaveBeenCalledWith("Improvement");
			expect(mockOnAddWorkItemType).toHaveBeenCalledTimes(1);
		});
	});

	it("does not call onAddWorkItemType when the input is empty", async () => {
		renderWithContext();

		// Wait for suggestions to load
		await waitFor(() => {
			expect(
				mockSuggestionService.getWorkItemTypesForTeams,
			).toHaveBeenCalledTimes(1);
		});

		const input = screen.getByLabelText("New Work Item Type");

		fireEvent.change(input, { target: { value: "" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddWorkItemType).not.toHaveBeenCalled();
	});

	it("calls onRemoveWorkItemType when a work item type is removed", async () => {
		renderWithContext();

		// Wait for suggestions to load
		await waitFor(() => {
			expect(
				mockSuggestionService.getWorkItemTypesForTeams,
			).toHaveBeenCalledTimes(1);
		});

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Bug").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveWorkItemType).toHaveBeenCalledWith(workItemTypes[0]);
		expect(mockOnRemoveWorkItemType).toHaveBeenCalledTimes(1);
	});

	it("displays error in console when fetching suggestions fails", async () => {
		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
		const mockError = new Error("Failed to fetch");
		mockGetWorkItemTypesForTeams.mockRejectedValueOnce(mockError);

		renderWithContext();

		await waitFor(() => {
			expect(consoleSpy).toHaveBeenCalledWith(
				"Failed to fetch work item types:",
				mockError,
			);
		});

		consoleSpy.mockRestore();
	});
});

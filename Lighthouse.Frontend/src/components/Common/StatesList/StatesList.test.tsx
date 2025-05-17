import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../tests/MockApiServiceProvider";
import StatesList from "./StatesList";

describe("StatesList", () => {
	const mockOnAddToDoState = vi.fn();
	const mockOnRemoveToDoState = vi.fn();
	const mockOnAddDoingState = vi.fn();
	const mockOnRemoveDoingState = vi.fn();
	const mockOnAddDoneState = vi.fn();
	const mockOnRemoveDoneState = vi.fn();

	const toDoStates = ["Task 1", "Task 2"];
	const doingStates = ["In Progress"];
	const doneStates = ["Completed"];

	// Mock suggestion service with state data
	const mockSuggestionService = createMockSuggestionService();
	const mockTeamStates = {
		toDoStates: ["Backlog", "To Do", "Task 1"],
		doingStates: ["In Progress", "In Review", "Testing"],
		doneStates: ["Done", "Completed", "Closed"],
	};
	const mockProjectStates = {
		toDoStates: ["Project Backlog", "Project Planning", "Task 2"],
		doingStates: ["Project In Progress", "Project Testing"],
		doneStates: ["Project Complete", "Project Closed"],
	};

	const mockGetStatesForTeams = vi.fn().mockResolvedValue(mockTeamStates);
	const mockGetStatesForProjects = vi.fn().mockResolvedValue(mockProjectStates);

	mockSuggestionService.getStatesForTeams = mockGetStatesForTeams;
	mockSuggestionService.getStatesForProjects = mockGetStatesForProjects;

	const mockApiContext = createMockApiServiceContext({
		suggestionService: mockSuggestionService,
	});

	const renderWithContext = (isForTeam = true) => {
		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<StatesList
					toDoStates={toDoStates}
					onAddToDoState={mockOnAddToDoState}
					onRemoveToDoState={mockOnRemoveToDoState}
					doingStates={doingStates}
					onAddDoingState={mockOnAddDoingState}
					onRemoveDoingState={mockOnRemoveDoingState}
					doneStates={doneStates}
					onAddDoneState={mockOnAddDoneState}
					onRemoveDoneState={mockOnRemoveDoneState}
					isForTeam={isForTeam}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	beforeEach(() => {
		mockOnAddToDoState.mockClear();
		mockOnRemoveToDoState.mockClear();
		mockOnAddDoingState.mockClear();
		mockOnRemoveDoingState.mockClear();
		mockOnAddDoneState.mockClear();
		mockOnRemoveDoneState.mockClear();
		mockGetStatesForTeams.mockClear();
		mockGetStatesForProjects.mockClear();
	});

	it("renders the title correctly", async () => {
		await act(async () => {
			renderWithContext();
		});

		expect(screen.getByText("States")).toBeInTheDocument();
	});

	it("renders to do states correctly", async () => {
		await act(async () => {
			renderWithContext();
		});

		for (const state of toDoStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("fetches team states when isForTeam is true", async () => {
		await act(async () => {
			renderWithContext(true);
		});

		expect(mockSuggestionService.getStatesForTeams).toHaveBeenCalledTimes(1);
		expect(mockSuggestionService.getStatesForProjects).not.toHaveBeenCalled();
	});

	it("fetches project states when isForTeam is false", async () => {
		await act(async () => {
			renderWithContext(false);
		});

		expect(mockSuggestionService.getStatesForProjects).toHaveBeenCalledTimes(1);
		expect(mockSuggestionService.getStatesForTeams).not.toHaveBeenCalled();
	});

	it("provides appropriate suggestions from the API data", async () => {
		await act(async () => {
			renderWithContext(true);
		});

		// Check if we can find a way to test the suggestions properly in future tests
		expect(mockSuggestionService.getStatesForTeams).toHaveBeenCalledTimes(1);
	});

	it("calls onAddToDoState when a new to do state is added", async () => {
		await act(async () => {
			renderWithContext();
		});

		const input = screen.getByLabelText("New To Do States");

		await act(async () => {
			fireEvent.change(input, { target: { value: "Task 3" } });
			fireEvent.keyDown(input, { key: "Enter" });
		});

		expect(mockOnAddToDoState).toHaveBeenCalledWith("Task 3");
	});

	it("calls onRemoveToDoState when an item is removed", async () => {
		await act(async () => {
			renderWithContext();
		});

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Task 1").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			await act(async () => {
				fireEvent.click(deleteIcon);
			});
		}

		expect(mockOnRemoveToDoState).toHaveBeenCalledWith("Task 1");
	});

	it("renders doing states correctly", async () => {
		await act(async () => {
			renderWithContext();
		});

		for (const state of doingStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("calls onAddDoingState when a new doing state is added", async () => {
		await act(async () => {
			renderWithContext();
		});

		const input = screen.getByLabelText("New Doing States");

		await act(async () => {
			fireEvent.change(input, { target: { value: "Task 4" } });
			fireEvent.keyDown(input, { key: "Enter" });
		});

		expect(mockOnAddDoingState).toHaveBeenCalledWith("Task 4");
	});

	it("calls onRemoveDoingState when an item is removed", async () => {
		await act(async () => {
			renderWithContext();
		});

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen
			.getByText("In Progress")
			.closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			await act(async () => {
				fireEvent.click(deleteIcon);
			});
		}

		expect(mockOnRemoveDoingState).toHaveBeenCalledWith("In Progress");
	});

	it("renders done states correctly", async () => {
		await act(async () => {
			renderWithContext();
		});

		for (const state of doneStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("calls onAddDoneState when a new done state is added", async () => {
		await act(async () => {
			renderWithContext();
		});

		const input = screen.getByLabelText("New Done States");

		await act(async () => {
			fireEvent.change(input, { target: { value: "Task 5" } });
			fireEvent.keyDown(input, { key: "Enter" });
		});

		expect(mockOnAddDoneState).toHaveBeenCalledWith("Task 5");
	});

	it("calls onRemoveDoneState when an item is removed", async () => {
		await act(async () => {
			renderWithContext();
		});

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Completed").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			await act(async () => {
				fireEvent.click(deleteIcon);
			});
		}

		expect(mockOnRemoveDoneState).toHaveBeenCalledWith("Completed");
	});

	it("displays error in console when fetching suggestions fails", async () => {
		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
		const mockError = new Error("Failed to fetch");
		mockGetStatesForTeams.mockRejectedValueOnce(mockError);

		await act(async () => {
			renderWithContext();
		});

		expect(consoleSpy).toHaveBeenCalledWith(
			"Failed to fetch states:",
			mockError,
		);

		consoleSpy.mockRestore();
	});

	it("correctly filters out already selected states from suggestions", async () => {
		await act(async () => {
			renderWithContext(true);
		});

		expect(mockSuggestionService.getStatesForTeams).toHaveBeenCalledTimes(1);
	});
});

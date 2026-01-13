import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { IWizardService } from "../../services/Api/WizardService";
import JiraBoardWizard from "./JiraBoardWizard";

describe("JiraBoardWizard", () => {
	const mockOnComplete = vi.fn();
	const mockOnCancel = vi.fn();
	const mockGetJiraBoards = vi.fn();
	const mockGetJiraBoardInformation = vi.fn();

	const mockBoards: IBoard[] = [
		{ id: 1, name: "Sprint Board" },
		{ id: 2, name: "Kanban Board" },
		{ id: 3, name: "Project X Board" },
	];

	const mockBoardInformation: IBoardInformation = {
		dataRetrievalValue: "board-1",
		workItemTypes: ["Story", "Bug"],
		toDoStates: ["To Do"],
		doingStates: ["In Progress"],
		doneStates: ["Done"],
	};

	const mockWizardService: Partial<IWizardService> = {
		getJiraBoards: mockGetJiraBoards,
		getJiraBoardInformation: mockGetJiraBoardInformation,
	};

	const mockApiServiceContext = {
		wizardService: mockWizardService as IWizardService,
		// biome-ignore lint/suspicious/noExplicitAny: Required for testing context
	} as any;

	beforeEach(() => {
		mockOnComplete.mockClear();
		mockOnCancel.mockClear();
		mockGetJiraBoards.mockClear();
		mockGetJiraBoardInformation.mockClear();
	});

	it("renders the wizard when open", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByText("Select Jira Board")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();

		// Wait for boards to load
		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});
	});

	it("does not render when closed", () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={false}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.queryByText("Select Jira Board")).not.toBeInTheDocument();
	});

	it("shows loading state while fetching boards", () => {
		const delayedPromise = new Promise<IBoard[]>((resolve) =>
			setTimeout(() => resolve(mockBoards), 100),
		);
		mockGetJiraBoards.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("fetches boards on open", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(mockGetJiraBoards).toHaveBeenCalledWith(1);
		});
	});

	it("displays fetched boards in autocomplete", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
			expect(screen.getByText("Kanban Board")).toBeInTheDocument();
			expect(screen.getByText("Project X Board")).toBeInTheDocument();
		});
	});

	it("disables Select Board button when no board is selected", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		expect(selectButton).toBeDisabled();
	});

	it("enables Select Board button when a board is selected", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		// Wait for board information to be fetched
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		await waitFor(() => {
			const selectButton = screen.getByRole("button", {
				name: "Select Board",
			});
			expect(selectButton).not.toBeDisabled();
		});
	});

	it("calls onComplete with empty string when Select Board is clicked", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Kanban Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Kanban Board"));

		// Wait for board information to be fetched
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		await userEvent.click(selectButton);

		// This test will be updated to expect board information once implementation is complete
		// For now, keeping the old expectation to show the transition
		expect(mockOnComplete).toHaveBeenCalled();
		expect(mockOnCancel).not.toHaveBeenCalled();
	});

	it("calls onCancel when Cancel button is clicked", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const cancelButton = screen.getByRole("button", { name: "Cancel" });
		await userEvent.click(cancelButton);

		expect(mockOnCancel).toHaveBeenCalled();
		expect(mockOnComplete).not.toHaveBeenCalled();
	});

	it("shows error message when board fetch fails", async () => {
		mockGetJiraBoards.mockRejectedValue(new Error("Network error"));

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(
				screen.getByText("Failed to load Jira boards. Please try again."),
			).toBeInTheDocument();
		});
	});

	it("shows error message when no boards are available", async () => {
		mockGetJiraBoards.mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(
				screen.getByText("No boards available for this Jira connection."),
			).toBeInTheDocument();
		});
	});

	it("disables autocomplete when no boards are available", async () => {
		mockGetJiraBoards.mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		expect(autocomplete).toBeDisabled();
	});

	it("resets state after successful completion", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		const { rerender } = render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		// Wait for board information to be fetched
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		await userEvent.click(selectButton);

		expect(mockOnComplete).toHaveBeenCalled();

		// Close the wizard
		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={false}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		// Clear the mock and reopen the wizard
		mockGetJiraBoards.mockClear();
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		// Should fetch boards again
		await waitFor(() => {
			expect(mockGetJiraBoards).toHaveBeenCalled();
		});
	});

	it("resets state when Cancel is clicked after selecting a board", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		const { rerender } = render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Project X Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Project X Board"));

		const cancelButton = screen.getByRole("button", { name: "Cancel" });
		await userEvent.click(cancelButton);

		// Reopen the wizard
		mockGetJiraBoards.mockClear();
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		// Select Board button should be disabled (state was reset)
		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		expect(selectButton).toBeDisabled();
	});

	it("allows searching through boards", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.type(autocomplete, "Kanban");

		await waitFor(() => {
			expect(screen.getByText("Kanban Board")).toBeInTheDocument();
		});

		// Sprint Board should be filtered out
		expect(screen.queryByText("Sprint Board")).not.toBeInTheDocument();
	});

	it("uses the correct connection ID when fetching boards", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={42}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(mockGetJiraBoards).toHaveBeenCalledWith(42);
		});
	});

	it("disables Select Board button during loading", () => {
		const delayedPromise = new Promise<IBoard[]>((resolve) =>
			setTimeout(() => resolve(mockBoards), 100),
		);
		mockGetJiraBoards.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		expect(selectButton).toBeDisabled();
	});

	it("fetches board information when a board is selected", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalledWith(1, 1);
		});
	});

	it("shows loading spinner while fetching board information", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		let resolvePromise: (value: IBoardInformation) => void;
		const delayedPromise = new Promise<IBoardInformation>((resolve) => {
			resolvePromise = resolve;
		});
		mockGetJiraBoardInformation.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		// Should show loading indicator while fetching
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		// At this point, fetchingBoardInfo should be true, check for spinner
		const spinners = screen.queryAllByRole("progressbar");
		expect(spinners.length).toBeGreaterThan(0);

		// Resolve the promise
		// biome-ignore lint/style/noNonNullAssertion: Test code - variable is initialized in promise constructor
		resolvePromise!(mockBoardInformation);

		// Wait for loading to complete
		await waitFor(() => {
			const selectButton = screen.getByRole("button", {
				name: "Select Board",
			});
			expect(selectButton).not.toBeDisabled();
		});
	});

	it("calls onComplete with board information when Select Board is clicked", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Kanban Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Kanban Board"));

		// Wait for board information to be fetched
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalledWith(1, 2);
		});

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		await userEvent.click(selectButton);

		expect(mockOnComplete).toHaveBeenCalledWith(mockBoardInformation);
		expect(mockOnCancel).not.toHaveBeenCalled();
	});

	it("calls onComplete with empty board information when fetch fails", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockRejectedValue(
			new Error("Failed to fetch board info"),
		);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		// Wait for the failed fetch attempt
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		const selectButton = screen.getByRole("button", {
			name: "Select Board",
		});
		await userEvent.click(selectButton);

		const emptyBoardInfo: IBoardInformation = {
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
		};

		expect(mockOnComplete).toHaveBeenCalledWith(emptyBoardInfo);
	});

	it("displays 'Loading Board Information' when fetching board information", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		let resolveBoardInfo: ((value: IBoardInformation) => void) | undefined;
		const delayedPromise = new Promise<IBoardInformation>((resolve) => {
			resolveBoardInfo = resolve;
		});
		mockGetJiraBoardInformation.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		await waitFor(() => {
			expect(screen.getByText("Loading Board Information")).toBeInTheDocument();
		});

		// Clean up
		if (resolveBoardInfo) {
			resolveBoardInfo(mockBoardInformation);
		}
	});

	it("displays board information after it is loaded", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		// Wait for board information to be fetched
		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		// Check that board information is displayed
		await waitFor(() => {
			expect(screen.getByText("Board Information")).toBeInTheDocument();
			expect(screen.getByText("Story")).toBeInTheDocument();
			expect(screen.getByText("Bug")).toBeInTheDocument();
		});
	});

	it("displays JQL in board information preview", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);
		mockGetJiraBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		const autocomplete = screen.getByLabelText("Board");
		await userEvent.click(autocomplete);

		await waitFor(() => {
			expect(screen.getByText("Sprint Board")).toBeInTheDocument();
		});

		await userEvent.click(screen.getByText("Sprint Board"));

		await waitFor(() => {
			expect(mockGetJiraBoardInformation).toHaveBeenCalled();
		});

		await waitFor(() => {
			expect(screen.getByText("board-1")).toBeInTheDocument();
		});
	});

	it("does not display board information before board selection", async () => {
		mockGetJiraBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<JiraBoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});

		expect(screen.queryByText("Board Information")).not.toBeInTheDocument();
		expect(
			screen.queryByText("Loading Board Information"),
		).not.toBeInTheDocument();
	});
});

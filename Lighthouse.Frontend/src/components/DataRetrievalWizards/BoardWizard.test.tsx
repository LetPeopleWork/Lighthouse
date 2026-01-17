import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { IWizardService } from "../../services/Api/WizardService";
import BoardWizard from "./BoardWizard";

describe("BoardWizard", () => {
	const mockOnComplete = vi.fn();
	const mockOnCancel = vi.fn();
	const mockGetBoards = vi.fn();
	const mockGetBoardInformation = vi.fn();

	const mockBoards: IBoard[] = [
		{ id: "1", name: "Sprint Board" },
		{ id: "2", name: "Kanban Board" },
		{ id: "3", name: "Project X Board" },
	];

	const mockBoardInformation: IBoardInformation = {
		dataRetrievalValue: "board-1",
		workItemTypes: ["Story", "Bug"],
		toDoStates: ["To Do"],
		doingStates: ["In Progress"],
		doneStates: ["Done"],
	};

	const mockWizardService: Partial<IWizardService> = {
		getBoards: mockGetBoards,
		getBoardInformation: mockGetBoardInformation,
	};

	const mockApiServiceContext = {
		wizardService: mockWizardService as IWizardService,
		// biome-ignore lint/suspicious/noExplicitAny: Required for testing context
	} as any;

	beforeEach(() => {
		mockOnComplete.mockClear();
		mockOnCancel.mockClear();
		mockGetBoards.mockClear();
		mockGetBoardInformation.mockClear();
	});

	it("renders the wizard when open", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByText("Confirm")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();

		// Wait for boards to load
		await waitFor(() => {
			expect(screen.getByLabelText("Board")).toBeInTheDocument();
		});
	});

	it("does not render when closed", () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={false}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.queryByText("Select Board")).not.toBeInTheDocument();
	});

	it("shows loading state while fetching boards", () => {
		const delayedPromise = new Promise<IBoard[]>((resolve) =>
			setTimeout(() => resolve(mockBoards), 100),
		);
		mockGetBoards.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(mockGetBoards).toHaveBeenCalledWith(1);
		});
	});

	it("displays fetched boards in autocomplete", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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

	it("disables Confirm button when no board is selected", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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

		const confirmButton = screen.getByRole("button", {
			name: "Confirm",
		});
		expect(confirmButton).toBeDisabled();
	});

	it("enables Confirm button when a board is selected", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		await waitFor(() => {
			const confirmButton = screen.getByRole("button", {
				name: "Confirm",
			});
			expect(confirmButton).not.toBeDisabled();
		});
	});

	it("calls onComplete with empty string when Confirm is clicked", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		const confirmButton = screen.getByRole("button", {
			name: "Confirm",
		});
		await userEvent.click(confirmButton);

		// This test will be updated to expect board information once implementation is complete
		// For now, keeping the old expectation to show the transition
		expect(mockOnComplete).toHaveBeenCalled();
		expect(mockOnCancel).not.toHaveBeenCalled();
	});

	it("calls onCancel when Cancel button is clicked", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockRejectedValue(new Error("Network error"));

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(
				screen.getByText("Failed to load boards. Please try again."),
			).toBeInTheDocument();
		});
	});

	it("shows error message when no boards are available", async () => {
		mockGetBoards.mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(
				screen.getByText("No boards available for this connection."),
			).toBeInTheDocument();
		});
	});

	it("disables autocomplete when no boards are available", async () => {
		mockGetBoards.mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		const { rerender } = render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		const confirmButton = screen.getByRole("button", {
			name: "Confirm",
		});
		await userEvent.click(confirmButton);

		expect(mockOnComplete).toHaveBeenCalled();

		// Close the wizard
		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={false}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		// Clear the mock and reopen the wizard
		mockGetBoards.mockClear();
		mockGetBoards.mockResolvedValue(mockBoards);

		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		// Should fetch boards again
		await waitFor(() => {
			expect(mockGetBoards).toHaveBeenCalled();
		});
	});

	it("resets state when Cancel is clicked after selecting a board", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		const { rerender } = render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockClear();
		mockGetBoards.mockResolvedValue(mockBoards);

		rerender(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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

		// Confirm button should be disabled (state was reset)
		const confirmButton = screen.getByRole("button", {
			name: "Confirm",
		});
		expect(confirmButton).toBeDisabled();
	});

	it("allows searching through boards", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={42}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(mockGetBoards).toHaveBeenCalledWith(42);
		});
	});

	it("disables Confirm button during loading", () => {
		const delayedPromise = new Promise<IBoard[]>((resolve) =>
			setTimeout(() => resolve(mockBoards), 100),
		);
		mockGetBoards.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
					open={true}
					workTrackingSystemConnectionId={1}
					onComplete={mockOnComplete}
					onCancel={mockOnCancel}
				/>
			</ApiServiceContext.Provider>,
		);

		const confirmButton = screen.getByRole("button", {
			name: "Confirm",
		});
		expect(confirmButton).toBeDisabled();
	});

	it("fetches board information when a board is selected", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalledWith(1, "1");
		});
	});

	it("shows loading spinner while fetching board information", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		let resolvePromise: (value: IBoardInformation) => void;
		const delayedPromise = new Promise<IBoardInformation>((resolve) => {
			resolvePromise = resolve;
		});
		mockGetBoardInformation.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		// At this point, fetchingBoardInfo should be true, check for spinner
		const spinners = screen.queryAllByRole("progressbar");
		expect(spinners.length).toBeGreaterThan(0);

		// Resolve the promise
		// biome-ignore lint/style/noNonNullAssertion: Test code - variable is initialized in promise constructor
		resolvePromise!(mockBoardInformation);

		// Wait for loading to complete
		await waitFor(() => {
			const confirmButton = screen.getByRole("button", {
				name: "Confirm",
			});
			expect(confirmButton).not.toBeDisabled();
		});
	});

	it("calls onComplete with board information when Confirm is clicked", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalledWith(1, "2");
		});

		const selectButton = screen.getByRole("button", {
			name: "Confirm",
		});
		await userEvent.click(selectButton);

		expect(mockOnComplete).toHaveBeenCalledWith(mockBoardInformation);
		expect(mockOnCancel).not.toHaveBeenCalled();
	});

	it("calls onComplete with empty board information when fetch fails", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockRejectedValue(
			new Error("Failed to fetch board info"),
		);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		const selectButton = screen.getByRole("button", {
			name: "Confirm",
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
		mockGetBoards.mockResolvedValue(mockBoards);

		let resolveBoardInfo: ((value: IBoardInformation) => void) | undefined;
		const delayedPromise = new Promise<IBoardInformation>((resolve) => {
			resolveBoardInfo = resolve;
		});
		mockGetBoardInformation.mockImplementation(() => delayedPromise);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		// Check that board information is displayed
		await waitFor(() => {
			expect(screen.getByText("Board Information")).toBeInTheDocument();
			expect(screen.getByText("Story")).toBeInTheDocument();
			expect(screen.getByText("Bug")).toBeInTheDocument();
		});
	});

	it("displays JQL in board information preview", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockResolvedValue(mockBoardInformation);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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
			expect(mockGetBoardInformation).toHaveBeenCalled();
		});

		await waitFor(() => {
			expect(screen.getByText("board-1")).toBeInTheDocument();
		});
	});

	it("does not display board information before board selection", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);

		render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BoardWizard
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

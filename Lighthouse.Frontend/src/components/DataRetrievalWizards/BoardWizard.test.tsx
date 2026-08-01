import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import { ApiError } from "../../services/Api/ApiError";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import type { IWizardService } from "../../services/Api/WizardService";
import BoardWizard from "./BoardWizard";

// What the backend already says when a board read is refused, carried to the browser as an ApiError
// by BaseApiService.parseApiErrorPayload. Retrying fixes none of the failures this replaces.
const REFUSAL =
	"ServiceNow refused to read the table 'vtb_board' with this account. Grant the account a role that can read that table.";

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

	// Story #5610 slice 02, AC-B3 / ADR-126 decision 2. Every refusal used to arrive here as
	// "Failed to load boards. Please try again." — the same sentence for a rejected credential, a
	// table the account may not read and a board nobody shared, and advice that fixes none of them.
	// The backend already wrote the words that name the table and the role to grant.
	// DISTILL scaffold for #5610 slice 02 - un-skip in DELIVER (ADR-025).
	it("shows the reason the board list was refused", async () => {
		mockGetBoards.mockRejectedValue(new ApiError(403, REFUSAL));

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
			expect(screen.getByText(REFUSAL)).toBeInTheDocument();
		});
	});

	// ADR-126 decision 3. Boards are shared, not roled, so an empty list has two causes that nothing
	// on the instance can tell apart — the account is a member of no board, or none of its boards
	// carries both a table and a filter. The copy names both and asserts neither.
	// DISTILL scaffold for #5610 slice 02 - un-skip in DELIVER (ADR-025).
	it("names both reasons a connection may have no board to offer", async () => {
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
			expect(screen.getByRole("dialog")).toHaveTextContent(
				/not a member of any Visual Task Board/i,
			);
		});

		expect(screen.getByRole("dialog")).toHaveTextContent(
			/both a table and a filter/i,
		);
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

	// Story #5610 slice 02, AC-B3 / D9 / ADR-126 decision 2. This replaces the assertion that pinned
	// the defect: a failed board read used to be substituted by an all-empty board, which is truthy,
	// which enabled Confirm. Nothing was overwritten — the settings screen writes each field only
	// when the incoming value is non-empty — so the dialog reported success and silently did
	// nothing. A refusal wearing a success costume, for all four connectors.
	// DISTILL scaffold for #5610 slice 02 - un-skip in DELIVER (ADR-025).
	it("cannot be confirmed when the board could not be read", async () => {
		mockGetBoards.mockResolvedValue(mockBoards);
		mockGetBoardInformation.mockRejectedValue(new ApiError(403, REFUSAL));

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

		await waitFor(() => {
			expect(screen.getByText(REFUSAL)).toBeInTheDocument();
		});

		const confirmButton = screen.getByRole("button", { name: "Confirm" });

		expect(confirmButton).toBeDisabled();

		// A disabled MUI button carries pointer-events: none, which user-event refuses to click
		// unless told to; the point here is that clicking it anyway still completes nothing.
		await userEvent.click(confirmButton, { pointerEventsCheck: 0 });

		expect(mockOnComplete).not.toHaveBeenCalled();
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

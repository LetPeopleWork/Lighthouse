import {
	Autocomplete,
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
	Typography,
} from "@mui/material";
import { useCallback, useContext, useEffect, useRef, useState } from "react";
import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import type { DataRetrievalWizardProps } from "../../models/DataRetrievalWizard/DataRetrievalWizard";
import { ApiError } from "../../services/Api/ApiError";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import BoardInformationDisplay from "../DataRetrieval/BoardInformationDisplay";

// An empty list has causes nothing on the instance can tell apart, so the copy names the
// possibilities and asserts none of them.
const NO_BOARDS =
	"No boards are available to this connection. The account may not have access to any board, or none of the boards it can see can be used by Lighthouse.";

// The backend words its own refusals; only a failure that never reached it needs copy here.
const refusalOr = (err: unknown, fallback: string): string =>
	err instanceof ApiError && err.message ? err.message : fallback;

const BoardWizard: React.FC<DataRetrievalWizardProps> = ({
	open,
	workTrackingSystemConnectionId,
	onComplete,
	onCancel,
	boardType = "Board",
}) => {
	const { wizardService } = useContext(ApiServiceContext);
	const [boards, setBoards] = useState<IBoard[]>([]);
	const [selectedBoard, setSelectedBoard] = useState<IBoard | null>(null);
	const [boardInformation, setBoardInformation] =
		useState<IBoardInformation | null>(null);
	const [loading, setLoading] = useState<boolean>(false);
	const [fetchingBoardInfo, setFetchingBoardInfo] = useState<boolean>(false);
	const [error, setError] = useState<string>("");
	const latestSelectionId = useRef<string | null>(null);

	const loadBoards = useCallback(async () => {
		setLoading(true);
		setError("");

		try {
			const fetchedBoards = await wizardService.getBoards(
				workTrackingSystemConnectionId,
			);
			setBoards(fetchedBoards);

			if (fetchedBoards.length === 0) {
				setError(NO_BOARDS);
			}
		} catch (error_) {
			setBoards([]);
			setError(refusalOr(error_, "Failed to load boards. Please try again."));
			console.error("Error loading boards:", error_);
		} finally {
			setLoading(false);
		}
	}, [wizardService, workTrackingSystemConnectionId]);

	useEffect(() => {
		if (open) {
			loadBoards().catch((error) => {
				console.error("Error loading boards:", error);
			});
		}
	}, [open, loadBoards]);

	const fetchBoardInformation = useCallback(
		async (board: IBoard) => {
			setFetchingBoardInfo(true);
			setError("");
			const isStillSelected = () => latestSelectionId.current === board.id;

			try {
				const info = await wizardService.getBoardInformation(
					workTrackingSystemConnectionId,
					board.id,
				);
				if (isStillSelected()) {
					setBoardInformation(info);
				}
			} catch (error_) {
				console.error("Error fetching board information:", error_);
				if (isStillSelected()) {
					setBoardInformation(null);
					setError(
						refusalOr(error_, "Failed to read the board. Please try again."),
					);
				}
			} finally {
				if (isStillSelected()) {
					setFetchingBoardInfo(false);
				}
			}
		},
		[wizardService, workTrackingSystemConnectionId],
	);

	const handleBoardChange = useCallback(
		(_: unknown, newValue: IBoard | null) => {
			setSelectedBoard(newValue);
			latestSelectionId.current = newValue?.id ?? null;
			if (newValue) {
				fetchBoardInformation(newValue).catch((error) => {
					console.error("Error fetching board information:", error);
				});
			} else {
				setBoardInformation(null);
				setFetchingBoardInfo(false);
			}
		},
		[fetchBoardInformation],
	);

	const handleConfirm = () => {
		if (boardInformation) {
			onComplete(boardInformation);
		}

		// Reset state
		latestSelectionId.current = null;
		setSelectedBoard(null);
		setBoardInformation(null);
		setError("");
	};

	const handleCancel = () => {
		latestSelectionId.current = null;
		setSelectedBoard(null);
		setBoardInformation(null);
		setError("");
		onCancel();
	};

	return (
		<Dialog open={open} onClose={handleCancel} maxWidth="sm" fullWidth>
			<DialogTitle>Select {boardType}</DialogTitle>
			<DialogContent>
				{loading ? (
					<CircularProgress sx={{ display: "block", margin: "2rem auto" }} />
				) : (
					<>
						<Autocomplete
							options={boards}
							getOptionLabel={(option) => option.name}
							value={selectedBoard}
							onChange={handleBoardChange}
							renderInput={(params) => (
								<TextField
									{...params}
									label={boardType}
									placeholder={`Search for a ${boardType.toLowerCase()}...`}
									fullWidth
									margin="normal"
								/>
							)}
							isOptionEqualToValue={(option, value) => option.id === value.id}
							disabled={boards.length === 0}
							noOptionsText={`No ${boardType.toLowerCase()}s available`}
						/>
						<BoardInformationDisplay
							boardInformation={boardInformation}
							loading={fetchingBoardInfo}
						/>
					</>
				)}
				{error && (
					<Typography color="error" sx={{ mt: 2 }}>
						{error}
					</Typography>
				)}
			</DialogContent>
			<DialogActions>
				<Button onClick={handleCancel}>Cancel</Button>
				<Button
					onClick={handleConfirm}
					variant="contained"
					disabled={!boardInformation || loading || fetchingBoardInfo}
				>
					Confirm
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default BoardWizard;

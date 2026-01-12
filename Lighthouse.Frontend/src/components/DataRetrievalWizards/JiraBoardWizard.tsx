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
import { useCallback, useContext, useEffect, useState } from "react";
import type { IBoard } from "../../models/Board";
import type { DataRetrievalWizardProps } from "../../models/DataRetrievalWizard/DataRetrievalWizard";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";

const JiraBoardWizard: React.FC<DataRetrievalWizardProps> = ({
	open,
	workTrackingSystemConnectionId,
	onComplete,
	onCancel,
}) => {
	const { wizardService } = useContext(ApiServiceContext);
	const [boards, setBoards] = useState<IBoard[]>([]);
	const [selectedBoard, setSelectedBoard] = useState<IBoard | null>(null);
	const [loading, setLoading] = useState<boolean>(false);
	const [error, setError] = useState<string>("");

	const loadBoards = useCallback(async () => {
		setLoading(true);
		setError("");

		try {
			const fetchedBoards = await wizardService.getJiraBoards(
				workTrackingSystemConnectionId,
			);
			setBoards(fetchedBoards);

			if (fetchedBoards.length === 0) {
				setError("No boards available for this Jira connection.");
			}
		} catch (err) {
			setError("Failed to load Jira boards. Please try again.");
			console.error("Error loading Jira boards:", err);
		} finally {
			setLoading(false);
		}
	}, [wizardService, workTrackingSystemConnectionId]);

	useEffect(() => {
		if (open) {
			loadBoards();
		}
	}, [open, loadBoards]);

	const handleConfirm = () => {
		// Return empty string for now as wizard is not yet complete
		onComplete("");

		// Reset state
		setSelectedBoard(null);
		setError("");
	};

	const handleCancel = () => {
		setSelectedBoard(null);
		setError("");
		onCancel();
	};

	return (
		<Dialog open={open} onClose={handleCancel} maxWidth="sm" fullWidth>
			<DialogTitle>Select Jira Board</DialogTitle>
			<DialogContent>
				{loading ? (
					<CircularProgress sx={{ display: "block", margin: "2rem auto" }} />
				) : (
					<Autocomplete
						options={boards}
						getOptionLabel={(option) => option.name}
						value={selectedBoard}
						onChange={(_, newValue) => setSelectedBoard(newValue)}
						renderInput={(params) => (
							<TextField
								{...params}
								label="Board"
								placeholder="Search for a board..."
								fullWidth
								margin="normal"
							/>
						)}
						isOptionEqualToValue={(option, value) => option.id === value.id}
						disabled={boards.length === 0}
						noOptionsText="No boards available"
					/>
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
					disabled={!selectedBoard || loading}
				>
					Select Board
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default JiraBoardWizard;

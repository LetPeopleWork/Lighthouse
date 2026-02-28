import {
	Alert,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from "@mui/material";
import type React from "react";
import { useId } from "react";

interface DeleteConfirmationDialogProps {
	open: boolean;
	itemName: string;
	onClose: (confirmed: boolean) => void;
	errorMessage?: string;
}

const DeleteConfirmationDialog: React.FC<DeleteConfirmationDialogProps> = ({
	open,
	itemName,
	onClose,
	errorMessage,
}) => {
	const handleClose = (confirmed: boolean) => {
		onClose(confirmed);
	};

	const alertDialogTitleId = useId();
	const alertDialogDescriptionId = useId();

	return (
		<Dialog
			open={open}
			onClose={() => handleClose(false)}
			aria-labelledby={alertDialogTitleId}
			aria-describedby={alertDialogDescriptionId}
		>
			<DialogTitle id={alertDialogTitleId}>Confirm Delete</DialogTitle>
			<DialogContent>
				<DialogContentText id={alertDialogDescriptionId}>
					Do you really want to delete {itemName}?
				</DialogContentText>
				{errorMessage && (
					<Alert
						severity="error"
						sx={{ mt: 2 }}
						data-testid="delete-error-alert"
					>
						{errorMessage}
					</Alert>
				)}
			</DialogContent>
			<DialogActions>
				<Button onClick={() => handleClose(false)} color="primary">
					Cancel
				</Button>
				<Button onClick={() => handleClose(true)} color="error" autoFocus>
					Delete
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default DeleteConfirmationDialog;

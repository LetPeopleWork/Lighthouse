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
	onConfirm: () => void;
	onCancel: () => void;
	errorMessage?: string;
	/** Something the deletion takes away for good, said before anybody confirms. */
	warning?: string;
}

const DeleteConfirmationDialog: React.FC<DeleteConfirmationDialogProps> = ({
	open,
	itemName,
	onConfirm,
	onCancel,
	errorMessage,
	warning,
}) => {
	const alertDialogTitleId = useId();
	const alertDialogDescriptionId = useId();

	return (
		<Dialog
			open={open}
			onClose={onCancel}
			aria-labelledby={alertDialogTitleId}
			aria-describedby={alertDialogDescriptionId}
		>
			<DialogTitle id={alertDialogTitleId}>Confirm Delete</DialogTitle>
			<DialogContent>
				<DialogContentText id={alertDialogDescriptionId}>
					Do you really want to delete {itemName}?
				</DialogContentText>
				{warning && (
					<Alert severity="warning" sx={{ mt: 2 }}>
						{warning}
					</Alert>
				)}
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
				<Button onClick={onCancel} color="primary">
					Cancel
				</Button>
				<Button onClick={onConfirm} color="error" autoFocus>
					Delete
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default DeleteConfirmationDialog;

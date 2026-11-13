import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from "@mui/material";
import type React from "react";
import { useEffect, useId, useState } from "react";

interface UnbindConfirmationDialogProps {
	open: boolean;
	sourceLabel: string;
	deliveryName: string;
	featuresTerm: string;
	onConfirm: () => void;
	onCancel: () => void;
}

/**
 * Asked because the way back is not symmetrical: binding again means finding the same entry in the
 * work tracking system and picking it a second time, which nothing on this page will do for you.
 */
const UnbindConfirmationDialog: React.FC<UnbindConfirmationDialogProps> = ({
	open,
	sourceLabel,
	deliveryName,
	featuresTerm,
	onConfirm,
	onCancel,
}) => {
	const titleId = useId();
	const descriptionId = useId();
	// The dialog is still on screen while it fades out, so the button that started the unbind can be
	// pressed a second time. That second press sends the same version number the first one has just
	// spent, and the reader is told someone else changed the delivery moments after their own change
	// went through.
	const [asked, setAsked] = useState(false);

	useEffect(() => {
		if (open) {
			setAsked(false);
		}
	}, [open]);

	return (
		<Dialog
			open={open}
			onClose={onCancel}
			aria-labelledby={titleId}
			aria-describedby={descriptionId}
		>
			<DialogTitle id={titleId}>Stop following the {sourceLabel}?</DialogTitle>
			<DialogContent>
				<DialogContentText id={descriptionId}>
					{`"${deliveryName}" keeps the name, the date and the ${featuresTerm} it has right now, and from then on they are yours to edit. It stops taking them from the ${sourceLabel}.`}
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary">
					Cancel
				</Button>
				<Button
					onClick={() => {
						setAsked(true);
						onConfirm();
					}}
					disabled={asked}
					color="primary"
					variant="contained"
					autoFocus
				>
					Stop following
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default UnbindConfirmationDialog;

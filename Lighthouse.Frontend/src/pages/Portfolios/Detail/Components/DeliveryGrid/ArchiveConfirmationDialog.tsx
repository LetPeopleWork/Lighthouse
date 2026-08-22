import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface ArchiveConfirmationDialogProps {
	open: boolean;
	itemName: string;
	onConfirm: () => void;
	onCancel: () => void;
}

/**
 * Deliberately not the delete dialog with different words. Archiving and deleting are both still
 * available on the same thing, and a reader who leaves here believing an archived one is out of
 * harm's way is being set up to archive something instead of copying it somewhere else. So this
 * says what archiving does — delists it, writes the numbers down, can be undone — and says plainly
 * that deleting still works on it afterwards, and it promises nothing beyond that.
 */
const ArchiveConfirmationDialog: React.FC<ArchiveConfirmationDialogProps> = ({
	open,
	itemName,
	onConfirm,
	onCancel,
}) => {
	const titleId = useId();
	const descriptionId = useId();

	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	return (
		<Dialog
			open={open}
			onClose={onCancel}
			aria-labelledby={titleId}
			aria-describedby={descriptionId}
		>
			<DialogTitle id={titleId}>Archive {itemName}?</DialogTitle>
			<DialogContent>
				<DialogContentText id={descriptionId}>
					This takes {itemName} out of the active {deliveriesTerm} list and
					writes down the numbers it shows right now, so they stay as they stand
					today.
				</DialogContentText>
				<DialogContentText sx={{ mt: 2 }}>
					You can bring it back at any time. Archiving is not the same as
					deleting, and it does not stand in the way of deleting either — an
					archived {deliveryTerm} can still be deleted, and deleting one takes
					the numbers written down here with it.
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary">
					Cancel
				</Button>
				<Button
					onClick={onConfirm}
					color="primary"
					variant="contained"
					autoFocus
				>
					Archive
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default ArchiveConfirmationDialog;

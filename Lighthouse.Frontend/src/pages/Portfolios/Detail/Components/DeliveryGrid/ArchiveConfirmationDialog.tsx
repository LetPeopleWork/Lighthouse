import {
	Button,
	Checkbox,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
	FormControlLabel,
} from "@mui/material";
import type React from "react";
import { useId, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface ArchiveConfirmationDialogProps {
	open: boolean;
	itemName: string;
	/** Told whether this reader ticked the box, so the caller can stop asking. */
	onConfirm: (stopAsking: boolean) => void;
	onCancel: () => void;
}

/**
 * Deliberately not the delete dialog with different words. Archiving and deleting are both still
 * available on the same thing, so this says what archiving does — delists it, writes the numbers
 * down, can be undone — and promises nothing beyond that. In particular it must not suggest that an
 * archived one is out of harm's way, because deleting still works on it and takes the written-down
 * numbers with it.
 */
const ArchiveConfirmationDialog: React.FC<ArchiveConfirmationDialogProps> = ({
	open,
	itemName,
	onConfirm,
	onCancel,
}) => {
	const titleId = useId();
	const descriptionId = useId();
	const [suppressed, setSuppressed] = useState(false);

	const { getTerm } = useTerminology();
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
					today. You can bring it back at any time.
				</DialogContentText>
				<FormControlLabel
					sx={{ mt: 2 }}
					control={
						<Checkbox
							checked={suppressed}
							onChange={(event) => setSuppressed(event.target.checked)}
							data-testid="skip-archive-confirmation"
						/>
					}
					label="Don't ask me again"
				/>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary">
					Cancel
				</Button>
				<Button
					onClick={() => onConfirm(suppressed)}
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

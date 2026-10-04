import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import { describeSplit } from "./voteWording";

const NO_SPLIT = { yes: 0, yesBut: 0, no: 0 };

interface VotesAndCommentsDialogProps {
	workItem: IRefinementRow;
	/** The name this browser votes under, or null when there is none to change (sign-in, or no vote yet). */
	voterName: string | null;
	onChangeName: () => void;
	onClose: () => void;
}

/** How the votes on one Work Item split, shown to every reader whether they voted or not. */
const VotesAndCommentsDialog: React.FC<
	Readonly<VotesAndCommentsDialogProps>
> = ({ workItem, voterName, onChangeName, onClose }) => (
	<Dialog open onClose={onClose} maxWidth="sm" fullWidth>
		<DialogTitle>{`${workItem.referenceId} ${workItem.name} · Votes and comments`}</DialogTitle>
		<DialogContent>
			<Typography>{describeSplit(workItem.split ?? NO_SPLIT)}</Typography>
		</DialogContent>
		<DialogActions>
			{voterName !== null && (
				<Stack direction="row" sx={{ alignItems: "center", mr: "auto", pl: 1 }}>
					<Typography variant="body2">{`Voting as ${voterName} ·`}</Typography>
					<Button size="small" onClick={onChangeName}>
						Change your name
					</Button>
				</Stack>
			)}
			<Button onClick={onClose}>Close</Button>
		</DialogActions>
	</Dialog>
);

export default VotesAndCommentsDialog;

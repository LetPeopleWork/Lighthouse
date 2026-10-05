import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import { describeSplit } from "./voteWording";

const NO_SPLIT = { yes: 0, yesBut: 0, no: 0 };

interface VotesAndCommentsDialogProps {
	workItem: IRefinementRow;
	/** The name this browser votes under, or null when there is none to change (sign-in, or no vote yet). */
	voterName: string | null;
	onChangeName: () => void;
	/** Sends a comment; `onSent` runs once the server has taken it. */
	onAddComment: (comment: string, onSent: () => void) => void;
	onClose: () => void;
}

const CommentBox: React.FC<
	Readonly<Pick<VotesAndCommentsDialogProps, "onAddComment">>
> = ({ onAddComment }) => {
	const [draft, setDraft] = useState<string | null>(null);

	if (draft === null) {
		return (
			<Button size="small" onClick={() => setDraft("")}>
				Add a comment
			</Button>
		);
	}

	const comment = draft.trim();
	return (
		<Stack spacing={1} sx={{ alignItems: "flex-end" }}>
			<TextField
				label="Comment"
				value={draft}
				onChange={(event) => setDraft(event.target.value)}
				multiline
				minRows={2}
				fullWidth
				autoFocus
			/>
			<Button
				variant="contained"
				size="small"
				disabled={comment === ""}
				onClick={() => onAddComment(comment, () => setDraft(null))}
			>
				Send
			</Button>
		</Stack>
	);
};

/** How the votes on one Work Item split, shown to every reader whether they voted or not. */
const VotesAndCommentsDialog: React.FC<
	Readonly<VotesAndCommentsDialogProps>
> = ({ workItem, voterName, onChangeName, onAddComment, onClose }) => (
	<Dialog open onClose={onClose} maxWidth="sm" fullWidth>
		<DialogTitle>{`${workItem.referenceId} ${workItem.name} · Votes and comments`}</DialogTitle>
		<DialogContent>
			<Stack spacing={2} sx={{ alignItems: "flex-start" }}>
				<Typography>{describeSplit(workItem.split ?? NO_SPLIT)}</Typography>
				<CommentBox onAddComment={onAddComment} />
			</Stack>
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

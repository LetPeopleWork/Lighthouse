import HelpOutlineIcon from "@mui/icons-material/HelpOutlineOutlined";
import {
	Alert,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	List,
	ListItem,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import type {
	IRefinementRow,
	ISizingLogEntry,
} from "../../../../models/Refinement/Refinement";
import { describeLogDay, describeLogEntry } from "./sizingLogWording";
import type { SizingLogState } from "./useSizingLog";
import { describeSplit, LONGEST_COMMENT } from "./voteWording";

const NO_SPLIT = { yes: 0, yesBut: 0, no: 0 };

interface VotesAndCommentsDialogProps {
	workItem: IRefinementRow;
	log: SizingLogState;
	isSendingAComment: boolean;
	/** The name this browser votes under, or null when there is none to change (sign-in, or no vote yet). */
	voterName: string | null;
	onChangeName: () => void;
	/** Sends a comment; `onSent` runs once the server has taken it. */
	onAddComment: (comment: string, onSent: () => void) => void;
	onClose: () => void;
}

const CommentBox: React.FC<
	Readonly<
		Pick<VotesAndCommentsDialogProps, "onAddComment" | "isSendingAComment">
	>
> = ({ onAddComment, isSendingAComment }) => {
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
		<Stack spacing={1} sx={{ alignItems: "flex-end", width: "100%" }}>
			<TextField
				label="Comment"
				value={draft}
				onChange={(event) => setDraft(event.target.value)}
				multiline
				minRows={2}
				fullWidth
				autoFocus
				disabled={isSendingAComment}
				slotProps={{ htmlInput: { maxLength: LONGEST_COMMENT } }}
			/>
			<Button
				variant="contained"
				size="small"
				disabled={comment === "" || isSendingAComment}
				onClick={() => onAddComment(comment, () => setDraft(null))}
			>
				Send
			</Button>
		</Stack>
	);
};

const OpenQuestionMarker: React.FC = () => (
	<Stack
		component="span"
		direction="row"
		spacing={0.5}
		sx={{ alignItems: "center", color: "warning.main" }}
	>
		<HelpOutlineIcon fontSize="small" />
		<Typography component="span" variant="body2">
			open question
		</Typography>
	</Stack>
);

const LogEntry: React.FC<Readonly<{ entry: ISizingLogEntry }>> = ({
	entry,
}) => (
	<ListItem disableGutters sx={{ display: "block" }}>
		<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
			<Typography sx={{ fontWeight: 500 }}>
				{describeLogEntry(entry)}
			</Typography>
			{entry.isOpenQuestion && <OpenQuestionMarker />}
			<Typography
				variant="body2"
				color="text.secondary"
				sx={{ flexGrow: 1, textAlign: "right" }}
			>
				{describeLogDay(entry)}
			</Typography>
		</Stack>
		{entry.comment !== null && (
			<Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
				{entry.comment}
			</Typography>
		)}
	</ListItem>
);

/** Comments are always shown as the plain text they are, never interpreted as markup. */
const SizingLog: React.FC<Readonly<{ log: SizingLogState }>> = ({ log }) => {
	if (log.status === "reading") {
		return null;
	}

	if (log.status === "failed") {
		return (
			<Alert severity="error" sx={{ width: "100%" }}>
				{log.message}
			</Alert>
		);
	}

	if (log.entries.length === 0) {
		return (
			<Typography color="text.secondary">No votes or comments yet.</Typography>
		);
	}

	// The log only ever grows at its end, so an entry's place in it never changes. Nothing else tells
	// two entries apart: one person can say the same thing twice within a second.
	const placed = log.entries.map((entry, place) => ({ entry, place }));
	return (
		<List dense disablePadding sx={{ width: "100%" }}>
			{placed.map(({ entry, place }) => (
				<LogEntry key={place} entry={entry} />
			))}
		</List>
	);
};

/** How the votes on one Work Item split and what was said about it, shown to every reader whether they voted or not. */
const VotesAndCommentsDialog: React.FC<
	Readonly<VotesAndCommentsDialogProps>
> = ({
	workItem,
	log,
	isSendingAComment,
	voterName,
	onChangeName,
	onAddComment,
	onClose,
}) => (
	<Dialog open onClose={onClose} maxWidth="sm" fullWidth>
		<DialogTitle>{`${workItem.referenceId} ${workItem.name} · Votes and comments`}</DialogTitle>
		<DialogContent>
			<Stack spacing={2} sx={{ alignItems: "flex-start" }}>
				<Typography>{describeSplit(workItem.split ?? NO_SPLIT)}</Typography>
				<SizingLog log={log} />
				<CommentBox
					onAddComment={onAddComment}
					isSendingAComment={isSendingAComment}
				/>
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

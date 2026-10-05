import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
	Alert,
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	List,
	ListItem,
	Stack,
	TextField,
	Tooltip,
	Typography,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import type {
	IRefinementRow,
	ISizingLogEntry,
	ISizingSplit,
	ISizingVoters,
	SizingAnswer,
} from "../../../../models/Refinement/Refinement";
import { useFocusTooltip } from "../useFocusTooltip";
import {
	describeAnswer,
	describeLogDay,
	describeWriter,
	isWritten,
} from "./sizingLogWording";
import type { SizingLogState } from "./useSizingLog";
import { LONGEST_COMMENT } from "./voteWording";

const NO_SPLIT: ISizingSplit = { yes: 0, yesBut: 0, no: 0 };
const NOBODY: ISizingVoters = { yes: [], yesBut: [], no: [] };

interface VotesAndCommentsDialogProps {
	workItem: IRefinementRow;
	log: SizingLogState;
	/** Why the log could not be read, in words the reader can act on. */
	describeFailure: (error: unknown) => string;
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

interface SplitCountProps {
	count: number;
	answer: SizingAnswer;
	voters: readonly string[];
}

const NamedCount: React.FC<
	Readonly<{ wording: string; voters: readonly string[] }>
> = ({ wording, voters }) => {
	const { tooltip, target } = useFocusTooltip();
	return (
		<Tooltip
			describeChild
			{...tooltip}
			title={voters.join("\n")}
			slotProps={{ tooltip: { sx: { whiteSpace: "pre-line" } } }}
		>
			<Box component="span" {...target}>
				{wording}
			</Box>
		</Tooltip>
	);
};

/** One count of the split; hovering or focusing it names the people whose current vote it is. */
const SplitCount: React.FC<Readonly<SplitCountProps>> = ({
	count,
	answer,
	voters,
}) => {
	const wording = `${count} ${describeAnswer(answer)}`;
	if (count === 0 || voters.length === 0) {
		return <span>{wording}</span>;
	}

	return <NamedCount wording={wording} voters={voters} />;
};

const countsOf = (voters: ISizingVoters): ISizingSplit => ({
	yes: voters.yes.length,
	yesBut: voters.yesBut.length,
	no: voters.no.length,
});

const VoteSplit: React.FC<
	Readonly<{ split: ISizingSplit; voters: ISizingVoters }>
> = ({ split, voters }) => (
	<Typography>
		<SplitCount count={split.yes} answer="Yes" voters={voters.yes} />
		{" · "}
		<SplitCount count={split.yesBut} answer="YesBut" voters={voters.yesBut} />
		{" · "}
		<SplitCount count={split.no} answer="No" voters={voters.no} />
	</Typography>
);

const OpenQuestionMarker: React.FC = () => (
	<Stack
		component="span"
		direction="row"
		spacing={0.5}
		sx={{ alignItems: "center", color: "warning.main" }}
	>
		<WarningAmberIcon fontSize="small" />
		<Typography component="span" variant="body2">
			open question
		</Typography>
	</Stack>
);

const WrittenEntry: React.FC<Readonly<{ entry: ISizingLogEntry }>> = ({
	entry,
}) => (
	<ListItem disableGutters sx={{ display: "block" }}>
		<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
			<Typography sx={{ fontWeight: 500 }}>{describeWriter(entry)}</Typography>
			{entry.isOpenQuestion && <OpenQuestionMarker />}
			<Typography
				variant="body2"
				color="text.secondary"
				sx={{ flexGrow: 1, textAlign: "right" }}
			>
				{describeLogDay(entry)}
			</Typography>
		</Stack>
		<Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
			{entry.comment}
		</Typography>
	</ListItem>
);

/** What people wrote, always shown as the plain text it is, never interpreted as markup. */
const WhatPeopleWrote: React.FC<
	Readonly<Pick<VotesAndCommentsDialogProps, "log" | "describeFailure">>
> = ({ log, describeFailure }) => {
	if (log.status === "reading") {
		return null;
	}

	if (log.status === "failed") {
		return (
			<Alert severity="error" sx={{ width: "100%" }}>
				{describeFailure(log.error)}
			</Alert>
		);
	}

	// The log only ever grows at its end, so an entry's place in it never changes. Nothing else tells
	// two entries apart: one person can say the same thing twice within a second.
	const written = log.entries
		.map((entry, place) => ({ entry, place }))
		.filter(({ entry }) => isWritten(entry));

	if (written.length === 0) {
		return <Typography color="text.secondary">No comments yet.</Typography>;
	}

	return (
		<List dense disablePadding sx={{ width: "100%" }}>
			{written.map(({ entry, place }) => (
				<WrittenEntry key={place} entry={entry} />
			))}
		</List>
	);
};

/** How the votes on one Work Item split and what was written about it, shown to every reader whether they voted or not. */
const VotesAndCommentsDialog: React.FC<
	Readonly<VotesAndCommentsDialogProps>
> = ({
	workItem,
	log,
	describeFailure,
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
				{/* The row's count was read when the tab loaded; once the log is in, the counts come from the
				    same reading as the names behind them, so the two can never disagree. */}
				{log.status === "read" ? (
					<VoteSplit split={countsOf(log.voters)} voters={log.voters} />
				) : (
					<VoteSplit split={workItem.split ?? NO_SPLIT} voters={NOBODY} />
				)}
				<WhatPeopleWrote log={log} describeFailure={describeFailure} />
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

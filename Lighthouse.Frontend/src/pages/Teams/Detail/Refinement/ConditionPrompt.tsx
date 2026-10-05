import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import { describeAnswer } from "./sizingLogWording";
import { LONGEST_COMMENT } from "./voteWording";

interface ConditionPromptProps {
	/** The Work Item the vote is on, as its reference and name. */
	workItem: string;
	onCancel: () => void;
	/** Casts the vote with its condition, already trimmed. */
	onConfirm: (condition: string) => void;
}

/** A "Yes, if…" means nothing without its condition, so the vote waits for one. */
const ConditionPrompt: React.FC<Readonly<ConditionPromptProps>> = ({
	workItem,
	onCancel,
	onConfirm,
}) => {
	const [draft, setDraft] = useState("");
	const condition = draft.trim();

	return (
		<Dialog open onClose={onCancel} maxWidth="xs" fullWidth>
			<DialogTitle>{describeAnswer("YesBut")}</DialogTitle>
			<DialogContent>
				<Typography>{workItem}</Typography>
				<TextField
					label="Condition"
					placeholder="What has to be true for a Yes?"
					value={draft}
					onChange={(event) => setDraft(event.target.value)}
					multiline
					minRows={2}
					fullWidth
					autoFocus
					margin="dense"
					slotProps={{ htmlInput: { maxLength: LONGEST_COMMENT } }}
				/>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				<Button
					variant="contained"
					disabled={condition === ""}
					onClick={() => onConfirm(condition)}
				>
					Vote
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default ConditionPrompt;

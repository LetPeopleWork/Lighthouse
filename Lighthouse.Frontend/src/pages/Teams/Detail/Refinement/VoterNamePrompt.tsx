import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
} from "@mui/material";
import type React from "react";
import { useState } from "react";

export const LONGEST_VOTER_NAME = 100;

interface VoterNamePromptProps {
	initialName?: string;
	confirmLabel: "Vote" | "Save";
	onCancel: () => void;
	onConfirm: (name: string) => void;
}

/** Asks a voter without sign-in for the name their votes are cast under: before the first vote, or to change it. */
const VoterNamePrompt: React.FC<Readonly<VoterNamePromptProps>> = ({
	initialName = "",
	confirmLabel,
	onCancel,
	onConfirm,
}) => {
	const [name, setName] = useState(initialName);
	const trimmed = name.trim();
	const isUsable = trimmed !== "" && trimmed.length <= LONGEST_VOTER_NAME;

	return (
		<Dialog open onClose={onCancel} maxWidth="xs" fullWidth>
			<DialogTitle>Who is voting?</DialogTitle>
			<DialogContent>
				<TextField
					label="Your name"
					value={name}
					onChange={(event) => setName(event.target.value)}
					helperText="Kept in this browser only."
					slotProps={{ htmlInput: { maxLength: LONGEST_VOTER_NAME } }}
					fullWidth
					autoFocus
					margin="dense"
				/>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				<Button
					variant="contained"
					disabled={!isUsable}
					onClick={() => onConfirm(name)}
				>
					{confirmLabel}
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default VoterNamePrompt;

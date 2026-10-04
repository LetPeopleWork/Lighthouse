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

interface VoterNamePromptProps {
	onCancel: () => void;
	onVote: (name: string) => void;
}

/** Asks a voter without sign-in for the name their votes are cast under, the first time they vote. */
const VoterNamePrompt: React.FC<Readonly<VoterNamePromptProps>> = ({
	onCancel,
	onVote,
}) => {
	const [name, setName] = useState("");
	const isBlank = name.trim() === "";

	return (
		<Dialog open onClose={onCancel} maxWidth="xs" fullWidth>
			<DialogTitle>Who is voting?</DialogTitle>
			<DialogContent>
				<TextField
					label="Your name"
					value={name}
					onChange={(event) => setName(event.target.value)}
					helperText="Kept in this browser only."
					fullWidth
					autoFocus
					margin="dense"
				/>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				<Button
					variant="contained"
					disabled={isBlank}
					onClick={() => onVote(name)}
				>
					Vote
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default VoterNamePrompt;

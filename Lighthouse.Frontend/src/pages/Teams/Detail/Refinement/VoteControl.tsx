import { ToggleButton, ToggleButtonGroup } from "@mui/material";
import type React from "react";
import type { SizingAnswer } from "../../../../models/Refinement/Refinement";

const ANSWERS: { answer: SizingAnswer; label: string }[] = [
	{ answer: "Yes", label: "Yes" },
	{ answer: "YesBut", label: "Yes, if…" },
	{ answer: "No", label: "No" },
];

interface VoteControlProps {
	myVote: SizingAnswer | null;
	onVote: (answer: SizingAnswer) => void;
}

const VoteControl: React.FC<Readonly<VoteControlProps>> = ({
	myVote,
	onVote,
}) => (
	<ToggleButtonGroup
		size="small"
		exclusive
		value={myVote}
		aria-label="Your vote"
		onChange={(_event, answer: SizingAnswer | null) => {
			if (answer !== null) {
				onVote(answer);
			}
		}}
	>
		{ANSWERS.map(({ answer, label }) => (
			<ToggleButton key={answer} value={answer}>
				{label}
			</ToggleButton>
		))}
	</ToggleButtonGroup>
);

export default VoteControl;

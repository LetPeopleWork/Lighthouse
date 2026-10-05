import { ToggleButton, ToggleButtonGroup } from "@mui/material";
import type React from "react";
import type { SizingAnswer } from "../../../../models/Refinement/Refinement";
import { describeAnswer } from "./sizingLogWording";

const ANSWERS: SizingAnswer[] = ["Yes", "YesBut", "No"];

interface VoteControlProps {
	referenceId: string;
	myVote: SizingAnswer | null;
	isSending: boolean;
	onVote: (answer: SizingAnswer) => void;
}

const VoteControl: React.FC<Readonly<VoteControlProps>> = ({
	referenceId,
	myVote,
	isSending,
	onVote,
}) => (
	<ToggleButtonGroup
		size="small"
		exclusive
		value={myVote}
		disabled={isSending}
		aria-label={`Your vote on ${referenceId}`}
		onChange={(_event, answer: SizingAnswer | null) => {
			if (answer !== null) {
				onVote(answer);
			}
		}}
	>
		{ANSWERS.map((answer) => (
			<ToggleButton key={answer} value={answer}>
				{describeAnswer(answer)}
			</ToggleButton>
		))}
	</ToggleButtonGroup>
);

export default VoteControl;

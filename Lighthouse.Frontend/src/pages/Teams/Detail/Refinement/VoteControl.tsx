import { ToggleButton, ToggleButtonGroup, Tooltip } from "@mui/material";
import type React from "react";
import type { SizingAnswer } from "../../../../models/Refinement/Refinement";
import { describeAnswer } from "./sizingLogWording";

const ANSWERS: SizingAnswer[] = ["Yes", "YesBut", "No"];

const TAKE_BACK_HINT = "Click again to take back your vote";

interface VoteControlProps {
	referenceId: string;
	myVote: SizingAnswer | null;
	isSending: boolean;
	onVote: (answer: SizingAnswer) => void;
	onTakeBack: () => void;
}

const VoteControl: React.FC<Readonly<VoteControlProps>> = ({
	referenceId,
	myVote,
	isSending,
	onVote,
	onTakeBack,
}) => (
	<ToggleButtonGroup
		size="small"
		exclusive
		value={myVote}
		disabled={isSending}
		aria-label={`Your vote on ${referenceId}`}
		onChange={(_event, answer: SizingAnswer | null) => {
			// An exclusive group answers null when its pressed button is clicked again.
			if (answer === null) {
				onTakeBack();
			} else {
				onVote(answer);
			}
		}}
	>
		{ANSWERS.map((answer) => (
			<Tooltip
				key={answer}
				describeChild
				title={answer === myVote && !isSending ? TAKE_BACK_HINT : ""}
			>
				<ToggleButton value={answer}>{describeAnswer(answer)}</ToggleButton>
			</Tooltip>
		))}
	</ToggleButtonGroup>
);

export default VoteControl;

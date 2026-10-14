import { Stack, Typography } from "@mui/material";
import type React from "react";
import { useRefusalWords, windowRefusalCopyFor } from "../reportRefusalCopy";
import type { ReportTypeStepProps } from "../reportTypes";
import {
	nowLengthOf,
	nowRefusalOf,
	type ThenAndNowDraft,
} from "./thenAndNowDraft";
import WindowLengthControl from "./WindowLengthControl";

/** Now always ends on the server's today; its length follows Then until changed. */
const NowStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	owner,
	templates,
	draft,
	onChange,
	disabled,
}) => {
	const nowLength = nowLengthOf(draft);
	const refusalWordsFor = useRefusalWords(owner);
	const refusal = nowRefusalOf(draft, templates);

	return (
		<Stack spacing={2}>
			<WindowLengthControl
				owner={owner}
				length={nowLength}
				onChange={(length) => onChange({ ...draft, nowLength: length })}
				disabled={disabled}
				fieldLabel="Now length in days"
				leadingLabel="Length"
				customLabel="or"
			/>
			<Typography color="text.secondary">
				{`= the last ${nowLength} days, always ending today`}
			</Typography>
			{refusal !== null && (
				<Typography color="error">
					{windowRefusalCopyFor(refusal, refusalWordsFor(templates))}
				</Typography>
			)}
		</Stack>
	);
};

export default NowStep;

import { Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import { useRefusalWords, windowRefusalCopyFor } from "../reportRefusalCopy";
import type { ReportTypeStepProps } from "../reportTypes";
import {
	lengthOf,
	resolvedThenDays,
	type ThenAndNowDraft,
	thenEndOf,
	thenRefusalOf,
} from "./thenAndNowDraft";
import WindowLengthControl from "./WindowLengthControl";

const ThenStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	owner,
	templates,
	draft,
	onChange,
	disabled,
}) => {
	const end = thenEndOf(draft, templates);
	const days = resolvedThenDays(end, lengthOf(draft.thenLength));
	const refusalWordsFor = useRefusalWords(owner);
	const refusal = thenRefusalOf(draft, templates);

	return (
		<Stack spacing={2}>
			<TextField
				label="Then ends on"
				type="date"
				value={end}
				disabled={disabled}
				onChange={(event) =>
					onChange({ ...draft, thenEnd: event.target.value })
				}
				slotProps={{
					inputLabel: { shrink: true },
					htmlInput: { max: templates.today },
				}}
			/>
			<WindowLengthControl
				owner={owner}
				length={draft.thenLength}
				onChange={(thenLength) => onChange({ ...draft, thenLength })}
				disabled={disabled}
				fieldLabel="Then length in days"
				presetsLabel="Then length presets"
				minimumLengthDays={templates.minimumLengthDays}
				customLabel="Custom:"
			/>
			{days !== null && (
				<Typography color="text.secondary">{`= ${days}`}</Typography>
			)}
			{refusal !== null && (
				<Typography color="error" role="alert">
					{windowRefusalCopyFor(refusal, refusalWordsFor(templates))}
				</Typography>
			)}
		</Stack>
	);
};

export default ThenStep;

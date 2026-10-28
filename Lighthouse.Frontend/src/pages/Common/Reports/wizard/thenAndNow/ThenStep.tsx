import { Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import { FROZEN_NOTE } from "../frozenNote";
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
	windowPresets,
	frozen = false,
}) => {
	const end = thenEndOf(draft, templates);
	const days = resolvedThenDays(end, lengthOf(draft.thenLength));
	const daysLine = frozen ? `= ${days} · ${FROZEN_NOTE}` : `= ${days}`;
	const refusalWordsFor = useRefusalWords(owner);
	const refusal = frozen ? null : thenRefusalOf(draft, templates);
	const locked = disabled || frozen;

	return (
		<Stack spacing={2}>
			<TextField
				label="Then ends on"
				type="date"
				value={end}
				disabled={locked}
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
				presets={windowPresets}
				length={draft.thenLength}
				onChange={(thenLength) => onChange({ ...draft, thenLength })}
				disabled={locked}
				fieldLabel="Then length in days"
				presetsLabel="Then length presets"
				minimumLengthDays={templates.minimumLengthDays}
				customLabel="Custom:"
			/>
			{days !== null && (
				<Typography color="text.secondary">{daysLine}</Typography>
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

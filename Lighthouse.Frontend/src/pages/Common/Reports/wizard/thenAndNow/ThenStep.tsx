import { Stack, TextField, Typography } from "@mui/material";
import { format } from "date-fns";
import type React from "react";
import { parseLocalDate } from "../../../../../utils/date/localDate";
import { FROZEN_NOTE } from "../frozenNote";
import { LONG_DAY } from "../longDay";
import { useRefusalWords, windowRefusalCopyFor } from "../reportRefusalCopy";
import type { ReportTypeStepProps } from "../reportTypes";
import {
	lengthOf,
	type ReportWindow,
	type ThenAndNowDraft,
	thenEndOf,
	thenRefusalOf,
	type WindowsRelation,
	windowDaysOf,
	windowsOf,
	windowsRelationOf,
} from "./thenAndNowDraft";
import WindowLengthControl from "./WindowLengthControl";

const SAME_DAYS =
	"Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.";

const daysOf = (window: ReportWindow): string | null =>
	windowDaysOf(window.start, window.end);

const relationLineOf = (relation: WindowsRelation | null): string | null => {
	if (relation === null || relation.kind === "apart") return null;
	if (relation.kind === "sameDays") return SAME_DAYS;
	const until = parseLocalDate(relation.until);
	return until === null
		? null
		: `Now overlaps Then until ${format(until, LONG_DAY)}`;
};

const ThenStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	owner,
	ownerName,
	templates,
	draft,
	onChange,
	disabled,
	windowPresets,
	forSeveralOwners = false,
	frozen = false,
}) => {
	const end = thenEndOf(draft, templates);
	const windows = windowsOf(draft, templates);
	const relationLine = relationLineOf(windowsRelationOf(draft, templates));
	const refusalWordsFor = useRefusalWords(owner, ownerName);
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
				customLabel={forSeveralOwners ? "" : "Custom:"}
				customChoice={forSeveralOwners}
			/>
			{windows !== null && (
				<>
					<Typography color="text.secondary">
						{`Then ${daysOf(windows.then)} (${windows.then.lengthDays} days)`}
						{frozen && ` · ${FROZEN_NOTE}`}
					</Typography>
					<Typography color="text.secondary">
						{`Now ${daysOf(windows.now)} (${windows.now.lengthDays} days, ends today)`}
					</Typography>
				</>
			)}
			{relationLine !== null && (
				<Typography color="text.secondary">{relationLine}</Typography>
			)}
			{refusal !== null && (
				<Typography color="error" role="alert">
					{windowRefusalCopyFor(
						refusal,
						refusalWordsFor(templates, lengthOf(draft.thenLength)),
					)}
				</Typography>
			)}
		</Stack>
	);
};

export default ThenStep;

import {
	FormControlLabel,
	Radio,
	RadioGroup,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { useRefusalWords, windowRefusalCopyFor } from "../reportRefusalCopy";
import type { ReportTypeStepProps } from "../reportTypes";
import {
	lengthOf,
	nowLengthOf,
	nowRefusalOf,
	resolvedThenDays,
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
	windowPresets,
	forSeveralOwners = false,
}) => {
	const nowLength = nowLengthOf(draft);
	const nowDays = resolvedThenDays(templates.today, lengthOf(nowLength));
	const refusalWordsFor = useRefusalWords(owner);
	const refusal = nowRefusalOf(draft, templates);
	const followsThen = draft.nowLength === null;

	return (
		<Stack spacing={2}>
			{forSeveralOwners && (
				<RadioGroup
					aria-label="Now length"
					value={followsThen ? "same" : "custom"}
					onChange={(event) =>
						onChange({
							...draft,
							nowLength: event.target.value === "same" ? null : "",
						})
					}
				>
					<FormControlLabel
						value="same"
						control={<Radio />}
						label="Same as Then"
						disabled={disabled}
					/>
					<FormControlLabel
						value="custom"
						control={<Radio />}
						label="Custom"
						disabled={disabled}
					/>
				</RadioGroup>
			)}
			{!(forSeveralOwners && followsThen) && (
				<WindowLengthControl
					owner={owner}
					presets={windowPresets}
					length={nowLength}
					onChange={(length) => onChange({ ...draft, nowLength: length })}
					disabled={disabled}
					fieldLabel="Now length in days"
					presetsLabel="Now length presets"
					minimumLengthDays={templates.minimumLengthDays}
					leadingLabel={forSeveralOwners ? undefined : "Length"}
					customLabel={forSeveralOwners ? "" : "or"}
				/>
			)}
			{nowDays !== null && (
				<Stack spacing={0.5}>
					<Typography color="text.secondary">{`= ${nowDays}`}</Typography>
					<Typography variant="body2" color="text.secondary">
						Sliding window, always ending today
					</Typography>
				</Stack>
			)}
			{refusal !== null && (
				<Typography color="error" role="alert">
					{windowRefusalCopyFor(refusal, refusalWordsFor(templates))}
				</Typography>
			)}
		</Stack>
	);
};

export default NowStep;

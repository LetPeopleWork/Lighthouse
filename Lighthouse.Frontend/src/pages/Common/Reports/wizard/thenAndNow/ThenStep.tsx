import {
	Box,
	Stack,
	TextField,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import type React from "react";
import { reportWindowPresetsFor } from "../../panels/reportWindowPresets";
import { useRefusalWords, windowRefusalCopyFor } from "../reportRefusalCopy";
import type { ReportTypeStepProps } from "../reportTypes";
import { reportWindowCheck } from "../reportWindowCheck";
import {
	lengthOf,
	resolvedThenDays,
	type ThenAndNowDraft,
	thenEndOf,
} from "./thenAndNowDraft";

const ThenStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	owner,
	templates,
	draft,
	onChange,
	disabled,
}) => {
	const end = thenEndOf(draft, templates);
	const lengthDays = lengthOf(draft.thenLength);
	const presets = reportWindowPresetsFor(owner);
	const days = resolvedThenDays(end, lengthDays);
	const refusalWordsFor = useRefusalWords(owner);
	const refusal =
		lengthDays === null
			? null
			: reportWindowCheck({ side: "then", end, lengthDays }, templates);

	const chooseLength = (thenLength: string) =>
		onChange({ ...draft, thenLength });

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
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<ToggleButtonGroup
					exclusive
					size="small"
					disabled={disabled}
					value={presets.includes(lengthDays ?? 0) ? lengthDays : null}
					onChange={(_, preset: number | null) => {
						if (preset !== null) chooseLength(String(preset));
					}}
				>
					{presets.map((preset) => (
						<ToggleButton key={preset} value={preset}>
							{`${preset} days`}
						</ToggleButton>
					))}
				</ToggleButtonGroup>
				<Typography variant="body2">Custom:</Typography>
				<TextField
					type="number"
					size="small"
					value={draft.thenLength}
					disabled={disabled}
					onChange={(event) => chooseLength(event.target.value)}
					slotProps={{
						htmlInput: { "aria-label": "Then length in days", min: 1 },
					}}
					sx={{ width: 100 }}
				/>
				<Typography variant="body2">days</Typography>
			</Box>
			{days !== null && (
				<Typography color="text.secondary">{`= ${days}`}</Typography>
			)}
			{refusal !== null && (
				<Typography color="error">
					{windowRefusalCopyFor(refusal, refusalWordsFor(templates))}
				</Typography>
			)}
		</Stack>
	);
};

export default ThenStep;

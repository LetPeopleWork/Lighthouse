import {
	Checkbox,
	FormControlLabel,
	FormGroup,
	FormHelperText,
	Stack,
	TextField,
} from "@mui/material";
import type React from "react";
import type { ReportMetricKey } from "../../../../../models/Reports/Report";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { panelFor } from "../../panels/reportPanelRegistry";
import type { ReportTypeStepProps } from "../reportTypeSteps";
import {
	nameOf,
	offeredMetricsOf,
	shownMetricsOf,
	type ThenAndNowDraft,
} from "./thenAndNowDraft";

const NO_METRIC = "Choose at least one metric";

const MetricsAndNameStep: React.FC<
	Readonly<ReportTypeStepProps<ThenAndNowDraft>>
> = ({ templates, draft, onChange, disabled }) => {
	const { getTerm } = useTerminology();
	const shown = shownMetricsOf(draft, templates);

	const toggle = (key: ReportMetricKey, ticked: boolean) =>
		onChange({
			...draft,
			shownMetricKeys: ticked
				? [...shown, key]
				: shown.filter((shownKey) => shownKey !== key),
		});

	return (
		<Stack spacing={2}>
			<FormGroup>
				{offeredMetricsOf(templates).map((key) => (
					<FormControlLabel
						key={key}
						label={getTerm(panelFor(key)?.titleTermKey ?? key)}
						control={
							<Checkbox
								checked={shown.includes(key)}
								disabled={disabled}
								onChange={(event) => toggle(key, event.target.checked)}
							/>
						}
					/>
				))}
				{shown.length === 0 && (
					<FormHelperText error>{NO_METRIC}</FormHelperText>
				)}
			</FormGroup>
			<TextField
				label="Name"
				value={nameOf(draft, templates)}
				disabled={disabled}
				onChange={(event) => onChange({ ...draft, name: event.target.value })}
			/>
		</Stack>
	);
};

export default MetricsAndNameStep;

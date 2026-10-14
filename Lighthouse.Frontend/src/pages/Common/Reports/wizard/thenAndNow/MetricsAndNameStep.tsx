import {
	Checkbox,
	FormControlLabel,
	FormGroup,
	FormHelperText,
	Stack,
	TextField,
} from "@mui/material";
import type React from "react";
import { Fragment } from "react";
import type { ReportMetricKey } from "../../../../../models/Reports/Report";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { metricNameTermKeyOf } from "../../panels/reportPanelRegistry";
import type { ReportTypeStepProps } from "../reportTypes";
import {
	isCaptured,
	nameOf,
	offeredMetricsOf,
	shownMetricsOf,
	type ThenAndNowDraft,
} from "./thenAndNowDraft";

const NO_METRIC = "Choose at least one metric";
const NOT_CAPTURED = "Not captured for this report";
const NAME_MAX_LENGTH = 200;

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
				{offeredMetricsOf(templates).map((key) => {
					const captured = isCaptured(draft, key);
					return (
						<Fragment key={key}>
							<FormControlLabel
								label={getTerm(metricNameTermKeyOf(key))}
								control={
									<Checkbox
										checked={shown.includes(key)}
										disabled={disabled || !captured}
										onChange={(event) => toggle(key, event.target.checked)}
									/>
								}
							/>
							{!captured && (
								<FormHelperText sx={{ mt: 0, ml: 4 }}>
									{NOT_CAPTURED}
								</FormHelperText>
							)}
						</Fragment>
					);
				})}
				{shown.length === 0 && (
					<FormHelperText error>{NO_METRIC}</FormHelperText>
				)}
			</FormGroup>
			<TextField
				label="Name"
				value={nameOf(draft, templates)}
				disabled={disabled}
				slotProps={{ htmlInput: { maxLength: NAME_MAX_LENGTH } }}
				onChange={(event) => onChange({ ...draft, name: event.target.value })}
			/>
		</Stack>
	);
};

export default MetricsAndNameStep;

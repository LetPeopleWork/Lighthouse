import { Box, InputAdornment, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useRef } from "react";
import type { IRefinementBandSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import InfoTooltip from "../../../pages/Teams/Detail/Refinement/InfoTooltip";
import { describeLikelihoods } from "../../../pages/Teams/Detail/Refinement/needWording";
import { useTerminology } from "../../../services/TerminologyContext";
import { shownNumber } from "../../../utils/numberField";
import {
	bandErrors,
	DEFAULT_BAND,
	HIGHEST_LIKELIHOOD,
	hasBandErrors,
	LOWEST_LIKELIHOOD,
} from "./refinementBand";

type BandEnd = keyof IRefinementBandSetting;

const BAND_ENDS: { end: BandEnd; label: string }[] = [
	{ end: "lowPercentile", label: "Low end likelihood" },
	{ end: "highPercentile", label: "High end likelihood" },
];

interface RefinementBandSettingsProps {
	band: IRefinementBandSetting;
	onChange: (band: IRefinementBandSetting) => void;
}

/** The likelihoods the Team's forecast is read at for the two ends of the range the tab shows. */
const RefinementBandSettings: React.FC<
	Readonly<RefinementBandSettingsProps>
> = ({ band, onChange }) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const errors = bandErrors(band);
	const isValid = !hasBandErrors(band);
	// A cleared or refused field must not reach the explanation as "NaN%" or a wrong likelihood.
	const lastValidBand = useRef(isValid ? band : DEFAULT_BAND);
	if (isValid) {
		lastValidBand.current = band;
	}
	const origin =
		`Based on the ${teamTerm}'s ${getTerm(TERMINOLOGY_KEYS.THROUGHPUT)}: ` +
		`a How Many forecast for the working days until the next ${refinementTerm}. ` +
		describeLikelihoods({ ...lastValidBand.current, teamTerm });

	return (
		<>
			<Grid size={{ xs: 12 }}>
				<Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
					<Typography variant="subtitle1">
						{workItemsTerm} needed before the next {refinementTerm}
					</Typography>
					<InfoTooltip text={origin} />
				</Box>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<Box
					sx={{
						display: "flex",
						flexWrap: "wrap",
						gap: 2,
						alignItems: "flex-start",
					}}
				>
					{BAND_ENDS.map(({ end, label }) => (
						<TextField
							key={end}
							label={label}
							type="number"
							size="small"
							sx={{ width: 180 }}
							value={shownNumber(band[end])}
							error={errors[end] !== null}
							helperText={errors[end]}
							onChange={(event) =>
								onChange({
									...band,
									[end]: Number.parseInt(event.target.value, 10),
								})
							}
							slotProps={{
								input: {
									endAdornment: (
										<InputAdornment position="end">%</InputAdornment>
									),
								},
								htmlInput: {
									min: LOWEST_LIKELIHOOD,
									max: HIGHEST_LIKELIHOOD,
									step: 1,
								},
							}}
						/>
					))}
				</Box>
			</Grid>
		</>
	);
};

export default RefinementBandSettings;

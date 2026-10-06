import { Box, InputAdornment, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useId, useState } from "react";
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
	isBandInverted,
	LOWEST_LIKELIHOOD,
} from "./refinementBand";

type BandEnd = keyof IRefinementBandSetting;

const BAND_ENDS: { end: BandEnd; label: string }[] = [
	{ end: "lowPercentile", label: "Low end likelihood" },
	{ end: "highPercentile", label: "High end likelihood" },
];

// Unlike parseInt, a decimal stays a decimal, so it is refused instead of being saved rounded down.
const likelihoodTyped = (text: string): number =>
	text === "" ? Number.NaN : Number(text);

const isSameBand = (
	one: IRefinementBandSetting,
	other: IRefinementBandSetting,
): boolean =>
	one.lowPercentile === other.lowPercentile &&
	one.highPercentile === other.highPercentile;

interface RefinementBandSettingsProps {
	band: IRefinementBandSetting;
	onChange: (band: IRefinementBandSetting) => void;
}

/** The likelihoods the Team's forecast is read at for the two ends of the range the tab shows. */
const RefinementBandSettings: React.FC<
	Readonly<RefinementBandSettingsProps>
> = ({ band, onChange }) => {
	const { getTerm } = useTerminology();
	const messageIdPrefix = useId();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const errors = bandErrors(band);
	const inverted = isBandInverted(band);
	const isValid = !hasBandErrors(band);

	// A cleared or refused field must not reach the explanation as "NaN%" or a wrong likelihood.
	const [lastValidBand, setLastValidBand] = useState(
		isValid ? band : DEFAULT_BAND,
	);
	if (isValid && !isSameBand(band, lastValidBand)) {
		setLastValidBand(band);
	}
	const explainedBand = isValid ? band : lastValidBand;

	const origin =
		`Based on the ${teamTerm}'s ${getTerm(TERMINOLOGY_KEYS.THROUGHPUT)}: ` +
		`a How Many forecast for the working days between the next ${refinementTerm} and the one after. ` +
		describeLikelihoods({ ...explainedBand, teamTerm });

	const messageId = (end: BandEnd) => `${messageIdPrefix}-${end}-message`;
	// An inverted band is explained once, under the low end, but the high end is just as wrong,
	// so an admin typing in the high end hears the same explanation there.
	const describedBy = (end: BandEnd): string | undefined => {
		if (errors[end] !== null) {
			return messageId(end);
		}
		return inverted ? messageId("lowPercentile") : undefined;
	};

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
							error={errors[end] !== null || inverted}
							helperText={errors[end]}
							onChange={(event) =>
								onChange({
									...band,
									[end]: likelihoodTyped(event.target.value),
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
									"aria-describedby": describedBy(end),
								},
								formHelperText: { id: messageId(end) },
							}}
						/>
					))}
				</Box>
			</Grid>
		</>
	);
};

export default RefinementBandSettings;

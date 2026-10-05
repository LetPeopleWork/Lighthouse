import { Box, InputAdornment, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useRef } from "react";
import type {
	IRefinementBandSetting,
	IRefinementSettings,
	IRefinementStateSetting,
} from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import InfoTooltip from "../../../pages/Teams/Detail/Refinement/InfoTooltip";
import { describeLikelihoods } from "../../../pages/Teams/Detail/Refinement/needWording";
import { useTerminology } from "../../../services/TerminologyContext";
import { shownNumber } from "../../../utils/numberField";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";
import ReadinessSettings, {
	DEFAULT_READINESS,
	hasReadinessErrors,
} from "./ReadinessSettings";
import RefinementCadenceSettings, {
	hasCadenceErrors,
} from "./RefinementCadenceSettings";
import { bandErrors, hasBandErrors } from "./refinementBand";
import StageRulesSettings, {
	hasIncompleteStageRule,
	NO_STAGE_RULES,
} from "./StageRulesSettings";

/** Why the Refinement settings cannot be saved as they are; empty when they can. */
export const refinementSettingsBlockers = (
	refinement: IRefinementSettings | null | undefined,
	refinementTerm: string,
): string[] => {
	const reasons: string[] = [];
	const readiness = refinement?.readiness;
	if (readiness && hasReadinessErrors(readiness)) {
		reasons.push("Correct Readiness by votes");
	}
	if (hasIncompleteStageRule(refinement?.stageRules)) {
		reasons.push("Complete the stage rules");
	}
	const cadence = refinement?.cadence;
	if (cadence && hasCadenceErrors(cadence)) {
		reasons.push(`Complete the ${refinementTerm} cadence`);
	}
	const band = refinement?.band;
	if (band && hasBandErrors(band)) {
		reasons.push("Correct the low and high end likelihoods");
	}
	return reasons;
};

// What the server reads the Team's forecast at until the admin chooses otherwise.
const DEFAULT_BAND: IRefinementBandSetting = {
	lowPercentile: 50,
	highPercentile: 85,
};

type BandEnd = keyof IRefinementBandSetting;

const BAND_ENDS: { end: BandEnd; label: string }[] = [
	{ end: "lowPercentile", label: "Low end likelihood" },
	{ end: "highPercentile", label: "High end likelihood" },
];

interface BandSettingsProps {
	band: IRefinementBandSetting;
	onChange: (band: IRefinementBandSetting) => void;
}

/** The likelihoods the Team's forecast is read at for the two ends of the range the tab shows. */
const BandSettings: React.FC<Readonly<BandSettingsProps>> = ({
	band,
	onChange,
}) => {
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
			{BAND_ENDS.map(({ end, label }) => (
				<Grid key={end} size={{ xs: 12, sm: 6 }}>
					<TextField
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
								endAdornment: <InputAdornment position="end">%</InputAdornment>,
							},
							htmlInput: { min: 50, max: 95, step: 1 },
						}}
					/>
				</Grid>
			))}
		</>
	);
};

interface RefinementSettingsSectionProps {
	teamId: number;
	toDoStates: string[];
	doingStates: string[];
	refinement: IRefinementSettings | null | undefined;
	/** Receives only the part of the settings that changed. */
	onChange: (change: Partial<IRefinementSettings>) => void;
}

const toSettings = (states: string[]): IRefinementStateSetting[] =>
	states.map((state) => ({ state }));

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	teamId,
	toDoStates,
	doingStates,
	refinement,
	onChange,
}) => {
	const { getTerm } = useTerminology();

	const chosenStates = refinement?.states ?? [];
	const suggestions = [...toDoStates, ...doingStates];
	const offered = new Set(suggestions.map((state) => state.toLowerCase()));
	const chosen = chosenStates
		.map((entry) => entry.state)
		.filter((state) => offered.has(state.toLowerCase()));

	const save = (states: string[]) => onChange({ states: toSettings(states) });

	// Only To Do and Doing states can mean refinement, so a chosen state leaves as soon as the admin stops
	// offering it, and the same save that changes the states carries the shorter list. Opening the form is
	// not an edit: a stored state that is not offered is only hidden, and the server drops it on any save.
	const offeredKey = [...offered].join("\n");
	const lastOfferedKey = useRef(offeredKey);
	const anyNoLongerOffered = chosen.length < chosenStates.length;
	useEffect(() => {
		if (lastOfferedKey.current === offeredKey) {
			return;
		}
		lastOfferedKey.current = offeredKey;
		if (anyNoLongerOffered) {
			onChange({ states: toSettings(chosen) });
		}
	}, [offeredKey, anyNoLongerOffered, chosen, onChange]);

	// The list accepts free text, but the server refuses anything that is not a To Do or Doing state,
	// so a typed value only counts when it names one of the suggestions.
	const add = (typed: string) => {
		const wanted = typed.trim().toLowerCase();
		const match = suggestions.find((state) => state.toLowerCase() === wanted);
		if (match && !chosen.includes(match)) {
			save([...chosen, match]);
		}
	};

	const remove = (state: string) =>
		save(chosen.filter((name) => name !== state));

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);

	return (
		<InputGroup title={refinementTerm}>
			<Grid size={{ xs: 12 }}>
				<ItemListManager
					title={`${refinementTerm} State`}
					items={chosen}
					onAddItem={add}
					onRemoveItem={remove}
					suggestions={suggestions}
					isLoading={false}
				/>
			</Grid>
			<ReadinessSettings
				readiness={refinement?.readiness ?? DEFAULT_READINESS}
				onChange={(readiness) => onChange({ readiness })}
			/>
			<StageRulesSettings
				teamId={teamId}
				stageRules={refinement?.stageRules ?? NO_STAGE_RULES}
				onChange={(stageRules) => onChange({ stageRules })}
			/>
			<RefinementCadenceSettings
				cadence={refinement?.cadence ?? null}
				onChange={(cadence) => onChange({ cadence })}
			/>
			<BandSettings
				band={refinement?.band ?? DEFAULT_BAND}
				onChange={(band) => onChange({ band })}
			/>
		</InputGroup>
	);
};

export default RefinementSettingsSection;

import { TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import {
	type DayOfWeek,
	ORDERED_WEEKDAYS,
} from "../../../models/RecurringBlackoutRule";
import type { IRefinementCadenceSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { isAtLeastOne } from "../../../utils/numberField";
import WeeklyRecurrenceFields from "../WeeklyRecurrence/WeeklyRecurrenceFields";

export const NO_CADENCE: IRefinementCadenceSetting = {
	weekdays: [],
	intervalWeeks: 1,
	anchorWeek: null,
};

// Refinements less often than weekly need a week to count from, or the server cannot tell which weeks
// they fall in.
const asksForStartingWeek = (intervalWeeks: number): boolean =>
	intervalWeeks > 1;

// The server reads the starting week only when the Team refines less often than weekly. It is dropped
// only at exactly every week, so clearing the number of weeks to type another does not lose it.
export const cadenceOf = (
	weekdays: DayOfWeek[],
	intervalWeeks: number,
	anchorWeek: string | null,
): IRefinementCadenceSetting => ({
	weekdays: ORDERED_WEEKDAYS.filter((day) => weekdays.includes(day)),
	intervalWeeks,
	anchorWeek: intervalWeeks === 1 ? null : anchorWeek,
});

// The server looks a year ahead for the next Refinement, so a longer gap could leave it none to find.
const MOST_WEEKS_APART = 52;

const isTooFarApart = (intervalWeeks: number): boolean =>
	intervalWeeks > MOST_WEEKS_APART;

interface CadenceErrors {
	intervalWeeks: boolean;
	anchorWeek: boolean;
}

export const cadenceErrors = (
	cadence: IRefinementCadenceSetting,
): CadenceErrors => {
	const everyFewWeeks =
		isAtLeastOne(cadence.intervalWeeks) &&
		!isTooFarApart(cadence.intervalWeeks);
	return {
		intervalWeeks: !everyFewWeeks,
		anchorWeek:
			everyFewWeeks &&
			asksForStartingWeek(cadence.intervalWeeks) &&
			!cadence.anchorWeek,
	};
};

export const hasCadenceErrors = (cadence: IRefinementCadenceSetting): boolean =>
	Object.values(cadenceErrors(cadence)).some(Boolean);

interface RefinementCadenceSettingsProps {
	cadence: IRefinementCadenceSetting | null;
	onChange: (cadence: IRefinementCadenceSetting) => void;
}

/** The days a Team refines on. */
const RefinementCadenceSettings: React.FC<RefinementCadenceSettingsProps> = ({
	cadence,
	onChange,
}) => {
	const { getTerm } = useTerminology();
	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);

	const shownCadence = cadence ?? NO_CADENCE;
	const changeCadence = (
		weekdays: DayOfWeek[],
		intervalWeeks: number,
		anchorWeek = shownCadence.anchorWeek ?? null,
	) => onChange(cadenceOf(weekdays, intervalWeeks, anchorWeek));
	const errors = cadenceErrors(shownCadence);
	const refinementsTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENTS);
	const intervalWeeksError = isTooFarApart(shownCadence.intervalWeeks)
		? `${refinementsTerm} are at most ${MOST_WEEKS_APART} weeks apart.`
		: `${refinementsTerm} are at least one week apart.`;
	const toggleWeekday = (day: DayOfWeek) =>
		changeCadence(
			shownCadence.weekdays.includes(day)
				? shownCadence.weekdays.filter((selected) => selected !== day)
				: [...shownCadence.weekdays, day],
			shownCadence.intervalWeeks,
		);

	return (
		<Grid size={{ xs: 12 }}>
			<Typography variant="subtitle1">{`${refinementTerm} cadence`}</Typography>
			<WeeklyRecurrenceFields
				weekdays={shownCadence.weekdays}
				intervalWeeks={shownCadence.intervalWeeks}
				onToggleWeekday={toggleWeekday}
				onIntervalWeeksChange={(intervalWeeks) =>
					changeCadence(shownCadence.weekdays, intervalWeeks)
				}
				intervalWeeksError={errors.intervalWeeks ? intervalWeeksError : null}
			/>
			{asksForStartingWeek(shownCadence.intervalWeeks) && (
				<TextField
					label="Starting week"
					type="date"
					fullWidth
					value={shownCadence.anchorWeek ?? ""}
					onChange={(event) =>
						changeCadence(
							shownCadence.weekdays,
							shownCadence.intervalWeeks,
							event.target.value || null,
						)
					}
					error={errors.anchorWeek}
					helperText={
						errors.anchorWeek
							? `Pick a day in the week of a ${refinementTerm} to count the weeks from.`
							: null
					}
					slotProps={{ inputLabel: { shrink: true } }}
				/>
			)}
		</Grid>
	);
};

export default RefinementCadenceSettings;

import {
	Checkbox,
	FormControlLabel,
	FormGroup,
	TextField,
} from "@mui/material";
import type React from "react";
import {
	type DayOfWeek,
	ORDERED_WEEKDAYS,
} from "../../../models/RecurringBlackoutRule";

interface WeeklyRecurrenceFieldsProps {
	weekdays: DayOfWeek[];
	intervalWeeks: number;
	onToggleWeekday: (day: DayOfWeek) => void;
	/** Receives the typed number of weeks, NaN while the field is empty. */
	onIntervalWeeksChange: (intervalWeeks: number) => void;
	/** Marks the number of weeks invalid and says why underneath. */
	intervalWeeksError?: string | null;
	testIdPrefix?: string;
}

const testIdOf = (prefix: string | undefined, name: string) =>
	prefix ? `${prefix}-${name}` : undefined;

const WeeklyRecurrenceFields: React.FC<WeeklyRecurrenceFieldsProps> = ({
	weekdays,
	intervalWeeks,
	onToggleWeekday,
	onIntervalWeeksChange,
	intervalWeeksError = null,
	testIdPrefix,
}) => (
	<>
		<FormGroup row>
			{ORDERED_WEEKDAYS.map((day) => (
				<FormControlLabel
					key={day}
					control={
						<Checkbox
							checked={weekdays.includes(day)}
							onChange={() => onToggleWeekday(day)}
							data-testid={testIdOf(testIdPrefix, `weekday-${day}`)}
						/>
					}
					label={day}
				/>
			))}
		</FormGroup>
		<TextField
			label="Repeat every (weeks)"
			type="number"
			// A cleared number field holds NaN, which the input shows as empty rather than as "NaN".
			value={Number.isNaN(intervalWeeks) ? "" : intervalWeeks}
			onChange={(e) =>
				onIntervalWeeksChange(Number.parseInt(e.target.value, 10))
			}
			error={intervalWeeksError !== null}
			helperText={intervalWeeksError}
			fullWidth
			sx={{ mt: 2, mb: 2 }}
			slotProps={{ htmlInput: { min: 1 } }}
			data-testid={testIdOf(testIdPrefix, "interval-weeks")}
		/>
	</>
);

export default WeeklyRecurrenceFields;

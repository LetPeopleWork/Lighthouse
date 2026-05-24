import { FormControlLabel, Switch } from "@mui/material";
import { type ChangeEvent, type ReactElement, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";

export type ThroughputChartFilterToggleMode = "raw" | "filtered";

export interface ThroughputChartFilterToggleProps {
	readonly isPremium: boolean;
	readonly hasFilter: boolean;
	readonly onChange: (filtered: boolean) => void;
}

const ThroughputChartFilterToggle = ({
	isPremium,
	hasFilter,
	onChange,
}: ThroughputChartFilterToggleProps): ReactElement | null => {
	const [filtered, setFiltered] = useState(false);
	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);

	if (!isPremium || !hasFilter) {
		return null;
	}

	const handleChange = (
		_event: ChangeEvent<HTMLInputElement>,
		next: boolean,
	) => {
		setFiltered(next);
		onChange(next);
	};

	return (
		<FormControlLabel
			control={
				<Switch checked={filtered} onChange={handleChange} size="small" />
			}
			label={`Use filtered ${throughputTerm}`}
			sx={{ m: 0 }}
		/>
	);
};

export default ThroughputChartFilterToggle;

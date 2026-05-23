import { Chip, Tooltip } from "@mui/material";
import type React from "react";

const FILTER_EXCLUDED_ALL_FALLBACK_SUMMARY =
	"Filter excluded all throughput; showing unfiltered forecast";

export interface FilteredThroughputChipProps {
	excludedSummary?: string;
	visible: boolean;
}

const FilteredThroughputChip: React.FC<FilteredThroughputChipProps> = ({
	excludedSummary,
	visible,
}) => {
	if (!visible) {
		return null;
	}

	const isFallback = excludedSummary === FILTER_EXCLUDED_ALL_FALLBACK_SUMMARY;
	const tooltipTitle = excludedSummary ?? "";

	return (
		<Tooltip title={tooltipTitle} arrow>
			<Chip
				label="Filtered throughput"
				size="small"
				color={isFallback ? "warning" : "default"}
				variant="outlined"
			/>
		</Tooltip>
	);
};

export default FilteredThroughputChip;

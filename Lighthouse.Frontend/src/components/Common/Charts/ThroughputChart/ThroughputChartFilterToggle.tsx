import {
	Box,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import { type ReactElement, useMemo, useState } from "react";
import {
	type EvaluableWorkItem,
	type EvaluatorCondition,
	matchesAllConditions,
} from "./evaluateCondition";

export type ThroughputChartFilterToggleChartKind = "runChart" | "pbc";

export type ThroughputChartFilterToggleMode = "raw" | "filtered";

const EMPTY_STATE_MESSAGE =
	"No items match the throughput filter in this window. Switch to Raw to see total throughput.";

export interface ThroughputChartFilterToggleProps {
	readonly isPremium: boolean;
	readonly hasFilter: boolean;
	readonly chartKind: ThroughputChartFilterToggleChartKind;
	readonly conditions: readonly EvaluatorCondition[];
	readonly items?: readonly EvaluableWorkItem[];
	readonly onClientFiltered?: (
		filteredItems: readonly EvaluableWorkItem[],
		total: number,
	) => void;
	readonly onServerViewChange?: (view: ThroughputChartFilterToggleMode) => void;
}

const ThroughputChartFilterToggle = ({
	isPremium,
	hasFilter,
	chartKind,
	conditions,
	items,
	onClientFiltered,
	onServerViewChange,
}: ThroughputChartFilterToggleProps): ReactElement | null => {
	const [mode, setMode] = useState<ThroughputChartFilterToggleMode>("raw");

	const shouldRender = isPremium && hasFilter;

	const filteredItems = useMemo(() => {
		if (!items) return [];
		return items.filter((item) => matchesAllConditions(item, conditions));
	}, [items, conditions]);

	if (!shouldRender) return null;

	const handleModeChange = (
		_: React.MouseEvent<HTMLElement>,
		next: ThroughputChartFilterToggleMode | null,
	) => {
		if (next === null || next === mode) return;
		setMode(next);
		if (chartKind === "runChart") {
			onClientFiltered?.(filteredItems, items?.length ?? 0);
			return;
		}
		onServerViewChange?.(next);
	};

	const isFilteredActive = mode === "filtered";
	const showEmptyState =
		isFilteredActive &&
		chartKind === "runChart" &&
		(items?.length ?? 0) > 0 &&
		filteredItems.length === 0;

	return (
		<Box
			sx={{
				display: "flex",
				alignItems: "center",
				gap: 1,
				flexWrap: "wrap",
			}}
		>
			<ToggleButtonGroup
				value={mode}
				exclusive
				size="small"
				onChange={handleModeChange}
				aria-label="Throughput filter view"
			>
				<ToggleButton value="raw" aria-label="Raw">
					Raw
				</ToggleButton>
				<ToggleButton value="filtered" aria-label="Filtered">
					Filtered
				</ToggleButton>
			</ToggleButtonGroup>
			{showEmptyState && (
				<Typography variant="body2" color="text.secondary">
					{EMPTY_STATE_MESSAGE}
				</Typography>
			)}
		</Box>
	);
};

export default ThroughputChartFilterToggle;

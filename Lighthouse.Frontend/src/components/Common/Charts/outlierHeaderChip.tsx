import type { ReactNode, Ref } from "react";
import type { IWorkItem } from "../../../models/WorkItem";
import type { OutlierWording } from "../../../utils/charts/outlierWording";
import OutlierChip from "./OutlierChip";
import type { IPlotLine } from "./outlierPlot";

interface OutlierHeaderChipInput {
	readonly wording: OutlierWording;
	/** The outliers of the visible types, longest first. */
	readonly outliers: readonly IWorkItem[];
	readonly offScaleLines: readonly IPlotLine[];
	readonly disabled: boolean;
	/** The verdict is on its way: the chip holds its place, disabled and without a count. */
	readonly verdictPending: boolean;
	readonly onOpen: () => void;
	readonly chipRef: Ref<HTMLDivElement>;
}

/** The chip for the widget header, or nothing when no outlier of a visible type is left out of the plot. */
export function outlierHeaderChip({
	wording,
	outliers,
	offScaleLines,
	disabled,
	verdictPending,
	onOpen,
	chipRef,
}: OutlierHeaderChipInput): ReactNode {
	if (outliers.length === 0 && !verdictPending) return null;

	return (
		<OutlierChip
			wording={wording}
			count={outliers.length}
			aboveTheAxis={
				offScaleLines.length > 0
					? wording.aboveTheAxis(offScaleLines.map((line) => line.aboveAxis))
					: null
			}
			disabled={disabled || verdictPending}
			onOpen={onOpen}
			chipRef={chipRef}
		/>
	);
}

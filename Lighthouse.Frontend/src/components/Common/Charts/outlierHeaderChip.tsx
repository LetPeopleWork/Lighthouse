import type { ReactNode, Ref } from "react";
import type { IWorkItem } from "../../../models/WorkItem";
import type { OutlierWording } from "../../../utils/charts/outlierWording";
import OutlierChip from "./OutlierChip";
import { buildOutlierRows, type IPlotLine } from "./outlierPlot";

export const OUTLIER_POPOVER_ROWS = 10;

interface OutlierHeaderChipInput {
	readonly wording: OutlierWording;
	/** The outliers of the visible types, longest first. */
	readonly outliers: readonly IWorkItem[];
	readonly median: number | null;
	readonly cut: number | null;
	readonly offScaleLines: readonly IPlotLine[];
	readonly disabled: boolean;
	readonly onOpened: () => void;
	readonly onOpenInList: () => void;
	readonly chipRef: Ref<HTMLDivElement>;
}

/** The chip for the widget header, or nothing when no outlier of a visible type is left out of the plot. */
export function outlierHeaderChip({
	wording,
	outliers,
	median,
	cut,
	offScaleLines,
	disabled,
	onOpened,
	onOpenInList,
	chipRef,
}: OutlierHeaderChipInput): ReactNode {
	if (outliers.length === 0 || median === null || cut === null) return null;

	return (
		<OutlierChip
			wording={wording}
			count={outliers.length}
			cut={cut}
			median={median}
			rows={buildOutlierRows(outliers.slice(0, OUTLIER_POPOVER_ROWS), median)}
			moreCount={Math.max(outliers.length - OUTLIER_POPOVER_ROWS, 0)}
			aboveTheAxis={
				offScaleLines.length > 0
					? wording.aboveTheAxis(offScaleLines.map((line) => line.aboveAxis))
					: null
			}
			disabled={disabled}
			onOpened={onOpened}
			onOpenInList={onOpenInList}
			chipRef={chipRef}
		/>
	);
}

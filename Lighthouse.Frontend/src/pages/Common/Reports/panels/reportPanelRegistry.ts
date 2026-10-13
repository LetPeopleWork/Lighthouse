import type React from "react";
import type {
	IReportMetric,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import CycleTimePanel from "./CycleTimePanel";
import type { DirectionOfGood } from "./thenNowComparison";

/** Everything a panel draws comes in through its props, so the same props always draw the same panel. */
export interface ReportPanelProps {
	title: string;
	metric: IReportMetric;
	workItems: string;
	directionOfGood: DirectionOfGood;
}

export interface ReportPanelDefinition {
	component: React.FC<Readonly<ReportPanelProps>>;
	titleTermKey: string;
	directionOfGood: DirectionOfGood;
}

/** One entry per metric this version can show, in the order the panels stack. */
export const reportPanelRegistry = {
	"cycle-time": {
		component: CycleTimePanel,
		titleTermKey: TERMINOLOGY_KEYS.CYCLE_TIME,
		directionOfGood: "lower",
	},
} satisfies Partial<Record<ReportMetricKey, ReportPanelDefinition>>;

export const panelFor = (
	key: ReportMetricKey,
): ReportPanelDefinition | undefined =>
	(
		reportPanelRegistry as Partial<
			Record<ReportMetricKey, ReportPanelDefinition>
		>
	)[key];

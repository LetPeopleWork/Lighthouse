import type React from "react";
import type {
	IReportMetric,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import ArrivalsPanel, { ARRIVALS_CHANGE_LINES } from "./ArrivalsPanel";
import CycleTimePanel, { PERCENTILES } from "./CycleTimePanel";
import { SPREAD_LINES } from "./DailySpreadPanel";
import ThroughputPanel, { CHANGE_LINES } from "./ThroughputPanel";
import type { ChangeLine, DirectionOfGood } from "./thenNowComparison";
import WipPanel from "./WipPanel";
import WorkItemAgePanel from "./WorkItemAgePanel";

/** Everything a panel draws comes in through its props, so the same props always draw the same panel. */
export interface ReportPanelProps {
	title: string;
	metric: IReportMetric;
	workItem: string;
	workItems: string;
	directionOfGood: DirectionOfGood;
}

export interface ReportPanelDefinition {
	component: React.FC<Readonly<ReportPanelProps>>;
	titleOf: (getTerm: (key: string) => string) => string;
	directionOfGood: DirectionOfGood;
	/** The lines the panel draws a change mark on, in its row order. */
	changeLines: readonly ChangeLine[];
}

const ARRIVALS_NAME = "Arrivals";

/** Every metric in the order its panel stacks; each is named in the instance's words, or by a fixed name it has no term for. */
const METRIC_CATALOG: readonly (
	| { key: ReportMetricKey; nameTermKey: string }
	| { key: ReportMetricKey; fixedName: string }
)[] = [
	{ key: "cycle-time", nameTermKey: TERMINOLOGY_KEYS.CYCLE_TIME },
	{ key: "throughput", nameTermKey: TERMINOLOGY_KEYS.THROUGHPUT },
	{ key: "wip", nameTermKey: TERMINOLOGY_KEYS.WIP },
	{ key: "work-item-age", nameTermKey: TERMINOLOGY_KEYS.WORK_ITEM_AGE },
	{ key: "arrivals", fixedName: ARRIVALS_NAME },
];

/** One entry per metric this version can show; a report from a later version may still name a key with none. */
export const reportPanelRegistry = {
	"cycle-time": {
		component: CycleTimePanel,
		titleOf: (getTerm) => getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
		directionOfGood: "lower",
		changeLines: PERCENTILES,
	},
	throughput: {
		component: ThroughputPanel,
		titleOf: (getTerm) => getTerm(TERMINOLOGY_KEYS.THROUGHPUT),
		directionOfGood: "higher",
		changeLines: CHANGE_LINES,
	},
	wip: {
		component: WipPanel,
		titleOf: (getTerm) => getTerm(TERMINOLOGY_KEYS.WIP),
		directionOfGood: "lower",
		changeLines: SPREAD_LINES,
	},
	"work-item-age": {
		component: WorkItemAgePanel,
		titleOf: (getTerm) => `Total ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE)}`,
		directionOfGood: "lower",
		changeLines: SPREAD_LINES,
	},
	arrivals: {
		component: ArrivalsPanel,
		titleOf: () => ARRIVALS_NAME,
		directionOfGood: "lower",
		changeLines: ARRIVALS_CHANGE_LINES,
	},
} satisfies Record<ReportMetricKey, ReportPanelDefinition>;

export const panelFor = (
	key: ReportMetricKey,
): ReportPanelDefinition | undefined =>
	(
		reportPanelRegistry as Partial<
			Record<ReportMetricKey, ReportPanelDefinition>
		>
	)[key];

/** What a metric is called: the instance's word for it, a fixed name, or for a metric from a later version its key. */
export const metricNameOf = (
	key: ReportMetricKey,
	getTerm: (key: string) => string,
): string => {
	const entry = METRIC_CATALOG.find((candidate) => candidate.key === key);
	if (entry === undefined) return key;
	return "fixedName" in entry ? entry.fixedName : getTerm(entry.nameTermKey);
};

const catalogPositionOf = (key: ReportMetricKey): number => {
	const position = METRIC_CATALOG.findIndex((entry) => entry.key === key);
	return position === -1 ? METRIC_CATALOG.length : position;
};

/** The catalog's order whatever order the metrics were ticked in; a metric from a later version goes last. */
export const inCatalogOrder = (
	keys: readonly ReportMetricKey[],
): ReportMetricKey[] =>
	[...keys].sort((a, b) => catalogPositionOf(a) - catalogPositionOf(b));

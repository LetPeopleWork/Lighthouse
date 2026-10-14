import type React from "react";
import type {
	IReportMetric,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import CycleTimePanel from "./CycleTimePanel";
import ThroughputPanel from "./ThroughputPanel";
import type { DirectionOfGood } from "./thenNowComparison";

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
	titleTermKey: string;
	directionOfGood: DirectionOfGood;
}

/** Every metric in the order its panel stacks, each named in the instance's words. */
const METRIC_CATALOG: readonly {
	key: ReportMetricKey;
	nameTermKey: string;
}[] = [
	{ key: "cycle-time", nameTermKey: TERMINOLOGY_KEYS.CYCLE_TIME },
	{ key: "throughput", nameTermKey: TERMINOLOGY_KEYS.THROUGHPUT },
	{ key: "wip", nameTermKey: TERMINOLOGY_KEYS.WIP },
	{ key: "work-item-age", nameTermKey: TERMINOLOGY_KEYS.WORK_ITEM_AGE },
];

/** One entry per metric this version can show. */
export const reportPanelRegistry = {
	"cycle-time": {
		component: CycleTimePanel,
		titleTermKey: TERMINOLOGY_KEYS.CYCLE_TIME,
		directionOfGood: "lower",
	},
	throughput: {
		component: ThroughputPanel,
		titleTermKey: TERMINOLOGY_KEYS.THROUGHPUT,
		directionOfGood: "higher",
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

/** The term naming a metric; a metric from a later version has none and goes by its key. */
export const metricNameTermKeyOf = (key: ReportMetricKey): string =>
	METRIC_CATALOG.find((entry) => entry.key === key)?.nameTermKey ?? key;

const catalogPositionOf = (key: ReportMetricKey): number => {
	const position = METRIC_CATALOG.findIndex((entry) => entry.key === key);
	return position === -1 ? METRIC_CATALOG.length : position;
};

/** The catalog's order whatever order the metrics were ticked in; a metric from a later version goes last. */
export const inCatalogOrder = (
	keys: readonly ReportMetricKey[],
): ReportMetricKey[] =>
	[...keys].sort((a, b) => catalogPositionOf(a) - catalogPositionOf(b));

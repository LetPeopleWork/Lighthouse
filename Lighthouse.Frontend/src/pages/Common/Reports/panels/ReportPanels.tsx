import type React from "react";
import type { IReport, IReportMetric } from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import {
	inCatalogOrder,
	metricNameTermKeyOf,
	panelFor,
} from "./reportPanelRegistry";
import UnsupportedMetricPanel from "./UnsupportedMetricPanel";

/** A metric shown on a report made before it was captured reads as not captured on both sides. */
const NOTHING_CAPTURED: IReportMetric = { atThen: {}, atNow: {} };

/** The report's shown metrics, each in its own panel, in catalogue order. */
const ReportPanels: React.FC<{ readonly report: IReport }> = ({ report }) => {
	const { getTerm } = useTerminology();
	const workItem = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return inCatalogOrder(report.shownMetricKeys).map((key) => {
		const panel = panelFor(key);
		if (panel === undefined)
			return (
				<UnsupportedMetricPanel
					key={key}
					name={getTerm(metricNameTermKeyOf(key))}
				/>
			);
		const metric = report.thenAndNow.metrics[key] ?? NOTHING_CAPTURED;
		const Panel = panel.component;
		return (
			<Panel
				key={key}
				title={panel.titleOf(getTerm)}
				metric={metric}
				workItem={workItem}
				workItems={workItems}
				directionOfGood={panel.directionOfGood}
			/>
		);
	});
};

export default ReportPanels;

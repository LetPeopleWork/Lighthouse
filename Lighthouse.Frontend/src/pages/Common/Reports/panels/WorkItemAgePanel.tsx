import type React from "react";
import DailySpreadPanel from "./DailySpreadPanel";
import type { ReportPanelProps } from "./reportPanelRegistry";
import { daysText } from "./thenNowComparison";

/** The summed age of everything in progress on a day, spread over each window. */
const WorkItemAgePanel: React.FC<Readonly<ReportPanelProps>> = (props) => (
	<DailySpreadPanel {...props} valueText={daysText} />
);

export default WorkItemAgePanel;

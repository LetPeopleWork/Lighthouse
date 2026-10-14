import type React from "react";
import DailySpreadPanel from "./DailySpreadPanel";
import type { ReportPanelProps } from "./reportPanelRegistry";

const plainNumber = (value: number) => `${value}`;

/** How many Work Items were in progress on a day, spread over each window. */
const WipPanel: React.FC<Readonly<ReportPanelProps>> = (props) => (
	<DailySpreadPanel {...props} valueText={plainNumber} />
);

export default WipPanel;

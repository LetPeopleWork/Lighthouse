import type { IPortfolioSettings } from "../Project/PortfolioSettings";
import type { ITeamSettings } from "../Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../WorkTracking/WorkTrackingSystemConnection";

export interface ConfigurationExport {
	workTrackingSystems: IWorkTrackingSystemConnection[];
	teams: ITeamSettings[];
	projects: IPortfolioSettings[];
}

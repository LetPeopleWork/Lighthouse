import type { IProjectSettings } from "../Project/ProjectSettings";
import type { ITeamSettings } from "../Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../WorkTracking/WorkTrackingSystemConnection";

export interface ConfigurationExport {
	workTrackingSystems: IWorkTrackingSystemConnection[];
	teams: ITeamSettings[];
	projects: IProjectSettings[];
}

import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../../../models/Team/TeamSettings";
import type { WorkTrackingSystemConnection } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";

export interface ImportResult<
	TEntity extends { id: number | null; name: string },
> {
	entity: TEntity;
	status: "Success" | "Validation Failed" | "Error";
	errorMessage?: string;
}

export interface ImportResults {
	workTrackingSystems: ImportResult<WorkTrackingSystemConnection>[];
	teams: ImportResult<ITeamSettings>[];
	projects: ImportResult<IProjectSettings>[];
}

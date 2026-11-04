import type React from "react";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { ParentSourceEntry } from "./parentSources";

export interface ParentSourcesListProps {
	value: readonly ParentSourceEntry[];
	tracker: WorkTrackingSystemType;
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[];
	onChange: (list: readonly ParentSourceEntry[]) => void;
}

// SCAFFOLD: Story 6501. The ordered list of parent sources, edited by up, down and remove buttons.
const ParentSourcesList: React.FC<Readonly<ParentSourcesListProps>> = () => {
	throw new Error("SCAFFOLD: Story 6501, ParentSourcesList is not built yet.");
};

export default ParentSourcesList;

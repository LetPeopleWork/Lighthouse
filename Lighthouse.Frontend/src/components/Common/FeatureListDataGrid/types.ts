import type { GridValidRowModel } from "@mui/x-data-grid";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import type { DataGridColumn } from "../DataGrid/types";

export interface FeatureListDataGridProps {
	features: IFeature[];
	columns: DataGridColumn<IFeature & GridValidRowModel>[];
	storageKey: string;
	hideCompletedStorageKey: string;
	loading?: boolean;
	emptyStateMessage?: string;
	/** AC-1.5 names the Features view and the Portfolio Feature list; a whole-instance ordinal inside any other, narrower subset would misread. */
	showPosition?: boolean;
	getActiveWorkTeams?: (feature: IFeature) => IEntityReference[];
}

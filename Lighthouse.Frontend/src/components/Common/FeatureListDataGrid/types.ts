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
	getActiveWorkTeams?: (feature: IFeature) => IEntityReference[];
}

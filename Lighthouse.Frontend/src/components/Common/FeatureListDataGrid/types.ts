import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import type {
	FeatureMoveGate,
	FeatureMoveTarget,
} from "../../../models/FeatureOrdering";
import type { DataGridColumn, DataGridExportTable } from "../DataGrid/types";

/**
 * Everything the row menu needs, resolved by the grid that owns the rows. The column factory stays
 * ignorant of policy, licence and RBAC — it renders a verdict, it does not reach one.
 */
export interface FeatureOrderingBinding {
	resolveGate: (feature: IFeature) => FeatureMoveGate;
	/** The rows either side of this one AS SHOWN — hidden and filtered rows are jumped over. */
	neighboursFor: (feature: IFeature) => {
		firstId?: number;
		previousId?: number;
		nextId?: number;
	};
	onMove: (featureId: number, target: FeatureMoveTarget) => Promise<void>;
}

export interface FeatureListDataGridProps {
	features: IFeature[];
	columns: DataGridColumn<IFeature & GridValidRowModel>[];
	storageKey: string;
	hideCompletedStorageKey: string;
	loading?: boolean;
	emptyStateMessage?: string;
	/** Only where the list is the whole instance: an instance-wide ordinal shown inside a narrower subset reads as that subset's own numbering. */
	showPosition?: boolean;
	/** Enable CSV export and clipboard copy on this grid (default: false) - Premium Feature */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
	/** The whole exported table, built by the caller. Given one, the toolbar exports it verbatim. */
	exportTable?: (orderedRowIds: GridRowId[]) => DataGridExportTable;
	/** Called after a move lands, so the surface can re-read the order it just changed. */
	onOrderChanged?: () => void | Promise<void>;
	getActiveWorkTeams?: (feature: IFeature) => IEntityReference[];
}

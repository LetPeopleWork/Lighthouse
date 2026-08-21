import type { GridValidRowModel } from "@mui/x-data-grid";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import type {
	FeatureMoveGate,
	FeatureMoveTarget,
} from "../../../models/FeatureOrdering";
import type {
	DataGridColumn,
	DataGridExportHeaderRow,
} from "../DataGrid/types";

/**
 * Everything the row menu needs, resolved by the grid that owns the rows. The column factory stays
 * ignorant of policy, licence and RBAC — it renders a verdict, it does not reach one.
 */
export interface FeatureOrderingBinding {
	resolveGate: (feature: IFeature) => FeatureMoveGate;
	/** The rows either side of this one AS SHOWN — hidden and filtered rows are jumped (AC-3.3). */
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
	/** AC-1.5 names the Features view and the Portfolio Feature list; a whole-instance ordinal inside any other, narrower subset would misread. */
	showPosition?: boolean;
	/** Enable CSV export and clipboard copy on this grid (default: false) - Premium Feature */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
	/** Key-value lines placed above the grid's own header row when exporting. */
	exportHeaderRows?: readonly DataGridExportHeaderRow[];
	/** Called after a move lands, so the surface can re-read the order it just changed. */
	onOrderChanged?: () => void | Promise<void>;
	getActiveWorkTeams?: (feature: IFeature) => IEntityReference[];
}

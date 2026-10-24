import type { IFeature } from "../../../models/Feature";

/**
 * Mode for the FeatureGrid component
 * - "selectable": checkboxes can be toggled by user
 * - "readonly": checkboxes are always checked and disabled (dimmed)
 */
export type FeatureGridMode = "selectable" | "readonly";

/**
 * Props for the FeatureGrid component
 */
export interface FeatureGridProps {
	/** Array of features to display */
	features: IFeature[];

	/** Array of currently selected feature IDs (used for checkbox state) */
	selectedFeatureIds: number[];

	/**
	 * Callback fired when selection changes (only in "selectable" mode).
	 * In "readonly" mode, this is not required.
	 */
	onChange?: (selectedIds: number[]) => void;

	/** Storage key for persisting DataGrid state */
	storageKey: string;

	/**
	 * Mode for the grid.
	 * - "selectable": user can toggle checkboxes (default)
	 * - "readonly": checkboxes are always checked and disabled
	 */
	mode?: FeatureGridMode;

	/** Shows the grid's own loading overlay while the features are on their way */
	loading?: boolean;
}

/**
 * Row structure for the FeatureGrid DataGrid
 */
export interface FeatureGridRow {
	id: number;
	reference: string;
	name: string;
	selected: boolean;
	feature: IFeature;
}

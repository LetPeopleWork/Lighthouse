import type { IFeature } from "../../../models/Feature";

export interface FeatureSelectorProps {
	/** Array of features to select from */
	features: IFeature[];

	/** Array of currently selected feature IDs */
	selectedFeatureIds: number[];

	/** Callback fired when selection changes */
	onChange: (selectedIds: number[]) => void;

	/** Storage key for persisting DataGrid state */
	storageKey: string;
}

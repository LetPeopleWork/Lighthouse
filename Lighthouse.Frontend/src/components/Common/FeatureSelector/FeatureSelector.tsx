import type React from "react";
import { FeatureGrid } from "../FeatureGrid";
import type { FeatureSelectorProps } from "./types";

/**
 * FeatureSelector - A component for selecting features from a list.
 *
 * This is a thin wrapper around FeatureGrid that provides the "selectable" mode
 * for manual feature selection in deliveries.
 */
export const FeatureSelector: React.FC<FeatureSelectorProps> = ({
	features,
	selectedFeatureIds,
	onChange,
	storageKey,
}) => {
	return (
		<FeatureGrid
			features={features}
			selectedFeatureIds={selectedFeatureIds}
			onChange={onChange}
			storageKey={storageKey}
			mode="selectable"
		/>
	);
};

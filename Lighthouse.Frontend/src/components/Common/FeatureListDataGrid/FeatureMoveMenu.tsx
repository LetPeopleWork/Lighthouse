import type React from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	FeatureMoveGate,
	FeatureMoveTarget,
} from "../../../models/FeatureOrdering";

/**
 * The row action menu behind D18's four gestures. It renders enabled or disabled from a verdict it is
 * **given**, never one it derives: the natural client-side expression
 * `projects.every(p => isPortfolioAdmin(p.id))` fails open twice — `projects` is already read-filtered,
 * and `every` is vacuously true on the empty array an orphan Feature produces (ADR-136 SA-10).
 */
export interface FeatureMoveMenuProps {
	feature: IFeature;
	/** Resolved once, by `useFeatureOrdering` (SA-12). This component asks no further questions. */
	gate: FeatureMoveGate;
	onMove: (target: FeatureMoveTarget) => Promise<void>;
	/**
	 * The rows either side of this one as the user sees them. Hidden Done Features (D15) and rows the
	 * grid filtered out are jumped, not landed on (AC-3.3), so the neighbours are the *visible* ones.
	 */
	visibleNeighbours: {
		firstId?: number;
		previousId?: number;
		nextId?: number;
	};
}

// __SCAFFOLD__ (Epic 5375 slice 03)
const FeatureMoveMenu: React.FC<FeatureMoveMenuProps> = () => {
	throw new Error("Not yet implemented — RED scaffold");
};

export default FeatureMoveMenu;

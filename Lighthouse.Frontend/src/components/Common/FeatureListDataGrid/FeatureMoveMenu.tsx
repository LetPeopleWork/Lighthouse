import MoreVertIcon from "@mui/icons-material/MoreVert";
import { IconButton, Menu, MenuItem, Typography } from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	FeatureMoveGate,
	FeatureMoveTarget,
} from "../../../models/FeatureOrdering";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

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

// Announced, not shown: the outcome is already visible to anyone who can see the list re-sort.
const visuallyHidden: React.CSSProperties = {
	position: "absolute",
	width: 1,
	height: 1,
	overflow: "hidden",
	clip: "rect(0 0 0 0)",
	whiteSpace: "nowrap",
};

interface Gesture {
	label: string;
	target?: FeatureMoveTarget;
}

const FeatureMoveMenu: React.FC<FeatureMoveMenuProps> = ({
	feature,
	gate,
	onMove,
	visibleNeighbours,
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);

	const [anchor, setAnchor] = useState<HTMLElement | null>(null);
	const [announcement, setAnnouncement] = useState("");

	// AC-3.10: an instance that does not order its own Features has no move actions at all, rather than
	// four greyed-out ones explaining a capability it does not have.
	if (
		!gate.enabled &&
		(gate.reason === "not-premium" || gate.reason === "policy-off")
	) {
		return null;
	}

	const refusal = gate.enabled ? null : refusalText(gate, feature, featureTerm);

	const gestures: Gesture[] = [
		{
			label: "Move to Top",
			target:
				visibleNeighbours.firstId === undefined
					? undefined
					: { beforeFeatureId: visibleNeighbours.firstId },
		},
		{
			label: "Move Up",
			target:
				visibleNeighbours.previousId === undefined
					? undefined
					: { beforeFeatureId: visibleNeighbours.previousId },
		},
		{
			label: "Move Down",
			target:
				visibleNeighbours.nextId === undefined
					? undefined
					: { afterFeatureId: visibleNeighbours.nextId },
		},
		{ label: "Move to Bottom", target: { beforeFeatureId: null } },
	];

	const choose = async (gesture: Gesture) => {
		if (refusal !== null || gesture.target === undefined) {
			return;
		}

		setAnchor(null);
		await onMove(gesture.target);
		setAnnouncement(`${feature.name} — ${gesture.label.toLowerCase()}`);
	};

	return (
		<>
			<IconButton
				size="small"
				aria-label={`Move ${feature.name}`}
				onClick={(event) => setAnchor(event.currentTarget)}
			>
				<MoreVertIcon fontSize="small" />
			</IconButton>

			<Menu
				anchorEl={anchor}
				open={anchor !== null}
				onClose={() => setAnchor(null)}
			>
				{refusal !== null && (
					<Typography
						variant="caption"
						sx={{ display: "block", px: 2, py: 1, maxWidth: 320 }}
						color="text.secondary"
					>
						{refusal}
					</Typography>
				)}

				{gestures.map((gesture) => (
					<MenuItem
						key={gesture.label}
						// Deliberately aria-disabled rather than `disabled`: a disabled element fires no
						// events, so it carries no tooltip and a screen reader skips the very sentence that
						// explains the refusal (AC-3.8, AC-3.11).
						aria-disabled={refusal !== null || gesture.target === undefined}
						title={refusal ?? undefined}
						sx={refusal !== null ? { opacity: 0.5 } : undefined}
						onClick={() => choose(gesture)}
					>
						{gesture.label}
					</MenuItem>
				))}
			</Menu>

			{/* The grid re-sorts itself silently, which tells a screen-reader user nothing at all. */}
			<output aria-live="polite" style={visuallyHidden}>
				{announcement}
			</output>
		</>
	);
};

const refusalText = (
	gate: Exclude<FeatureMoveGate, { enabled: true }>,
	feature: IFeature,
	featureTerm: string,
): string => {
	if (gate.reason === "sorted") {
		return `Sorting by a column decides this list's order, so up and down have no meaning here. Sort by position to move ${feature.name}.`;
	}

	if (gate.reason === "orphan") {
		return `${feature.name} belongs to no Portfolio, so nobody can decide where it sits.`;
	}

	if (gate.blockingPortfolios.length > 0) {
		return `${feature.name} is also worked on in ${gate.blockingPortfolios.join(", ")}, which you do not run. Moving it would re-sequence their delivery too.`;
	}

	return `This ${featureTerm} is also worked on in a Portfolio you do not run, so moving it would re-sequence somebody else's delivery.`;
};

export default FeatureMoveMenu;

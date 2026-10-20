import type { SvgIconComponent } from "@mui/icons-material";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import RemoveIcon from "@mui/icons-material/Remove";
import { createSvgIcon } from "@mui/material/utils";
import type React from "react";
import type { LineMarkKind } from "./thenNowComparison";

/** Which way a metric or a line moved, as a mark draws it: the summary's kinds and a line's rise, fall or no change. */
export type ChangeMarkKind = "improved" | "worse" | "mixed" | LineMarkKind;

const PlusMinusIcon = createSvgIcon(
	<path d="M11 3h2v5h5v2h-5v5h-2v-5H6V8h5zM6 18h12v2H6z" />,
	"PlusMinus",
);

const ICON_OF: Record<ChangeMarkKind, SvgIconComponent> = {
	improved: CheckIcon,
	worse: CloseIcon,
	mixed: PlusMinusIcon,
	up: ArrowUpwardIcon,
	down: ArrowDownwardIcon,
	none: RemoveIcon,
};

// A line's words read the same for a rise and a fall once its percent rounds to nothing, so only the mark can tell a
// screen reader which way it went. The other marks only repeat the words beside them.
const SPOKEN_AS: Partial<Record<ChangeMarkKind, string>> = {
	up: "rose",
	down: "fell",
};

/**
 * Drawn rather than typed, so the mark looks the same on every machine and in every PDF whatever fonts it has. A
 * screen reader hears a rise or a fall and skips every other mark, whose words beside it already say it.
 */
export const ChangeMarkIcon: React.FC<{
	readonly kind: ChangeMarkKind;
	readonly colour: string;
}> = ({ kind, colour }) => {
	const Icon = ICON_OF[kind];
	const spoken = SPOKEN_AS[kind];
	const sx = { color: colour, verticalAlign: "-0.125em" };
	if (spoken === undefined) {
		return <Icon aria-hidden fontSize="inherit" sx={sx} />;
	}
	// The label rather than a title inside the drawing, which would add the word to the line's text.
	return (
		<Icon
			role="img"
			aria-label={spoken}
			aria-hidden={undefined}
			fontSize="inherit"
			sx={sx}
		/>
	);
};

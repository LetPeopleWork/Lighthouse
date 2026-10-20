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

/**
 * Drawn rather than typed, so the mark looks the same on every machine and in every PDF whatever fonts it has. The
 * words beside it carry the meaning, so a screen reader skips it.
 */
export const ChangeMarkIcon: React.FC<{
	readonly kind: ChangeMarkKind;
	readonly colour: string;
}> = ({ kind, colour }) => {
	const Icon = ICON_OF[kind];
	return (
		<Icon
			aria-hidden
			fontSize="inherit"
			sx={{ color: colour, verticalAlign: "-0.125em" }}
		/>
	);
};

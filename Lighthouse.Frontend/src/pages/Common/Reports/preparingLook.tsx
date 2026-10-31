import { CircularProgress } from "@mui/material";
import type React from "react";

// Disabled only in look while preparing: a truly disabled button would throw a keyboard reader's focus to the page.
export const PREPARING_LOOK = {
	color: "action.disabled",
	borderColor: "action.disabledBackground",
	cursor: "default",
};

/**
 * The props of an Export PDF button that, while its PDF is prepared, shows the waiting spinner and ignores a second
 * choice, but keeps the focus.
 */
export const exportPdfButtonProps = (
	preparing: boolean,
	onExport: () => void,
	startIcon?: React.ReactNode,
) => ({
	onClick: preparing ? undefined : onExport,
	"aria-disabled": preparing ? ("true" as const) : undefined,
	"aria-busy": preparing ? ("true" as const) : undefined,
	startIcon: preparing ? <CircularProgress size={20} /> : startIcon,
	sx: preparing ? PREPARING_LOOK : undefined,
});

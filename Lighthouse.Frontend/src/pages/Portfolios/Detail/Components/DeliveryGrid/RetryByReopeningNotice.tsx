import {
	Alert,
	type AlertColor,
	type SxProps,
	type Theme,
} from "@mui/material";
import type React from "react";

/**
 * Every answer the Delivery dialog waits on is asked for afresh on each opening, so reopening it is the
 * one way to try a failed answer again, and each such notice says so the same way.
 */
export const RetryByReopeningNotice: React.FC<
	Readonly<{ problem: string; severity?: AlertColor; sx?: SxProps<Theme> }>
> = ({ problem, severity = "warning", sx }) => (
	<Alert severity={severity} sx={sx}>
		{problem} Close this dialog and open it again to retry.
	</Alert>
);

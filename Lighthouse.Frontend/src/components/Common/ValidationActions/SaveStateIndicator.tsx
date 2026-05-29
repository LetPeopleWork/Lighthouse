import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import { Button, CircularProgress, Stack, Typography } from "@mui/material";
import type React from "react";

export type SaveState = "idle" | "saving" | "saved" | "error";

export interface SaveStateIndicatorProps {
	saveState: SaveState;
	canSave: boolean;
	onRetry?: () => void;
}

const SAVING_COPY = "Saving…";
const SAVED_COPY = "All changes saved";
const ERROR_COPY = "Couldn't save";

const SaveStateIndicator: React.FC<SaveStateIndicatorProps> = ({
	saveState,
	canSave,
	onRetry,
}) => {
	if (!canSave || saveState === "idle") {
		return null;
	}

	if (saveState === "saving") {
		return (
			<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
				<CircularProgress size={16} />
				<Typography variant="body2" color="text.secondary">
					{SAVING_COPY}
				</Typography>
			</Stack>
		);
	}

	if (saveState === "saved") {
		return (
			<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
				<CheckCircleIcon color="success" fontSize="small" />
				<Typography variant="body2" color="text.secondary">
					{SAVED_COPY}
				</Typography>
			</Stack>
		);
	}

	return (
		<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
			<ErrorOutlineIcon color="error" fontSize="small" />
			<Typography variant="body2" color="error">
				{ERROR_COPY}
			</Typography>
			<Button size="small" variant="text" onClick={onRetry}>
				Retry
			</Button>
		</Stack>
	);
};

export default SaveStateIndicator;

import type React from "react";

export type SaveState = "idle" | "saving" | "saved" | "error";

export interface SaveStateIndicatorProps {
	saveState: SaveState;
	onRetry?: () => void;
}

const NOT_IMPLEMENTED =
	"SaveStateIndicator not yet implemented — RED scaffold (DISTILL)";

const SaveStateIndicator: React.FC<SaveStateIndicatorProps> = () => {
	throw new Error(NOT_IMPLEMENTED);
};

export default SaveStateIndicator;

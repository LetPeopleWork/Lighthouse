import CloudOffIcon from "@mui/icons-material/CloudOff";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import IconButton from "@mui/material/IconButton";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";

/**
 * What the instance is doing right now, as far as this browser can tell.
 *
 * `unknown` exists because the state endpoint can fail or be unreachable, and the indicator has to
 * render something. It renders as not-sending: the opposite of `useRbac`, which fails open on
 * purpose. A privacy indicator that guesses "sending" when it does not know would be alarming and
 * wrong; one that guesses "not sending" when it does not know is only wrong.
 */
export type UsageDataSendingState = "sending" | "not-sending" | "unknown";

export interface UsageDataIndicatorProps {
	state: UsageDataSendingState;
	onOpenDecision: () => void;
}

// The two states are told apart by these words, not by the icon and not by colour. Someone using a
// screen reader, or looking at the page in greyscale, gets the same answer as everyone else - and
// "is this instance sending data about how I work" is not a question to answer in hue alone.
const SENDING_LABEL = "Usage data: being sent from this browser";
const NOT_SENDING_LABEL = "Usage data: not being sent";

export const UsageDataIndicator = ({
	state,
	onOpenDecision,
}: UsageDataIndicatorProps): React.ReactElement => {
	const theme = useTheme();

	const isSending = state === "sending";
	const label = isSending ? SENDING_LABEL : NOT_SENDING_LABEL;
	const Icon = isSending ? CloudUploadIcon : CloudOffIcon;

	return (
		<Tooltip title={label} arrow>
			<IconButton
				size="small"
				color="inherit"
				onClick={onOpenDecision}
				aria-label={label}
				data-testid="usage-data-indicator"
			>
				<Icon fontSize="small" style={{ color: theme.palette.primary.main }} />
			</IconButton>
		</Tooltip>
	);
};

export default UsageDataIndicator;

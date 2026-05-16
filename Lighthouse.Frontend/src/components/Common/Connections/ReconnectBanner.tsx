import { Alert, Button } from "@mui/material";
import { useContext, useState } from "react";
import { useOAuthPopup } from "../../../hooks/useOAuthPopup";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface ReconnectBannerProps {
	connection: IWorkTrackingSystemConnection;
	onReconnected?: () => void;
}

interface InlineMessage {
	severity: "error" | "info";
	text: string;
}

const RECONNECT_COPY =
	"Reconnect required — the OAuth refresh token is no longer valid";

const POPUP_BLOCKED_COPY =
	"Your browser blocked the OAuth popup. Allow popups for this site and click Connect/Reconnect again.";

const CANCELLED_COPY = "OAuth was cancelled. Click Reconnect to try again.";

const START_FAILED_COPY = "Failed to start reconnect. Please try again.";

const ReconnectBanner = ({
	connection,
	onReconnected,
}: ReconnectBannerProps) => {
	const { oauthService } = useContext(ApiServiceContext);
	const { openOAuthPopup } = useOAuthPopup();
	const [isReconnecting, setIsReconnecting] = useState(false);
	const [inlineMessage, setInlineMessage] = useState<InlineMessage | null>(
		null,
	);

	if (connection.requiresReconnect !== true) {
		return null;
	}

	if (connection.id === null) {
		return null;
	}

	const providerKey = connection.authenticationMethodKey;
	const connectionId = connection.id;

	const handleReconnect = async () => {
		setIsReconnecting(true);
		setInlineMessage(null);
		try {
			await oauthService.disconnect(providerKey, connectionId);
			const { authorizationUrl } = await oauthService.initiateConnect(
				providerKey,
				connectionId,
			);
			const result = await openOAuthPopup(authorizationUrl);
			switch (result.status) {
				case "success":
					setInlineMessage(null);
					onReconnected?.();
					break;
				case "popup_blocked":
					setInlineMessage({ severity: "error", text: POPUP_BLOCKED_COPY });
					break;
				case "cancelled":
					setInlineMessage({ severity: "info", text: CANCELLED_COPY });
					break;
				case "error":
					setInlineMessage({
						severity: "error",
						text: result.reason ?? "OAuth failed.",
					});
					break;
			}
		} catch {
			setInlineMessage({ severity: "error", text: START_FAILED_COPY });
		} finally {
			setIsReconnecting(false);
		}
	};

	return (
		<>
			<Alert
				severity="warning"
				action={
					<Button
						color="inherit"
						size="small"
						onClick={handleReconnect}
						disabled={isReconnecting}
					>
						Reconnect
					</Button>
				}
			>
				{RECONNECT_COPY}
			</Alert>
			{inlineMessage !== null && (
				<Alert
					severity={inlineMessage.severity}
					data-testid="reconnect-banner-inline-message"
				>
					{inlineMessage.text}
				</Alert>
			)}
		</>
	);
};

export default ReconnectBanner;

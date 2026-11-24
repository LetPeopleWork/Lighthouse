import { Alert, Button } from "@mui/material";
import { useContext } from "react";
import {
	type OAuthSignInCopy,
	useOAuthSignIn,
} from "../../../hooks/useOAuthSignIn";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface ReconnectBannerProps {
	connection: IWorkTrackingSystemConnection;
	onReconnected?: () => void;
}

const RECONNECT_COPY =
	"Reconnect required — the OAuth refresh token is no longer valid";

const RECONNECT_SIGN_IN_COPY: OAuthSignInCopy = {
	popupBlocked:
		"Your browser blocked the OAuth popup. Allow popups for this site and click Connect/Reconnect again.",
	cancelled: "OAuth was cancelled. Click Reconnect to try again.",
	startFailed: "Failed to start reconnect. Please try again.",
};

const ReconnectBanner = ({
	connection,
	onReconnected,
}: ReconnectBannerProps) => {
	const { oauthService } = useContext(ApiServiceContext);
	const { signIn, isSigningIn, inlineMessage } = useOAuthSignIn(
		RECONNECT_SIGN_IN_COPY,
		onReconnected,
	);

	if (connection.requiresReconnect !== true) {
		return null;
	}

	if (connection.id === null) {
		return null;
	}

	const target = {
		providerKey: connection.authenticationMethodKey,
		connectionId: connection.id,
	};

	const handleReconnect = () =>
		signIn(target, () =>
			oauthService.disconnect(target.providerKey, target.connectionId),
		);

	return (
		<>
			<Alert
				severity="warning"
				action={
					<Button
						color="inherit"
						size="small"
						onClick={handleReconnect}
						disabled={isSigningIn}
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

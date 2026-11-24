import { useContext } from "react";
import {
	type OAuthSignInCopy,
	useOAuthSignIn,
} from "../../../hooks/useOAuthSignIn";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import SignInAlert from "./SignInAlert";

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
		<SignInAlert
			message={RECONNECT_COPY}
			actionLabel="Reconnect"
			onAction={handleReconnect}
			isSigningIn={isSigningIn}
			inlineMessage={inlineMessage}
			inlineMessageTestId="reconnect-banner-inline-message"
		/>
	);
};

export default ReconnectBanner;

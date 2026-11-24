import { Alert, Button } from "@mui/material";
import {
	type OAuthSignInCopy,
	useOAuthSignIn,
} from "../../../hooks/useOAuthSignIn";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface WriteConsentAlertProps {
	connection: IWorkTrackingSystemConnection;
	onSignedIn?: () => void;
}

const WRITE_CONSENT_COPY =
	"Write-back needs more rights. Lighthouse can only read from Azure DevOps with this sign-in. Sign in again to allow Lighthouse to write the mapped fields.";

const SIGN_IN_AGAIN_COPY: OAuthSignInCopy = {
	popupBlocked:
		"Your browser blocked the OAuth popup. Allow popups for this site and click Sign in again.",
	cancelled: "OAuth was cancelled. Click Sign in again to try again.",
	startFailed: "Failed to start the sign-in. Please try again.",
};

/**
 * Asks for write consent when write-back is set up on a sign-in that may only read. Unlike Reconnect it
 * keeps the current sign-in, so reads go on working if the admin cancels.
 */
const WriteConsentAlert = ({
	connection,
	onSignedIn,
}: WriteConsentAlertProps) => {
	const { signIn, isSigningIn, inlineMessage } = useOAuthSignIn(
		SIGN_IN_AGAIN_COPY,
		onSignedIn,
	);

	if (connection.needsWriteConsent !== true || connection.id === null) {
		return null;
	}

	const target = {
		providerKey: connection.authenticationMethodKey,
		connectionId: connection.id,
	};

	return (
		<>
			<Alert
				severity="warning"
				action={
					<Button
						color="inherit"
						size="small"
						onClick={() => signIn(target)}
						disabled={isSigningIn}
					>
						Sign in again
					</Button>
				}
			>
				{WRITE_CONSENT_COPY}
			</Alert>
			{inlineMessage !== null && (
				<Alert
					severity={inlineMessage.severity}
					data-testid="write-consent-alert-inline-message"
				>
					{inlineMessage.text}
				</Alert>
			)}
		</>
	);
};

export default WriteConsentAlert;

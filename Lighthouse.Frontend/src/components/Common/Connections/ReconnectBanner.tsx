import { Alert, Button } from "@mui/material";
import { useContext, useState } from "react";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface ReconnectBannerProps {
	connection: IWorkTrackingSystemConnection;
}

const RECONNECT_COPY =
	"Reconnect required — the OAuth refresh token is no longer valid";

const ReconnectBanner = ({ connection }: ReconnectBannerProps) => {
	const { oauthService } = useContext(ApiServiceContext);
	const [isReconnecting, setIsReconnecting] = useState(false);
	const [error, setError] = useState<string | null>(null);

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
		setError(null);
		try {
			await oauthService.disconnect(providerKey, connectionId);
			const result = await oauthService.initiateConnect(
				providerKey,
				connectionId,
			);
			globalThis.location.assign(result.authorizationUrl);
		} catch {
			setError("Failed to start reconnect. Please try again.");
			setIsReconnecting(false);
		}
	};

	return (
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
			{error && <span data-testid="reconnect-banner-error"> — {error}</span>}
		</Alert>
	);
};

export default ReconnectBanner;

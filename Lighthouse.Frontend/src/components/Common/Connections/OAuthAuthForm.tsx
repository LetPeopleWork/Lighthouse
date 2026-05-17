import { Alert, Box, Button, Stack, TextField } from "@mui/material";
import { useContext, useState } from "react";
import { useOAuthPopup } from "../../../hooks/useOAuthPopup";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface OAuthAuthFormProps {
	connectionId: number;
	providerKey: string;
	baseUrl: string | null;
	onConnect?: () => void;
}

interface InlineMessage {
	severity: "error" | "info";
	text: string;
}

const POPUP_BLOCKED_COPY =
	"Your browser blocked the OAuth popup. Allow popups for this site and click Connect/Reconnect again.";

const CANCELLED_COPY = "OAuth was cancelled. Click Connect to try again.";

const buildCallbackUrl = (baseUrl: string | null): string => {
	const root =
		baseUrl && baseUrl.length > 0 ? baseUrl : globalThis.location.origin;
	return `${root}/api/oauth/callback`;
};

const isAdoOauthOverHttp = (
	providerKey: string,
	baseUrl: string | null,
): boolean =>
	providerKey === "ado.oauth" && (baseUrl?.startsWith("http://") ?? false);

const OAuthAuthForm = ({
	connectionId,
	providerKey,
	baseUrl,
	onConnect,
}: OAuthAuthFormProps) => {
	const { oauthService } = useContext(ApiServiceContext);
	const { openOAuthPopup } = useOAuthPopup();
	const [isConnecting, setIsConnecting] = useState(false);
	const [inlineMessage, setInlineMessage] = useState<InlineMessage | null>(
		null,
	);

	const callbackUrl = buildCallbackUrl(baseUrl);
	const hasBaseUrl = Boolean(baseUrl && baseUrl.length > 0);
	const showAdoHttpsWarning = isAdoOauthOverHttp(providerKey, baseUrl);

	const handleConnect = async () => {
		setIsConnecting(true);
		setInlineMessage(null);
		try {
			const { authorizationUrl } = await oauthService.initiateConnect(
				providerKey,
				connectionId,
			);
			const result = await openOAuthPopup(authorizationUrl);
			switch (result.status) {
				case "success":
					setInlineMessage(null);
					onConnect?.();
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
		} finally {
			setIsConnecting(false);
		}
	};

	return (
		<Stack spacing={2}>
			{!hasBaseUrl && (
				<Alert severity="warning">
					Your callback URL may be incorrect. Set Lighthouse:BaseUrl in your
					server configuration to guarantee OAuth registration works.
				</Alert>
			)}

			{showAdoHttpsWarning && (
				<Alert severity="warning">
					Azure DevOps requires HTTPS callback URLs in production. Configure
					Lighthouse:BaseUrl with https:// before registering the OAuth app.
				</Alert>
			)}

			<TextField
				label="Callback URL"
				value={callbackUrl}
				slotProps={{ input: { readOnly: true } }}
				fullWidth
			/>

			{inlineMessage !== null && (
				<Alert
					severity={inlineMessage.severity}
					data-testid="oauth-auth-form-inline-message"
				>
					{inlineMessage.text}
				</Alert>
			)}

			<Box>
				<Button
					variant="contained"
					onClick={handleConnect}
					disabled={isConnecting}
				>
					Connect
				</Button>
			</Box>
		</Stack>
	);
};

export default OAuthAuthForm;

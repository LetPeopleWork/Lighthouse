import { Alert, Box, Button, Stack, TextField } from "@mui/material";
import { useContext, useState } from "react";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface OAuthAuthFormProps {
	connectionId: number;
	providerKey: string;
	baseUrl: string | null;
	onConnect?: () => void;
}

const buildCallbackUrl = (baseUrl: string | null): string => {
	const root =
		baseUrl && baseUrl.length > 0 ? baseUrl : globalThis.location.origin;
	return `${root}/api/oauth/callback`;
};

const OAuthAuthForm = ({
	connectionId,
	providerKey,
	baseUrl,
	onConnect,
}: OAuthAuthFormProps) => {
	const { oauthService } = useContext(ApiServiceContext);
	const [isConnecting, setIsConnecting] = useState(false);

	const callbackUrl = buildCallbackUrl(baseUrl);
	const hasBaseUrl = Boolean(baseUrl && baseUrl.length > 0);

	const handleConnect = async () => {
		setIsConnecting(true);
		try {
			const result = await oauthService.initiateConnect(
				providerKey,
				connectionId,
			);
			onConnect?.();
			globalThis.location.assign(result.authorizationUrl);
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

			<TextField
				label="Callback URL"
				value={callbackUrl}
				slotProps={{ input: { readOnly: true } }}
				fullWidth
			/>

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

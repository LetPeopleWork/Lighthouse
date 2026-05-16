import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";

type OAuthCompleteMessage = {
	type: "oauth.complete";
	status: string;
	connectionId?: number;
	reason?: string;
};

const buildMessage = (
	status: string,
	connectionId: string | null,
	reason: string | null,
): OAuthCompleteMessage => {
	const message: OAuthCompleteMessage = {
		type: "oauth.complete",
		status,
	};
	if (connectionId !== null && connectionId.length > 0) {
		const parsed = Number(connectionId);
		if (Number.isFinite(parsed)) {
			message.connectionId = parsed;
		}
	}
	if (reason !== null && reason.length > 0) {
		message.reason = reason;
	}
	return message;
};

const OAuthPopupComplete = () => {
	const [searchParams] = useSearchParams();
	const status = searchParams.get("status") ?? "";
	const connectionId = searchParams.get("connectionId");
	const reason = searchParams.get("reason");
	const opener = window.opener;
	const hasOpener = opener !== null && opener !== undefined;

	useEffect(() => {
		if (!hasOpener) {
			return;
		}
		const message = buildMessage(status, connectionId, reason);
		opener.postMessage(message, window.location.origin);
		window.close();
	}, [hasOpener, status, connectionId, reason]);

	if (hasOpener) {
		return null;
	}

	return (
		<p>
			OAuth completed successfully. You may close this window — if it doesn't
			close automatically, close it manually.
		</p>
	);
};

export default OAuthPopupComplete;

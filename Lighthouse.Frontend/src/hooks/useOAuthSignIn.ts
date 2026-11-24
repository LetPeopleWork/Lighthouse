import { useContext, useState } from "react";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useOAuthPopup } from "./useOAuthPopup";

export interface OAuthSignInMessage {
	severity: "error" | "info";
	text: string;
}

/** What the admin is told when a sign-in does not complete, worded for the button that started it. */
export interface OAuthSignInCopy {
	popupBlocked: string;
	cancelled: string;
	startFailed: string;
}

export interface OAuthSignInTarget {
	providerKey: string;
	connectionId: number;
}

export interface UseOAuthSignIn {
	/** Runs `beforeSignIn` (if any), then starts the sign-in for the target and opens its popup. */
	signIn(
		target: OAuthSignInTarget,
		beforeSignIn?: () => Promise<void>,
	): Promise<void>;
	isSigningIn: boolean;
	inlineMessage: OAuthSignInMessage | null;
}

export const useOAuthSignIn = (
	copy: OAuthSignInCopy,
	onSignedIn?: () => void,
): UseOAuthSignIn => {
	const { oauthService } = useContext(ApiServiceContext);
	const { openOAuthPopup } = useOAuthPopup();
	const [isSigningIn, setIsSigningIn] = useState(false);
	const [inlineMessage, setInlineMessage] = useState<OAuthSignInMessage | null>(
		null,
	);

	const signIn = async (
		{ providerKey, connectionId }: OAuthSignInTarget,
		beforeSignIn?: () => Promise<void>,
	) => {
		setIsSigningIn(true);
		setInlineMessage(null);
		try {
			await beforeSignIn?.();
			const { authorizationUrl } = await oauthService.initiateConnect(
				providerKey,
				connectionId,
			);
			const result = await openOAuthPopup(authorizationUrl);
			switch (result.status) {
				case "success":
					setInlineMessage(null);
					onSignedIn?.();
					break;
				case "popup_blocked":
					setInlineMessage({ severity: "error", text: copy.popupBlocked });
					break;
				case "cancelled":
					setInlineMessage({ severity: "info", text: copy.cancelled });
					break;
				case "error":
					setInlineMessage({
						severity: "error",
						text: result.reason ?? "OAuth failed.",
					});
					break;
			}
		} catch {
			setInlineMessage({ severity: "error", text: copy.startFailed });
		} finally {
			setIsSigningIn(false);
		}
	};

	return { signIn, isSigningIn, inlineMessage };
};

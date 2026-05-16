// SCAFFOLD: true — remove after Story #5018 DELIVER

export type OAuthPopupStatus =
	| "success"
	| "error"
	| "cancelled"
	| "popup_blocked";

export interface OAuthPopupResult {
	status: OAuthPopupStatus;
	connectionId?: number;
	reason?: string;
}

export interface UseOAuthPopup {
	openOAuthPopup(authorizationUrl: string): Promise<OAuthPopupResult>;
}

export const useOAuthPopup = (): UseOAuthPopup => {
	throw new Error("Not yet implemented — RED scaffold (Story #5018 DISTILL)");
};

export default useOAuthPopup;

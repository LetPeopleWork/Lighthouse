import type { IAuthenticationMethod } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IWorkTrackingSystemOption,
	SecretStates,
} from "../../../models/WorkTracking/WorkTrackingSystemOption";

const OAUTH_KEY_SUFFIX = ".oauth";

export const CONNECTION_COPY = {
	concurrencyConflict:
		"This connection was changed by someone else since you opened it. Reload to get the latest values, then re-apply your change.",
	oauthLocked: "Locked after the OAuth connect handshake. Reconnect to rotate.",
	unreadableSecret:
		"This secret cannot be read with the current encryption key. Enter it again to store it under the key this instance uses now.",
} as const;

/** Said only when the refusal arrived without a word of its own to say. */
export const unexplainedRefusalCopy = (
	workTrackingSystemTerm: string,
): string =>
	`Could not connect to the ${workTrackingSystemTerm} with the provided settings. Please review and try again.`;

export const isOAuthMethod = (method: IAuthenticationMethod | null): boolean =>
	Boolean(method?.key.endsWith(OAUTH_KEY_SUFFIX));

export const secretCannotBeRead = (
	option: IWorkTrackingSystemOption,
): boolean => option.secretState === SecretStates.Unreadable;

export const resolveAuthOptionHelperText = (
	option: IWorkTrackingSystemOption,
	lockOAuthField: boolean,
): string | undefined => {
	if (secretCannotBeRead(option)) {
		return CONNECTION_COPY.unreadableSecret;
	}

	if (lockOAuthField) {
		return CONNECTION_COPY.oauthLocked;
	}

	return undefined;
};

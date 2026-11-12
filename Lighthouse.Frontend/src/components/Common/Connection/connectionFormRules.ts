import type {
	IAuthenticationMethod,
	IConnectionDefaults,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { isParentSourcesDefaultName } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IWorkTrackingSystemOption,
	SecretStates,
} from "../../../models/WorkTracking/WorkTrackingSystemOption";
import { ApiError } from "../../../services/Api/ApiError";

const OAUTH_KEY_SUFFIX = ".oauth";

export const CONFLICT_STATUS = 409;
// The server's own sentence names the field and who still uses it, so it is shown as it stands.
export const FIELD_STILL_IN_USE_CODE = "additional-field-in-use";

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

export function resolveValidationErrorMessage(
	error: unknown,
	workTrackingSystemTerm: string,
): { message: string; details: string | null } {
	if (error instanceof ApiError && error.code === 403) {
		return {
			message:
				error.message ||
				"You've exceeded the number of additional fields allowed on your plan.",
			details: error.technicalDetails ?? null,
		};
	}

	if (error instanceof ApiError) {
		return { message: error.message, details: error.technicalDetails ?? null };
	}

	return {
		message: unexplainedRefusalCopy(workTrackingSystemTerm),
		details: null,
	};
}

// The parent defaults travel as lists. The old number a read carries would be read as an edit of the
// default if it were sent back beside a list that has changed.
export const defaultsWithoutTheOldParentNumbers = (
	defaults: IConnectionDefaults,
): IConnectionDefaults =>
	Object.fromEntries(
		Object.entries(defaults).filter(
			([name]) => !isParentSourcesDefaultName(name),
		),
	);

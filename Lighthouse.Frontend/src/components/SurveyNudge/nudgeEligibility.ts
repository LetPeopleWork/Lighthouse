export interface NudgeEligibilityInput {
	isPremium: boolean | undefined | null;
	installTimestamp: string | undefined | null;
	lastShownAt?: string | undefined | null;
	now?: Date;
}

export interface NudgeDecision {
	shouldShow: boolean;
}

const TWO_WEEKS_IN_MILLISECONDS = 14 * 24 * 60 * 60 * 1000;

const parseInstant = (value: string | undefined | null): number | null => {
	if (!value) {
		return null;
	}

	const parsed = Date.parse(value);
	return Number.isNaN(parsed) ? null : parsed;
};

export const evaluateNudgeEligibility = (
	input: NudgeEligibilityInput,
): NudgeDecision => {
	if (input.isPremium !== false) {
		return { shouldShow: false };
	}

	const installedAt = parseInstant(input.installTimestamp);
	if (installedAt === null) {
		return { shouldShow: false };
	}

	const now = (input.now ?? new Date()).getTime();
	if (now - installedAt < TWO_WEEKS_IN_MILLISECONDS) {
		return { shouldShow: false };
	}

	return { shouldShow: true };
};

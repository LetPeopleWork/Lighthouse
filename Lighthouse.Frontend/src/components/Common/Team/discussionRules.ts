import type { IDiscussWhenSetting } from "../../../models/Refinement/Refinement";
import { isAtLeastOne } from "../../../utils/numberField";

export type DiscussionRule = keyof IDiscussWhenSetting;

export const DEFAULT_DISCUSS_WHEN: Record<DiscussionRule, number> = {
	no: 1,
	yesIf: 2,
};

export type RememberedThresholds = Record<DiscussionRule, number>;

export const isUsableThreshold = (threshold: number): boolean =>
	isAtLeastOne(threshold);

/** Where each rule returns to when switched on: the threshold it has now, else its default. */
export const initialRememberedThresholds = (
	discussWhen: IDiscussWhenSetting,
): RememberedThresholds => ({
	no: discussWhen.no ?? DEFAULT_DISCUSS_WHEN.no,
	yesIf: discussWhen.yesIf ?? DEFAULT_DISCUSS_WHEN.yesIf,
});

/** Switching a rule off keeps its threshold, unless that threshold is off already or not allowed. */
export const rememberOnSwitchOff = (
	remembered: RememberedThresholds,
	rule: DiscussionRule,
	current: number | null,
): RememberedThresholds =>
	current !== null && isUsableThreshold(current)
		? { ...remembered, [rule]: current }
		: remembered;

/** The threshold a rule holds after being switched on or off. */
export const thresholdAfterToggle = (
	remembered: RememberedThresholds,
	rule: DiscussionRule,
	on: boolean,
): number | null => (on ? remembered[rule] : null);

export const withThreshold = (
	discussWhen: IDiscussWhenSetting,
	rule: DiscussionRule,
	threshold: number | null,
): IDiscussWhenSetting => ({ ...discussWhen, [rule]: threshold });

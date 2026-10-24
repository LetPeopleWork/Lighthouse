import { describe, expect, it } from "vitest";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import { type PremiumVerdict, premiumVerdictOf } from "./premiumVerdict";

const known = (canUsePremiumFeatures: boolean): ILicenseStatus => ({
	hasLicense: canUsePremiumFeatures,
	isValid: canUsePremiumFeatures,
	canUsePremiumFeatures,
});

const cases: [string, unknown, PremiumVerdict][] = [
	["a licence nobody has read yet is not known yet", null, "unknown"],
	["a licence the hook has nothing for is not known yet", undefined, "unknown"],
	[
		"an answer that does not say whether premium features may be used is not known yet",
		{ hasLicense: true, isValid: true },
		"unknown",
	],
	[
		"an answer that says it with anything but true or false is not known yet",
		{ ...known(true), canUsePremiumFeatures: "true" },
		"unknown",
	],
	["an empty answer is not known yet", {}, "unknown"],
	["a licence that allows premium features is premium", known(true), "premium"],
	[
		"a licence that does not allow them is not premium",
		known(false),
		"notPremium",
	],
	[
		"an expired licence that still names its holder is not premium",
		{ ...known(false), hasLicense: true, name: "Lena Hoffmann" },
		"notPremium",
	],
];

describe("what the dialog may say about its premium tabs", () => {
	it.each(cases)("%s", (_case, licenseStatus, verdict) => {
		expect(premiumVerdictOf(licenseStatus as ILicenseStatus | null)).toBe(
			verdict,
		);
	});
});

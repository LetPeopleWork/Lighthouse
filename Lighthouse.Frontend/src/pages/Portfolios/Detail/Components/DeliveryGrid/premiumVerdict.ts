import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";

/**
 * What the dialog may say about its premium tabs. Only a licence whose answer says, as true or false,
 * whether premium features may be used is known; anything else the licence hook hands over (nothing
 * yet, a failed check, a shape still loading) is "not known yet", never "not premium".
 */
export type PremiumVerdict = "unknown" | "premium" | "notPremium";

export function premiumVerdictOf(
	licenseStatus: ILicenseStatus | null | undefined | Partial<ILicenseStatus>,
): PremiumVerdict {
	const canUsePremiumFeatures: unknown = licenseStatus?.canUsePremiumFeatures;
	if (typeof canUsePremiumFeatures !== "boolean") {
		return "unknown";
	}
	if (canUsePremiumFeatures) {
		return "premium";
	}
	return "notPremium";
}

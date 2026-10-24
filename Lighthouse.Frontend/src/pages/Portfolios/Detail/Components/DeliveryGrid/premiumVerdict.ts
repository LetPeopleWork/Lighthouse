import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";

export const __SCAFFOLD__ = true;

/**
 * What the dialog may say about its premium tabs. Only a licence whose answer says, as true or false,
 * whether premium features may be used is known; anything else the licence hook hands over (nothing
 * yet, a failed check, a shape still loading) is "not known yet", never "not premium".
 */
export type PremiumVerdict = "unknown" | "premium" | "notPremium";

export function premiumVerdictOf(
	_licenseStatus: ILicenseStatus | null | undefined | Partial<ILicenseStatus>,
): PremiumVerdict {
	throw new Error("premiumVerdictOf is not built yet");
}

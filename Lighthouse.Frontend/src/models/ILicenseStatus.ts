import { z } from "zod";

// The dates arrive as JSON null when absent, and the licensee's details are left out altogether
// for anyone but a System Admin, so accept both and normalise to `string | undefined` / `Date | undefined`.
const optionalString = z
	.string()
	.nullish()
	.transform((value) => value ?? undefined);
const optionalDate = z.coerce
	.date()
	.nullish()
	.transform((value) => value ?? undefined);

export interface ILicenseStatus {
	hasLicense: boolean;
	isValid: boolean;
	canUsePremiumFeatures: boolean;
	name?: string;
	email?: string;
	organization?: string;
	expiryDate?: Date;
	validFrom?: Date;
	licenseNumber?: string;
}

export const LicenseStatusSchema = z.object({
	hasLicense: z.boolean(),
	isValid: z.boolean(),
	canUsePremiumFeatures: z.boolean().optional().default(false),
	name: optionalString,
	email: optionalString,
	organization: optionalString,
	expiryDate: optionalDate,
	validFrom: optionalDate,
	licenseNumber: optionalString,
});

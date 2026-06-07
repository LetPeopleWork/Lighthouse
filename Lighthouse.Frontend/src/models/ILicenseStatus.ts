import { z } from "zod";

// Backend serialises absent optional members as JSON null (not omitted), so accept
// null and normalise to undefined to keep the `string | undefined` / `Date | undefined` contract.
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

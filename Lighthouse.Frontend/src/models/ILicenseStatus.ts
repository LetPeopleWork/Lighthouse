import { z } from "zod";

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
	name: z.string().optional(),
	email: z.string().optional(),
	organization: z.string().optional(),
	expiryDate: z.coerce.date().optional(),
	validFrom: z.coerce.date().optional(),
	licenseNumber: z.string().optional(),
});

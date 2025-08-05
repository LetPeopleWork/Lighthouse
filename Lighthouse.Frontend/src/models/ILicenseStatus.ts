export interface ILicenseStatus {
	hasLicense: boolean;
	isValid: boolean;
	canUsePremiumFeatures: boolean;
	name?: string;
	email?: string;
	organization?: string;
	expiryDate?: Date;
}

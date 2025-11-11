export interface ILicenseStatus {
	hasLicense: boolean;
	isValid: boolean;
	name?: string;
	email?: string;
	organization?: string;
	expiryDate?: Date;
	validFrom?: Date;
	licenseNumber?: string;
}

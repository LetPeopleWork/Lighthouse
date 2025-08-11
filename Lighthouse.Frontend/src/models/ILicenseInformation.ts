export interface ILicenseStatus {
	hasLicense: boolean;
	isValid: boolean;
	name?: string;
	email?: string;
	organization?: string;
	expiryDate?: Date;
	licenseNumber?: string;
}

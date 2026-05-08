export interface RbacStatus {
	enabled: boolean;
	premiumGateSatisfied: boolean;
	hasSystemAdmin: boolean;
	hasEmergencyAdminConfigured: boolean;
	readyForEnablement: boolean;
}

export interface RbacUser {
	id: number;
	subject: string;
	displayName?: string;
	email?: string;
	isSystemAdmin: boolean;
}

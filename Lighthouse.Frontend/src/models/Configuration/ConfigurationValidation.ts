export type ConfigurationValidationStatus = "New" | "Update" | "Error";

export interface ConfigurationValidationItem {
	id: number;
	name: string;
	status: ConfigurationValidationStatus;
	errorMessage: string;
}

export interface ConfigurationValidation {
	workTrackingSystems: ConfigurationValidationItem[];
	teams: ConfigurationValidationItem[];
	projects: ConfigurationValidationItem[];
}

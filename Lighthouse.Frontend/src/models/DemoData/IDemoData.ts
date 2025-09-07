export interface IDemoDataScenario {
	id: string;
	title: string;
	description: string;
	numberOfTeams: number;
	numberOfProjects: number;
	isPremium: boolean;
}

export interface IDemoDataService {
	getAvailableScenarios(): Promise<IDemoDataScenario[]>;
	loadScenario(scenarioId: string): Promise<void>;
	loadAllScenarios(): Promise<void>;
}

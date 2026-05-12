import type React from "react";

export interface IScopeRow {
	role?: string;
	scopeType?: string;
	scopeId?: number;
}

export interface ITeamOption {
	id: number;
	name: string;
}

export interface IPortfolioOption {
	id: number;
	name: string;
}

export interface IScopeRowListProps {
	rows: IScopeRow[];
	onChange: (rows: IScopeRow[]) => void;
	availableTeams: ITeamOption[];
	availablePortfolios: IPortfolioOption[];
}

export const __SCAFFOLD__ = true;

const ScopeRowList: React.FC<IScopeRowListProps> = () => {
	throw new Error("Not yet implemented — RED scaffold");
};

export default ScopeRowList;

export interface IMilestone {
	id: number;
	name: string;
	date: Date;
}

export class Milestone implements IMilestone {
	id: number;
	name: string;
	date: Date;

	constructor(id: number, name: string, date: Date) {
		this.id = id;
		this.name = name;
		this.date = date;
	}
}

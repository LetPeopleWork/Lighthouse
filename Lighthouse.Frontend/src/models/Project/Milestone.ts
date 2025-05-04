import { Type, plainToInstance } from "class-transformer";
import "reflect-metadata";

export interface IMilestone {
	id: number;
	name: string;
	date: Date;
}

export class Milestone implements IMilestone {
	id = 0;
	name = "";

	@Type(() => Date)
	date: Date = new Date();

	static fromBackend(data: IMilestone): Milestone {
		return plainToInstance(Milestone, data);
	}

	static new(id: number, name: string, date: Date) {
		const milestone = new Milestone();
		milestone.id = id;
		milestone.name = name;
		milestone.date = date;

		return milestone;
	}
}

export interface IMilestone {
    name: string;
    date: Date;
}

export class Milestone implements IMilestone {
    name: string;
    date: Date;

    constructor(name: string, date: Date) {
        this.name = name;
        this.date = date;
    }
}
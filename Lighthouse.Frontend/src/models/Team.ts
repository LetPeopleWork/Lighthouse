export class Team {
    name!: string;
    id!: number;
    remainingWork!: number;
    projects!: number;
    features!: number;

    constructor(name: string, id: number, remainingWork: number, projects: number, features: number) {
        this.name = name;
        this.id = id;
        this.remainingWork = remainingWork;
        this.projects = projects;
        this.features = features;
    }
}
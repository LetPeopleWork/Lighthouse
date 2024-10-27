import { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";

export interface IWorkTrackingSystemConnection {
    id : number | null;
    name: string;
    workTrackingSystem: string;
    options: IWorkTrackingSystemOption[]
}

export class WorkTrackingSystemConnection implements IWorkTrackingSystemConnection {
    id : number | null;
    name: string;
    workTrackingSystem: string;
    options: IWorkTrackingSystemOption[]

    constructor(name : string, workTrackingSystem: string, options: IWorkTrackingSystemOption[], id: number | null = null) {
        this.id = id;
        this.name = name;
        this.workTrackingSystem = workTrackingSystem;
        this.options = options;
    }
}
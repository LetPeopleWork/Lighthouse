export interface IWorkTrackingSystemOption {
    key: string;
    value: string;
    isSecret: boolean;
}

export class WorkTrackingSystemOption implements IWorkTrackingSystemOption {
    key: string;
    value: string;
    isSecret: boolean;

    constructor(key: string, value: string, isScecret: boolean) {
        this.key = key;
        this.value = value;
        this.isSecret = isScecret;
    }
}
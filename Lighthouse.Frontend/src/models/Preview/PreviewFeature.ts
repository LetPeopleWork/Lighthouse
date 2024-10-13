export interface IPreviewFeature {
    name: string;
    key: string;
    id: number;
    description: string;
    enabled: boolean;
}

export class PreviewFeature implements IPreviewFeature {
    name: string;
    key: string;
    id: number;
    description: string;
    enabled: boolean;

    constructor(id: number, key: string, name: string, description: string, enabled: boolean) {
        this.name = name;
        this.key = key;
        this.id = id;
        this.description = description;
        this.enabled = enabled;
    }
}
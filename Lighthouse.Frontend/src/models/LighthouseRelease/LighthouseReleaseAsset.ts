export interface ILighthouseReleaseAsset {
    name: string;
    link: string;
}

export class LighthouseReleaseAsset implements ILighthouseReleaseAsset {
    name: string;
    link: string;

    constructor(name: string, link: string) {
        this.name = name;
        this.link = link;
    }
}
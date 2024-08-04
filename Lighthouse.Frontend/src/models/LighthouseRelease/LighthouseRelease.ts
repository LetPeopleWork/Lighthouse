import { ILighthouseReleaseAsset } from "./LighthouseReleaseAsset";

export interface ILighthouseRelease {
    name: string;
    link: string;
    highlights: string;
    version: string;
    assets: ILighthouseReleaseAsset[];
}

export class LighthouseRelease implements ILighthouseRelease {
    name: string;
    link: string;
    highlights: string;
    version: string;
    assets: ILighthouseReleaseAsset[];

    constructor(name: string, link: string, highlights: string, version: string, assets: ILighthouseReleaseAsset[]) {
        this.name = name;
        this.link = link;
        this.highlights = highlights;
        this.version = version;
        this.assets = assets;
    }
}
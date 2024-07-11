export interface IWhenForecast {
    probability: number;
    expectedDate: Date;
}

export class WhenForecast implements IWhenForecast {
    probability! : number
    expectedDate! : Date

    constructor(probability: number, expectedDate: Date){
        this.probability = probability;
        this.expectedDate = expectedDate;
    }
}
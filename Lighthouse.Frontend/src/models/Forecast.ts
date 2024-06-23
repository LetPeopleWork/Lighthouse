export class Forecast{
    probability! : number
    expectedDate! : Date

    constructor(probability: number, expectedDate: Date){
        this.probability = probability;
        this.expectedDate = expectedDate;
    }
}
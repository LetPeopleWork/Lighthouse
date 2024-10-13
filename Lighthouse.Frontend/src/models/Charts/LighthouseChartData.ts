import { IMilestone } from "../Project/Milestone";

export const featureColors = [
    "#3f51b5", // Indigo
    "#e57373", // Light Red
    "#81c784", // Light Green
    "#64b5f6", // Light Blue
    "#ba68c8", // Light Purple
    "#4dd0e1", // Light Cyan
    "#f06292", // Light Pink
    "#c5e1a5", // Pale Green
    "#ff7043", // Light Deep Orange
    "#90a4ae", // Light Blue Grey
    "#bcaaa4", // Light Brown
    "#bdbdbd", // Light Grey
    "#ffb74d", // Light Orange
    "#9575cd", // Medium Purple
    "#dce775", // Light Lime
    "#ffd54f", // Light Amber
    "#66bb6a", // Medium Dark Green
    "#7986cb", // Medium Dark Indigo
    "#f48fb1", // Light Pink
    "#e57373", // Medium Dark Red
    "#42a5f5"  // Medium Dark Blue
];

const usedColors = new Set<string>();

export interface ILighthouseChartData {
    startDate: Date;
    endDate: Date;
    maxRemainingItems: number;

    features: ILighthouseChartFeatureData[];
    milestones: IMilestone[];
}

export interface ILighthouseChartFeatureData {
    name: string;
    color: string;
    forecasts: Date[];
    remainingItemsTrend: IBurndownEntry[];
}

export interface IBurndownEntry {
    date: Date;
    remainingItems: number;
}

export class LighthouseChartData implements ILighthouseChartData {
    startDate: Date;
    endDate: Date;
    maxRemainingItems: number;
    features: ILighthouseChartFeatureData[];
    milestones: IMilestone[];

    constructor(features: ILighthouseChartFeatureData[], milestones: IMilestone[]) {
        this.features = features;
        this.milestones = milestones;

        const dates: Date[] = [];
        const remainingItems: number[] = [];

        for (const milestone of milestones) {
            dates.push(milestone.date);
        }

        for (const feature of features) {
            dates.push(...feature.forecasts);
            for (const entry of feature.remainingItemsTrend) {
                dates.push(entry.date);
                remainingItems.push(entry.remainingItems);
            }
        }

        this.startDate = new Date(Math.min(...dates.map(date => date.getTime())));
        this.startDate.setDate(this.startDate.getDate() - 2);

        this.endDate = new Date(Math.max(...dates.map(date => date.getTime())));
        this.endDate.setDate(this.endDate.getDate() + 2); 

        this.maxRemainingItems = Math.max(...remainingItems);
    }
}

export class LighthouseChartFeatureData implements ILighthouseChartFeatureData {
    name: string;
    color: string;
    forecasts: Date[];
    remainingItemsTrend: IBurndownEntry[];

    constructor(name: string, forecastData: Date[], remainingItemsTrend: IBurndownEntry[]) {
        this.name = name;
        this.color = this.assignRandomColor(); 
        this.forecasts = forecastData;
        this.remainingItemsTrend = remainingItemsTrend;
    }

    private assignRandomColor(): string {
        let color;
        do {
            const randomIndex = Math.floor(Math.random() * featureColors.length);
            color = featureColors[randomIndex];
        } while (usedColors.has(color));
        
        usedColors.add(color);
        return color;
    }
}

export class BurndownEntry implements IBurndownEntry {
    date: Date;
    remainingItems: number;

    constructor(date: Date, remainingItems: number) {
        this.date = date;
        this.remainingItems = remainingItems;
    }
}
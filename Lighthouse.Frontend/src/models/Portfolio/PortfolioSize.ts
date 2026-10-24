/** How many Features a Portfolio held after its last update, and whether that is past the point pages slow down. */
export interface IPortfolioSize {
	featureCount: number;
	threshold: number;
	isOverThreshold: boolean;
}

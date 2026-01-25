import type { IPortfolio, Portfolio } from "../../models/Portfolio/Portfolio";
import type { IPortfolioSettings } from "../../models/Portfolio/PortfolioSettings";
import { BaseApiService } from "./BaseApiService";

export interface IPortfolioService {
	getPortfolios(): Promise<Portfolio[]>;
	deletePortfolio(id: number): Promise<void>;
	getPortfolio(id: number): Promise<Portfolio | null>;
	getPortfolioSettings(id: number): Promise<IPortfolioSettings>;
	updatePortfolio(
		portfolioSettings: IPortfolioSettings,
	): Promise<IPortfolioSettings>;
	createPortfolio(
		portfolioSettings: IPortfolioSettings,
	): Promise<IPortfolioSettings>;
	refreshFeaturesForPortfolio(id: number): Promise<void>;
	refreshFeaturesForAllPortfolios(): Promise<void>;
	refreshForecastsForPortfolio(id: number): Promise<void>;
	validatePortfolioSettings(
		portfolioSettings: IPortfolioSettings,
	): Promise<boolean>;
}

export class PortfolioService
	extends BaseApiService
	implements IPortfolioService
{
	async getPortfolios(): Promise<Portfolio[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPortfolio[]>("/portfolios");
			return response.data.map(BaseApiService.deserializePortfolio);
		});
	}

	async getPortfolio(id: number): Promise<Portfolio | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPortfolio>(
				`/portfolios/${id}`,
			);
			return BaseApiService.deserializePortfolio(response.data);
		});
	}

	async deletePortfolio(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/portfolios/${id}`);
		});
	}

	async getPortfolioSettings(id: number): Promise<IPortfolioSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPortfolioSettings>(
				`/portfolios/${id}/settings`,
			);
			return response.data;
		});
	}

	async updatePortfolio(
		portfolioSettings: IPortfolioSettings,
	): Promise<IPortfolioSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<IPortfolioSettings>(
				`/portfolios/${portfolioSettings.id}`,
				portfolioSettings,
			);
			return response.data;
		});
	}

	async createPortfolio(
		portfolioSettings: IPortfolioSettings,
	): Promise<IPortfolioSettings> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<IPortfolioSettings>(
				"/portfolios",
				portfolioSettings,
			);
			return response.data;
		});
	}

	async refreshFeaturesForPortfolio(id: number): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<IPortfolio>(`/portfolios/${id}/refresh`);
		});
	}

	async refreshFeaturesForAllPortfolios(): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post(`/portfolios/refresh-all`);
		});
	}

	async refreshForecastsForPortfolio(id: number): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<IPortfolio>(`/forecast/${id}/update`);
		});
	}

	async validatePortfolioSettings(
		portfolioSettings: IPortfolioSettings,
	): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<boolean>(
				"/portfolios/validate",
				portfolioSettings,
			);
			return response.data;
		});
	}
}

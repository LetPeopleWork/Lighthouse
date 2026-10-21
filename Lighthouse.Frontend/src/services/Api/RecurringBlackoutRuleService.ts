import type { IRecurringBlackoutRule } from "../../models/RecurringBlackoutRule";
import { BaseApiService } from "./BaseApiService";

export type RecurringBlackoutRulePayload = Omit<
	IRecurringBlackoutRule,
	"id" | "summary"
>;

export interface IRecurringBlackoutRuleService {
	getAll(): Promise<IRecurringBlackoutRule[]>;
	create(rule: RecurringBlackoutRulePayload): Promise<IRecurringBlackoutRule>;
	update(
		id: number,
		rule: RecurringBlackoutRulePayload,
	): Promise<IRecurringBlackoutRule>;
	delete(id: number): Promise<void>;
}

export class RecurringBlackoutRuleService
	extends BaseApiService
	implements IRecurringBlackoutRuleService
{
	public getAll(): Promise<IRecurringBlackoutRule[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IRecurringBlackoutRule[]>(
				"/recurring-blackout-rules",
			);
			return response.data;
		});
	}

	public create(
		rule: RecurringBlackoutRulePayload,
	): Promise<IRecurringBlackoutRule> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IRecurringBlackoutRule>(
				"/recurring-blackout-rules",
				rule,
			);
			return response.data;
		});
	}

	public update(
		id: number,
		rule: RecurringBlackoutRulePayload,
	): Promise<IRecurringBlackoutRule> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<IRecurringBlackoutRule>(
				`/recurring-blackout-rules/${id}`,
				rule,
			);
			return response.data;
		});
	}

	public delete(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/recurring-blackout-rules/${id}`);
		});
	}
}

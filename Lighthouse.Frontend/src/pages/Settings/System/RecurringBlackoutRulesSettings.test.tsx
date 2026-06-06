import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IRecurringBlackoutRule } from "../../../models/RecurringBlackoutRule";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IRecurringBlackoutRuleService } from "../../../services/Api/RecurringBlackoutRuleService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockRecurringBlackoutRuleService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import RecurringBlackoutRulesSettings from "./RecurringBlackoutRulesSettings";

const getMockRule = (
	overrides?: Partial<IRecurringBlackoutRule>,
): IRecurringBlackoutRule => ({
	id: 1,
	weekdays: ["Saturday", "Sunday"],
	intervalWeeks: 2,
	start: "2026-01-03",
	end: null,
	description: "Sprint weekends",
	summary: "Every Saturday, Sunday — every 2 weeks — from 2026-01-03 — no end",
	...overrides,
});

const renderComponent = (
	service: IRecurringBlackoutRuleService,
	isPremium = true,
) => {
	const mockContext = createMockApiServiceContext({
		recurringBlackoutRuleService: service,
		terminologyService: createMockTerminologyService(),
	});

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockContext}>
				<TerminologyProvider>
					<RecurringBlackoutRulesSettings isPremium={isPremium} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("RecurringBlackoutRulesSettings", () => {
	afterEach(() => {
		vi.clearAllMocks();
		vi.restoreAllMocks();
	});

	it("should list the server-provided summary of each recurring rule", async () => {
		const service = createMockRecurringBlackoutRuleService();
		service.getAll = vi.fn().mockResolvedValue([getMockRule()]);

		renderComponent(service);

		await waitFor(() => {
			expect(
				screen.getByText(
					"Every Saturday, Sunday — every 2 weeks — from 2026-01-03 — no end",
				),
			).toBeVisible();
		});
	});

	it("should disable add and hide row actions when not premium", async () => {
		const service = createMockRecurringBlackoutRuleService();
		service.getAll = vi.fn().mockResolvedValue([getMockRule()]);

		renderComponent(service, false);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-recurring-blackout-rule-button"),
			).toBeDisabled();
		});

		expect(
			screen.queryByTestId("edit-recurring-blackout-1"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("delete-recurring-blackout-1"),
		).not.toBeInTheDocument();
	});

	it("should surface the backend validation message when saving fails", async () => {
		const service = createMockRecurringBlackoutRuleService();
		service.getAll = vi.fn().mockResolvedValue([]);
		service.create = vi
			.fn()
			.mockRejectedValue(new Error("At least one weekday must be selected."));

		renderComponent(service);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-recurring-blackout-rule-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-recurring-blackout-rule-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("recurring-weekday-Saturday"));

		const startInput = screen
			.getByTestId("recurring-start-date")
			.querySelector("input");
		if (!startInput) {
			throw new Error("Start input not found");
		}
		fireEvent.change(startInput, { target: { value: "2026-01-03" } });

		fireEvent.click(screen.getByTestId("save-recurring-blackout-rule"));

		await waitFor(() => {
			expect(
				screen.getByText("At least one weekday must be selected."),
			).toBeVisible();
		});
	});

	it("should require at least one weekday before calling the service", async () => {
		const service = createMockRecurringBlackoutRuleService();
		service.getAll = vi.fn().mockResolvedValue([]);

		renderComponent(service);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-recurring-blackout-rule-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-recurring-blackout-rule-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("recurring-start-date")
			.querySelector("input");
		if (!startInput) {
			throw new Error("Start input not found");
		}
		fireEvent.change(startInput, { target: { value: "2026-01-03" } });

		fireEvent.click(screen.getByTestId("save-recurring-blackout-rule"));

		await waitFor(() => {
			expect(
				screen.getByText("Select at least one weekday and a start date."),
			).toBeVisible();
		});

		expect(service.create).not.toHaveBeenCalled();
	});
});

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { IRecurringBlackoutRule } from "../../../models/RecurringBlackoutRule";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IBlackoutPeriodService } from "../../../services/Api/BlackoutPeriodService";
import type { IRecurringBlackoutRuleService } from "../../../services/Api/RecurringBlackoutRuleService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockRecurringBlackoutRuleService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import BlackoutSettings from "./BlackoutSettings";

const getMockPeriod = (
	overrides?: Partial<IBlackoutPeriod>,
): IBlackoutPeriod => ({
	id: 1,
	start: "2026-01-01",
	end: "2026-01-05",
	description: "New Year",
	...overrides,
});

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
	periodService: IBlackoutPeriodService,
	ruleService: IRecurringBlackoutRuleService,
	isPremium = true,
) => {
	const mockContext = createMockApiServiceContext({
		blackoutPeriodService: periodService,
		recurringBlackoutRuleService: ruleService,
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
					<BlackoutSettings isPremium={isPremium} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("BlackoutSettings", () => {
	afterEach(() => {
		vi.clearAllMocks();
		vi.restoreAllMocks();
	});

	it("shows a single empty-state row when both lists are empty", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByText("No blackout periods or recurring rules configured."),
			).toBeVisible();
		});
	});

	it("renders one-off periods and recurring rules in one merged table", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi
			.fn()
			.mockResolvedValue([getMockPeriod({ description: "New Year" })]);
		ruleService.getAll = vi.fn().mockResolvedValue([getMockRule()]);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByText("New Year")).toBeVisible();
		});

		expect(screen.getByText("2026-01-01 → 2026-01-05")).toBeVisible();
		expect(
			screen.getByText(
				"Every Saturday, Sunday — every 2 weeks — from 2026-01-03 — no end",
			),
		).toBeVisible();
		expect(screen.getByText("Sprint weekends")).toBeVisible();
		expect(screen.getByTestId("blackout-period-row-1")).toBeVisible();
		expect(screen.getByTestId("recurring-blackout-row-1")).toBeVisible();
	});

	it("disables both add buttons and hides row actions when not premium", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi.fn().mockResolvedValue([getMockPeriod()]);
		ruleService.getAll = vi.fn().mockResolvedValue([getMockRule()]);

		renderComponent(periodService, ruleService, false);

		await waitFor(() => {
			expect(screen.getByTestId("add-blackout-period-button")).toBeDisabled();
		});

		expect(
			screen.getByTestId("add-recurring-blackout-rule-button"),
		).toBeDisabled();
		expect(screen.queryByTestId("edit-blackout-1")).not.toBeInTheDocument();
		expect(screen.queryByTestId("delete-blackout-1")).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("edit-recurring-blackout-1"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("delete-recurring-blackout-1"),
		).not.toBeInTheDocument();
	});

	it("shows a single info alert about premium when not premium", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService, false);

		await waitFor(() => {
			expect(screen.getByText(/requires a premium license/)).toBeVisible();
		});
	});

	it("creates a one-off blackout period through the add dialog", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.create = vi.fn().mockResolvedValue(
			getMockPeriod({
				id: 2,
				start: "2026-04-10",
				end: "2026-04-15",
				description: "Spring break",
			}),
		);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-blackout-period-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-blackout-period-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("blackout-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("blackout-end-date")
			.querySelector("input");
		const descInput = screen
			.getByTestId("blackout-description")
			.querySelector("input");

		if (!startInput || !endInput || !descInput) {
			throw new Error("Form inputs not found");
		}

		fireEvent.change(startInput, { target: { value: "2026-04-10" } });
		fireEvent.change(endInput, { target: { value: "2026-04-15" } });
		fireEvent.change(descInput, { target: { value: "Spring break" } });

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(periodService.create).toHaveBeenCalledWith({
				start: "2026-04-10",
				end: "2026-04-15",
				description: "Spring break",
			});
		});
	});

	it("requires both dates before creating a one-off period", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-blackout-period-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-blackout-period-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(
				screen.getByText("Start and end dates are required."),
			).toBeVisible();
		});

		expect(periodService.create).not.toHaveBeenCalled();
	});

	it("refetches and renders the new period row after creating it", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi
			.fn()
			.mockResolvedValueOnce([])
			.mockResolvedValueOnce([
				getMockPeriod({ id: 2, description: "Spring break" }),
			]);
		periodService.create = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-blackout-period-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-blackout-period-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("blackout-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("blackout-end-date")
			.querySelector("input");
		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}
		fireEvent.change(startInput, { target: { value: "2026-01-01" } });
		fireEvent.change(endInput, { target: { value: "2026-01-05" } });

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(screen.getByTestId("blackout-period-row-2")).toBeVisible();
		});
		expect(screen.getByText("Spring break")).toBeVisible();
		expect(periodService.getAll).toHaveBeenCalledTimes(2);
	});

	it("prefills the edit dialog and updates the period with its id", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi
			.fn()
			.mockResolvedValue([
				getMockPeriod({ id: 7, start: "2026-03-01", end: "2026-03-05" }),
			]);
		periodService.update = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByTestId("edit-blackout-7")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("edit-blackout-7"));

		await waitFor(() => {
			expect(screen.getByText("Edit Blackout Period")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("blackout-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("blackout-end-date")
			.querySelector("input");
		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}
		expect(startInput.value).toBe("2026-03-01");
		expect(endInput.value).toBe("2026-03-05");

		fireEvent.change(endInput, { target: { value: "2026-03-10" } });
		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(periodService.update).toHaveBeenCalledWith(7, {
				start: "2026-03-01",
				end: "2026-03-10",
				description: "New Year",
			});
		});
		expect(periodService.create).not.toHaveBeenCalled();
	});

	it("shows the fallback message when a period save throws a non-Error", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.create = vi.fn().mockRejectedValue("boom");

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-blackout-period-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-blackout-period-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("blackout-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("blackout-end-date")
			.querySelector("input");
		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}
		fireEvent.change(startInput, { target: { value: "2026-01-01" } });
		fireEvent.change(endInput, { target: { value: "2026-01-05" } });

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(screen.getByText("Failed to save blackout period.")).toBeVisible();
		});
	});

	it("validates the one-off period start is on or before end", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-blackout-period-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-blackout-period-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("blackout-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("blackout-end-date")
			.querySelector("input");

		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}

		fireEvent.change(startInput, { target: { value: "2026-04-15" } });
		fireEvent.change(endInput, { target: { value: "2026-04-10" } });

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(
				screen.getByText("Start date must be on or before end date."),
			).toBeVisible();
		});

		expect(periodService.create).not.toHaveBeenCalled();
	});

	it("deletes a one-off period after confirmation", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi.fn().mockResolvedValue([getMockPeriod()]);
		periodService.delete = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByText("New Year")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("delete-blackout-1"));

		await waitFor(() => {
			expect(
				screen.getByText(
					"Are you sure you want to delete this blackout period?",
				),
			).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("confirm-delete-blackout"));

		await waitFor(() => {
			expect(periodService.delete).toHaveBeenCalledWith(1);
		});
	});

	it("does not delete a one-off period when the dialog is cancelled", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi.fn().mockResolvedValue([getMockPeriod()]);
		periodService.delete = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByText("New Year")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("delete-blackout-1"));

		await waitFor(() => {
			expect(
				screen.getByText(
					"Are you sure you want to delete this blackout period?",
				),
			).toBeVisible();
		});

		fireEvent.click(screen.getByText("Cancel"));

		await waitFor(() => {
			expect(
				screen.queryByText(
					"Are you sure you want to delete this blackout period?",
				),
			).not.toBeInTheDocument();
		});
		expect(periodService.delete).not.toHaveBeenCalled();
	});

	it("creates a recurring rule with the toggled weekdays and null open end", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		ruleService.create = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-recurring-blackout-rule-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-recurring-blackout-rule-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("recurring-weekday-Monday"));
		fireEvent.click(screen.getByTestId("recurring-weekday-Wednesday"));
		fireEvent.click(screen.getByTestId("recurring-weekday-Wednesday"));
		fireEvent.click(screen.getByTestId("recurring-weekday-Friday"));

		const intervalInput = screen
			.getByTestId("recurring-interval-weeks")
			.querySelector("input");
		const startInput = screen
			.getByTestId("recurring-start-date")
			.querySelector("input");
		const descInput = screen
			.getByTestId("recurring-description")
			.querySelector("input");
		if (!intervalInput || !startInput || !descInput) {
			throw new Error("Form inputs not found");
		}
		fireEvent.change(intervalInput, { target: { value: "3" } });
		fireEvent.change(startInput, { target: { value: "2026-02-02" } });
		fireEvent.change(descInput, { target: { value: "Maintenance" } });

		fireEvent.click(screen.getByTestId("save-recurring-blackout-rule"));

		await waitFor(() => {
			expect(ruleService.create).toHaveBeenCalledWith({
				weekdays: ["Monday", "Friday"],
				intervalWeeks: 3,
				start: "2026-02-02",
				end: null,
				description: "Maintenance",
			});
		});
	});

	it("validates the recurring rule end is on or after start", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(
				screen.getByTestId("add-recurring-blackout-rule-button"),
			).not.toBeDisabled();
		});

		fireEvent.click(screen.getByTestId("add-recurring-blackout-rule-button"));

		await waitFor(() => {
			expect(screen.getByRole("dialog")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("recurring-weekday-Tuesday"));

		const startInput = screen
			.getByTestId("recurring-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("recurring-end-date")
			.querySelector("input");
		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}
		fireEvent.change(startInput, { target: { value: "2026-02-10" } });
		fireEvent.change(endInput, { target: { value: "2026-02-05" } });

		fireEvent.click(screen.getByTestId("save-recurring-blackout-rule"));

		await waitFor(() => {
			expect(
				screen.getByText("Start date must be on or before end date."),
			).toBeVisible();
		});
		expect(ruleService.create).not.toHaveBeenCalled();
	});

	it("prefills the recurring edit dialog and updates the rule with its id", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		ruleService.getAll = vi.fn().mockResolvedValue([
			getMockRule({
				id: 9,
				weekdays: ["Monday"],
				intervalWeeks: 2,
				start: "2026-01-05",
				end: "2026-12-31",
				description: "Quarterly",
			}),
		]);
		ruleService.update = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByTestId("edit-recurring-blackout-9")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("edit-recurring-blackout-9"));

		await waitFor(() => {
			expect(screen.getByText("Edit Recurring Blackout Rule")).toBeVisible();
		});

		const startInput = screen
			.getByTestId("recurring-start-date")
			.querySelector("input");
		const endInput = screen
			.getByTestId("recurring-end-date")
			.querySelector("input");
		if (!startInput || !endInput) {
			throw new Error("Date inputs not found");
		}
		expect(startInput.value).toBe("2026-01-05");
		expect(endInput.value).toBe("2026-12-31");
		expect(
			(
				screen
					.getByTestId("recurring-weekday-Monday")
					.querySelector("input") as HTMLInputElement
			).checked,
		).toBe(true);

		fireEvent.click(screen.getByTestId("recurring-weekday-Tuesday"));
		fireEvent.click(screen.getByTestId("save-recurring-blackout-rule"));

		await waitFor(() => {
			expect(ruleService.update).toHaveBeenCalledWith(9, {
				weekdays: ["Monday", "Tuesday"],
				intervalWeeks: 2,
				start: "2026-01-05",
				end: "2026-12-31",
				description: "Quarterly",
			});
		});
		expect(ruleService.create).not.toHaveBeenCalled();
	});

	it("requires at least one weekday before creating a recurring rule", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();

		renderComponent(periodService, ruleService);

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

		expect(ruleService.create).not.toHaveBeenCalled();
	});

	it("surfaces the backend message when a recurring rule save fails", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		ruleService.create = vi
			.fn()
			.mockRejectedValue(new Error("At least one weekday must be selected."));

		renderComponent(periodService, ruleService);

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

	it("deletes a recurring rule after confirmation", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		ruleService.getAll = vi.fn().mockResolvedValue([getMockRule()]);
		ruleService.delete = vi.fn().mockResolvedValue(undefined);

		renderComponent(periodService, ruleService);

		await waitFor(() => {
			expect(screen.getByTestId("recurring-blackout-row-1")).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("delete-recurring-blackout-1"));

		await waitFor(() => {
			expect(
				screen.getByText(
					"Are you sure you want to delete this recurring blackout rule?",
				),
			).toBeVisible();
		});

		fireEvent.click(screen.getByTestId("confirm-delete-recurring-blackout"));

		await waitFor(() => {
			expect(ruleService.delete).toHaveBeenCalledWith(1);
		});
	});

	it("renders and dismisses the load-error alert when fetching fails", async () => {
		const periodService = createMockBlackoutPeriodService();
		const ruleService = createMockRecurringBlackoutRuleService();
		periodService.getAll = vi.fn().mockRejectedValue(new Error("network down"));

		renderComponent(periodService, ruleService);

		const alert = await screen.findByText(
			"Failed to load blackout periods or recurring rules",
		);
		expect(alert).toBeVisible();

		fireEvent.click(screen.getByLabelText("Close"));

		await waitFor(() => {
			expect(
				screen.queryByText(
					"Failed to load blackout periods or recurring rules",
				),
			).not.toBeInTheDocument();
		});
	});
});

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IBlackoutPeriodService } from "../../../services/Api/BlackoutPeriodService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import BlackoutPeriodsSettings from "./BlackoutPeriodsSettings";

const setupMocks = () => {
	const mockBlackoutPeriodService: IBlackoutPeriodService =
		createMockBlackoutPeriodService();

	return { mockBlackoutPeriodService };
};

const renderComponent = (
	mockBlackoutPeriodService: IBlackoutPeriodService,
	isPremium = true,
) => {
	const mockContext = createMockApiServiceContext({
		blackoutPeriodService: mockBlackoutPeriodService,
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
					<BlackoutPeriodsSettings isPremium={isPremium} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("BlackoutPeriodsSettings", () => {
	afterEach(() => {
		vi.clearAllMocks();
		vi.restoreAllMocks();
	});

	it("should display empty state when no periods exist", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

		renderComponent(mockBlackoutPeriodService);

		await waitFor(() => {
			expect(screen.getByText("No blackout periods configured.")).toBeVisible();
		});
	});

	it("should display blackout periods in a table", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		const periods: IBlackoutPeriod[] = [
			{
				id: 1,
				start: "2026-01-01",
				end: "2026-01-05",
				description: "New Year",
			},
			{
				id: 2,
				start: "2026-06-01",
				end: "2026-06-03",
				description: "Summer break",
			},
		];
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue(periods);

		renderComponent(mockBlackoutPeriodService);

		await waitFor(() => {
			expect(screen.getByText("New Year")).toBeVisible();
		});
		expect(screen.getByText("Summer break")).toBeVisible();
		expect(screen.getByText("2026-01-01")).toBeVisible();
		expect(screen.getByText("2026-06-03")).toBeVisible();
	});

	it("should disable add button when not premium", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

		renderComponent(mockBlackoutPeriodService, false);

		await waitFor(() => {
			const addButton = screen.getByTestId("add-blackout-period-button");
			expect(addButton).toBeDisabled();
		});
	});

	it("should hide action buttons when not premium", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		const periods: IBlackoutPeriod[] = [
			{
				id: 1,
				start: "2026-01-01",
				end: "2026-01-05",
				description: "New Year",
			},
		];
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue(periods);

		renderComponent(mockBlackoutPeriodService, false);

		await waitFor(() => {
			expect(screen.getByText("New Year")).toBeVisible();
		});

		expect(screen.queryByTestId("edit-blackout-1")).not.toBeInTheDocument();
		expect(screen.queryByTestId("delete-blackout-1")).not.toBeInTheDocument();
	});

	it("should show info alert when not premium", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

		renderComponent(mockBlackoutPeriodService, false);

		await waitFor(() => {
			expect(
				screen.getByText(
					/Managing blackout periods requires a premium license/,
				),
			).toBeVisible();
		});
	});

	it("should open add dialog and create a period", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);
		mockBlackoutPeriodService.create = vi.fn().mockResolvedValue({
			id: 1,
			start: "2026-04-10",
			end: "2026-04-15",
			description: "Spring break",
		});

		renderComponent(mockBlackoutPeriodService);

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

		// After create, getAll is called again
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([
			{
				id: 1,
				start: "2026-04-10",
				end: "2026-04-15",
				description: "Spring break",
			},
		]);

		fireEvent.click(screen.getByTestId("save-blackout-period"));

		await waitFor(() => {
			expect(mockBlackoutPeriodService.create).toHaveBeenCalledWith({
				start: "2026-04-10",
				end: "2026-04-15",
				description: "Spring break",
			});
		});
	});

	it("should show validation error when start is after end", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

		renderComponent(mockBlackoutPeriodService);

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

		expect(mockBlackoutPeriodService.create).not.toHaveBeenCalled();
	});

	it("should open delete confirmation dialog", async () => {
		const { mockBlackoutPeriodService } = setupMocks();
		const periods: IBlackoutPeriod[] = [
			{
				id: 1,
				start: "2026-01-01",
				end: "2026-01-05",
				description: "New Year",
			},
		];
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue(periods);
		mockBlackoutPeriodService.delete = vi.fn().mockResolvedValue(undefined);

		renderComponent(mockBlackoutPeriodService);

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

		// After delete, getAll is called again
		mockBlackoutPeriodService.getAll = vi.fn().mockResolvedValue([]);

		fireEvent.click(screen.getByTestId("confirm-delete-blackout"));

		await waitFor(() => {
			expect(mockBlackoutPeriodService.delete).toHaveBeenCalledWith(1);
		});
	});
});

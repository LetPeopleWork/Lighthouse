import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ThroughputQuickSetting from "./ThroughputQuickSetting";

const getMockProps = (
	overrides?: Partial<{
		useFixedDates: boolean;
		startDate: Date | null;
		endDate: Date | null;
		onSave: (
			useFixedDates: boolean,
			throughputHistory: number,
			startDate: Date | null,
			endDate: Date | null,
		) => Promise<void>;
		disabled: boolean;
		hasBlackoutOverlap: boolean;
		hasForecastFilter: boolean;
	}>,
) => ({
	useFixedDates: false,
	startDate: null,
	endDate: null,
	onSave: vi.fn().mockResolvedValue(undefined),
	disabled: false,
	hasBlackoutOverlap: false,
	hasForecastFilter: false,
	...overrides,
});

describe("ThroughputQuickSetting", () => {
	it("should render icon button with tooltip showing rolling history", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30"); // 30 days
		render(
			<ThroughputQuickSetting {...getMockProps({ startDate, endDate })} />,
		);

		expect(
			screen.getByRole("button", { name: /Throughput: Rolling 30 days/i }),
		).toBeInTheDocument();
	});

	it("should render tooltip showing fixed dates when using fixed dates", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-31");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ useFixedDates: true, startDate, endDate })}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /Throughput: Fixed dates 2024-01-01 to 2024-01-31/i,
			}),
		).toBeInTheDocument();
	});

	it("should show greyed icon when unset (rolling with 0 days)", () => {
		render(<ThroughputQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", { name: /Not set/i });
		expect(button).toBeInTheDocument();
	});

	it("should open editor dialog when icon is clicked", async () => {
		const user = userEvent.setup();
		render(<ThroughputQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", { name: /Throughput/i });
		await user.click(button);

		expect(
			screen.getByRole("heading", { name: /Throughput Configuration/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("checkbox", { name: /Use Fixed Dates/i }),
		).toBeInTheDocument();
	});

	it("should show rolling history input when fixed dates is unchecked", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30"); // 30 days
		render(
			<ThroughputQuickSetting {...getMockProps({ startDate, endDate })} />,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		expect(
			screen.getByRole("spinbutton", { name: /Throughput History \(days\)/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("spinbutton", { name: /Throughput History \(days\)/i }),
		).toHaveValue(30);
	});

	it("should show date pickers when fixed dates is checked", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-31");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ useFixedDates: true, startDate, endDate })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		expect(
			screen.queryByRole("spinbutton", { name: /Throughput History/i }),
		).not.toBeInTheDocument();

		const startInput = screen.getByLabelText(/Start Date/i) as HTMLInputElement;
		const endInput = screen.getByLabelText(/End Date/i) as HTMLInputElement;

		expect(startInput).toBeInTheDocument();
		expect(endInput).toBeInTheDocument();
		expect(startInput.value).toBe("2024-01-01");
		expect(endInput.value).toBe("2024-01-31");
	});

	it("should validate start date is at least 10 days before end date", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-20");
		const endDate = new Date("2024-01-25");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ useFixedDates: true, startDate, endDate })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const startInput = screen.getByLabelText(/Start Date/i);
		await user.clear(startInput);
		await user.type(startInput, "2024-01-20");

		const endInput = screen.getByLabelText(/End Date/i);
		await user.clear(endInput);
		await user.type(endInput, "2024-01-25");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(screen.getByText(/at least 10 days/i)).toBeInTheDocument();
		});
	});

	it("should validate end date is not in the future", async () => {
		const user = userEvent.setup();
		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		const startDate = new Date("2024-01-01");

		render(
			<ThroughputQuickSetting
				{...getMockProps({
					useFixedDates: true,
					startDate,
					endDate: new Date(),
				})}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const endInput = screen.getByLabelText(/End Date/i);
		await user.clear(endInput);
		await user.type(endInput, tomorrow.toISOString().split("T")[0]);

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(screen.getByText(/cannot be in the future/i)).toBeInTheDocument();
		});
	});

	it("should reject negative rolling history and not save", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ startDate, endDate, onSave: mockOnSave })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const historyInput = screen.getByRole("spinbutton", {
			name: /Throughput History/i,
		});
		fireEvent.change(historyInput, { target: { value: "-5" } });

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(
				screen.getByText(/history must be at least 1 day/i),
			).toBeInTheDocument();
		});
		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should reject missing fixed dates and not save", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<ThroughputQuickSetting
				{...getMockProps({ useFixedDates: true, onSave: mockOnSave })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(
				screen.getByText(/Start and end dates are required/i),
			).toBeInTheDocument();
		});
		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should call onSave with updated rolling history when Enter is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<ThroughputQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const historyInput = screen.getByRole("spinbutton", {
			name: /Throughput History/i,
		});
		await user.clear(historyInput);
		await user.type(historyInput, "45");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(false, 45, null, null);
		});
	});

	it("should call onSave with fixed dates when Enter is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-31"); // 31 days initially
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					useFixedDates: true,
					startDate,
					endDate,
					onSave: mockOnSave,
				})}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const startInput = screen.getByLabelText(/Start Date/i);
		await user.clear(startInput);
		await user.type(startInput, "2024-02-01");

		const endInput = screen.getByLabelText(/End Date/i);
		await user.clear(endInput);
		await user.type(endInput, "2024-02-15");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			// Component passes initialThroughputHistory when useFixedDates is true
			// initialThroughputHistory is calculated from initial dates (Jan 1-31 = 31 days)
			expect(mockOnSave).toHaveBeenCalledWith(
				true,
				31,
				expect.any(Date),
				expect.any(Date),
			);
		});
	});

	it("should not call onSave when Esc is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<ThroughputQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const historyInput = screen.getByRole("spinbutton", {
			name: /Throughput History/i,
		});
		await user.clear(historyInput);
		await user.type(historyInput, "60");

		await user.keyboard("{Escape}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should be disabled when disabled prop is true", () => {
		render(<ThroughputQuickSetting {...getMockProps({ disabled: true })} />);

		const button = screen.getByRole("button", { name: /Throughput/i });
		expect(button).toBeDisabled();
	});

	it("should toggle between rolling and fixed dates mode", async () => {
		const user = userEvent.setup();
		render(<ThroughputQuickSetting {...getMockProps()} />);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		// Initially rolling history input should be visible
		expect(
			screen.getByRole("spinbutton", { name: /Throughput History/i }),
		).toBeInTheDocument();

		// Click the checkbox to switch to fixed dates
		const checkbox = screen.getByRole("checkbox", { name: /Use Fixed Dates/i });
		await user.click(checkbox);

		// Now date pickers should be visible
		await waitFor(() => {
			expect(
				screen.queryByRole("spinbutton", { name: /Throughput History/i }),
			).not.toBeInTheDocument();
			expect(screen.getByLabelText(/Start Date/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/End Date/i)).toBeInTheDocument();
		});
	});

	it("should allow unsetting by setting rolling history to 0", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30"); // 30 days initially
		render(
			<ThroughputQuickSetting
				{...getMockProps({ startDate, endDate, onSave: mockOnSave })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		const historyInput = screen.getByRole("spinbutton", {
			name: /Throughput History/i,
		});
		await user.clear(historyInput);
		await user.type(historyInput, "0");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(false, 0, null, null);
		});
	});

	it("should not call onSave when value is unchanged", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<ThroughputQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		await user.keyboard("{Enter}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should show blackout overlap indicator in tooltip when hasBlackoutOverlap is true", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					startDate,
					endDate,
					hasBlackoutOverlap: true,
				})}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /Blackout days within window/i,
			}),
		).toBeInTheDocument();
	});

	it("should not show blackout indicator when hasBlackoutOverlap is false", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ startDate, endDate, hasBlackoutOverlap: false })}
			/>,
		);

		expect(
			screen.getByRole("button", { name: /Rolling 30 days/i }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /Blackout/i }),
		).not.toBeInTheDocument();
	});

	it("should show blackout indicator in tooltip for fixed dates", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-31");
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					useFixedDates: true,
					startDate,
					endDate,
					hasBlackoutOverlap: true,
				})}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /Fixed dates.*Blackout days within window/i,
			}),
		).toBeInTheDocument();
	});

	it("shows forecast-filter indicator in tooltip when hasForecastFilter is true", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					startDate,
					endDate,
					hasForecastFilter: true,
				})}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /Forecast filter active/i,
			}),
		).toBeInTheDocument();
	});

	it("does not surface forecast-filter indicator when hasForecastFilter is false", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ startDate, endDate, hasForecastFilter: false })}
			/>,
		);

		expect(
			screen.queryByRole("button", { name: /Forecast filter active/i }),
		).not.toBeInTheDocument();
	});

	it("combines blackout warning and forecast-filter info suffixes in the tooltip", () => {
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					startDate,
					endDate,
					hasBlackoutOverlap: true,
					hasForecastFilter: true,
				})}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /Blackout days within window.*Forecast filter active/i,
			}),
		).toBeInTheDocument();
	});

	it("suppresses the blackout qualifier on the Not-set branch even when overlapping", () => {
		render(
			<ThroughputQuickSetting
				{...getMockProps({ hasBlackoutOverlap: true })}
			/>,
		);

		expect(
			screen.getByRole("button", { name: /Not set/i }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /Blackout days within window/i }),
		).not.toBeInTheDocument();
	});

	it("renders the base label alone with no list when there are no qualifiers", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting {...getMockProps({ startDate, endDate })} />,
		);

		await user.hover(screen.getByRole("button", { name: /Throughput/i }));

		await waitFor(() => {
			expect(screen.getByText(/Rolling 30 days/i)).toBeInTheDocument();
		});
		expect(screen.queryByRole("listitem")).not.toBeInTheDocument();
	});

	it("switches back to the rolling history input when fixed dates is unchecked", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-31");
		render(
			<ThroughputQuickSetting
				{...getMockProps({ useFixedDates: true, startDate, endDate })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Throughput/i }));

		expect(screen.getByLabelText(/Start Date/i)).toBeInTheDocument();

		await user.click(
			screen.getByRole("checkbox", { name: /Use Fixed Dates/i }),
		);

		await waitFor(() => {
			expect(
				screen.getByRole("spinbutton", { name: /Throughput History/i }),
			).toBeInTheDocument();
			expect(screen.queryByLabelText(/Start Date/i)).not.toBeInTheDocument();
		});
	});

	it("renders each active qualifier as a distinct list item in the tooltip", async () => {
		const user = userEvent.setup();
		const startDate = new Date("2024-01-01");
		const endDate = new Date("2024-01-30");
		render(
			<ThroughputQuickSetting
				{...getMockProps({
					startDate,
					endDate,
					hasBlackoutOverlap: true,
					hasForecastFilter: true,
				})}
			/>,
		);

		await user.hover(screen.getByRole("button", { name: /Throughput/i }));

		const qualifiers = await screen.findAllByRole("listitem");
		const qualifierTexts = qualifiers.map((item) => item.textContent);

		expect(qualifierTexts).toEqual([
			"Blackout days within window — excluded from forecast",
			"Forecast filter active — some throughput items excluded",
		]);
	});
});

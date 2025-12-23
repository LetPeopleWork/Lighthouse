import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import SleQuickSetting from "./SleQuickSetting";

describe("SleQuickSetting", () => {
	const getMockProps = (overrides?: {
		probability?: number;
		range?: number;
		onSave?: (probability: number, range: number) => Promise<void>;
		disabled?: boolean;
	}) => ({
		probability: overrides?.probability ?? 85,
		range: overrides?.range ?? 10,
		onSave: overrides?.onSave ?? vi.fn().mockResolvedValue(undefined),
		disabled: overrides?.disabled ?? false,
	});

	it("should render icon button with tooltip showing current SLE", async () => {
		const user = userEvent.setup();
		render(<SleQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", {
			name: "Service Level Expectation",
		});
		expect(button).toBeInTheDocument();

		await user.hover(button);
		await waitFor(() => {
			expect(
				screen.getByText(/Service Level Expectation: 85% .* within 10 days/i),
			).toBeInTheDocument();
		});
	});

	it("should show greyed icon when SLE is unset (probability or range is 0)", () => {
		render(<SleQuickSetting {...getMockProps({ probability: 0, range: 0 })} />);

		const button = screen.getByRole("button", {
			name: "Service Level Expectation",
		});
		expect(button).toBeInTheDocument();
	});

	it("should open editor dialog when icon is clicked", async () => {
		const user = userEvent.setup();
		render(<SleQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", {
			name: "Service Level Expectation",
		});
		await user.click(button);

		expect(
			screen.getByRole("heading", { name: /Service Level Expectation/i }),
		).toBeInTheDocument();
		expect(screen.getByLabelText(/Probability/i)).toBeInTheDocument();
		expect(screen.getByLabelText(/Range/i)).toBeInTheDocument();
	});

	it("should validate probability is between 50 and 95", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const probabilityInput = screen.getByLabelText(/Probability/i);

		await user.clear(probabilityInput);
		await user.type(probabilityInput, "49");
		await user.keyboard("{Enter}");

		expect(onSave).not.toHaveBeenCalled();
		expect(screen.getByText(/Must be between 50 and 95/i)).toBeInTheDocument();
	});

	it("should validate range is at least 1", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const rangeInput = screen.getByLabelText(/Range/i);

		await user.clear(rangeInput);
		await user.type(rangeInput, "0");
		await user.keyboard("{Enter}");

		expect(onSave).not.toHaveBeenCalled();
		expect(screen.getByText(/Must be at least 1/i)).toBeInTheDocument();
	});

	it("should call onSave with updated values when Enter is pressed", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ probability: 70, onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const probabilityInput = screen.getByLabelText(/Probability/i);
		await user.clear(probabilityInput);
		await user.type(probabilityInput, "90");
		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(90, 10);
		});
	});

	it("should call onSave when Enter is pressed with valid changes", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ range: 5, onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const rangeInput = screen.getByLabelText(/Range/i);
		await user.clear(rangeInput);
		await user.type(rangeInput, "15");
		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(85, 15);
		});
	});

	it("should not call onSave when Esc is pressed", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const probabilityInput = screen.getByLabelText(/Probability/i);
		await user.clear(probabilityInput);
		await user.type(probabilityInput, "90");
		await user.keyboard("{Escape}");

		expect(onSave).not.toHaveBeenCalled();
	});

	it("should be disabled when disabled prop is true", () => {
		render(<SleQuickSetting {...getMockProps({ disabled: true })} />);

		const button = screen.getByRole("button", {
			name: "Service Level Expectation",
		});
		expect(button).toBeDisabled();
	});

	it("should allow unsetting SLE by setting probability to 0", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SleQuickSetting {...getMockProps({ onSave })} />);

		await user.click(
			screen.getByRole("button", { name: "Service Level Expectation" }),
		);

		const probabilityInput = screen.getByLabelText(/Probability/i);
		const rangeInput = screen.getByLabelText(/Range/i);

		await user.clear(probabilityInput);
		await user.type(probabilityInput, "0");
		await user.clear(rangeInput);
		await user.type(rangeInput, "0");
		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(0, 0);
		});
	});
});

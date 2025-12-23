import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import FeatureWipQuickSetting from "./FeatureWipQuickSetting";

const getMockProps = (
	overrides?: Partial<{
		featureWip: number;
		onSave: (featureWip: number) => Promise<void>;
		disabled: boolean;
		itemTypeKey?: string;
	}>,
) => ({
	featureWip: 1,
	onSave: vi.fn().mockResolvedValue(undefined),
	disabled: false,
	...overrides,
});

describe("FeatureWipQuickSetting", () => {
	it("should render icon button with tooltip showing current Feature WIP", () => {
		render(<FeatureWipQuickSetting {...getMockProps()} />);

		expect(
			screen.getByRole("button", { name: /Feature WIP: 1 Feature/i }),
		).toBeInTheDocument();
	});

	it("should show greyed icon when Feature WIP is unset (0)", () => {
		render(<FeatureWipQuickSetting {...getMockProps({ featureWip: 0 })} />);

		const button = screen.getByRole("button", {
			name: /Feature WIP: Not set/i,
		});
		expect(button).toBeInTheDocument();
	});

	it("should open editor dialog when icon is clicked", async () => {
		const user = userEvent.setup();
		render(<FeatureWipQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", { name: /Feature WIP/i });
		await user.click(button);

		expect(
			screen.getByRole("heading", { name: /Feature WIP Limit/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("spinbutton", { name: /Feature WIP/i }),
		).toBeInTheDocument();
	});

	it("should display current Feature WIP value in dialog", async () => {
		const user = userEvent.setup();
		render(<FeatureWipQuickSetting {...getMockProps({ featureWip: 3 })} />);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const wipInput = screen.getByRole("spinbutton", { name: /Feature WIP/i });
		expect(wipInput).toHaveValue(3);
	});

	it("should allow 0 for unset state", async () => {
		const user = userEvent.setup();
		render(<FeatureWipQuickSetting {...getMockProps()} />);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const wipInput = screen.getByRole("spinbutton", { name: /Feature WIP/i });
		// HTML5 number input with min=0 prevents negative values
		expect(wipInput).toHaveAttribute("min", "0");
	});

	it("should call onSave with updated value when Enter is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<FeatureWipQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const wipInput = screen.getByRole("spinbutton", { name: /Feature WIP/i });
		await user.clear(wipInput);
		await user.type(wipInput, "5");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(5);
		});
	});

	it("should not call onSave when Esc is pressed", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<FeatureWipQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const wipInput = screen.getByRole("spinbutton", { name: /Feature WIP/i });
		await user.clear(wipInput);
		await user.type(wipInput, "10");

		await user.keyboard("{Escape}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});

	it("should be disabled when disabled prop is true", () => {
		render(<FeatureWipQuickSetting {...getMockProps({ disabled: true })} />);

		const button = screen.getByRole("button", { name: /Feature WIP/i });
		expect(button).toBeDisabled();
	});

	it("should allow unsetting Feature WIP by setting to 0", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<FeatureWipQuickSetting
				{...getMockProps({ featureWip: 3, onSave: mockOnSave })}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		const wipInput = screen.getByRole("spinbutton", { name: /Feature WIP/i });
		await user.clear(wipInput);
		await user.type(wipInput, "0");

		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(mockOnSave).toHaveBeenCalledWith(0);
		});
	});

	it("should not call onSave when value is unchanged", async () => {
		const user = userEvent.setup();
		const mockOnSave = vi.fn().mockResolvedValue(undefined);
		render(
			<FeatureWipQuickSetting {...getMockProps({ onSave: mockOnSave })} />,
		);

		await user.click(screen.getByRole("button", { name: /Feature WIP/i }));

		await user.keyboard("{Enter}");

		expect(mockOnSave).not.toHaveBeenCalled();
	});
});

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import SystemWipQuickSetting from "./SystemWipQuickSetting";

describe("SystemWipQuickSetting", () => {
	const getMockProps = (overrides?: {
		wipLimit?: number;
		onSave?: (wipLimit: number) => Promise<void>;
		disabled?: boolean;
	}) => ({
		wipLimit: overrides?.wipLimit ?? 5,
		onSave: overrides?.onSave ?? vi.fn().mockResolvedValue(undefined),
		disabled: overrides?.disabled ?? false,
	});

	it("should render icon button with tooltip showing current WIP limit", async () => {
		const user = userEvent.setup();
		render(<SystemWipQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", { name: "System WIP Limit" });
		expect(button).toBeInTheDocument();

		await user.hover(button);
		await waitFor(() => {
			expect(screen.getByText(/System WIP Limit: 5/i)).toBeInTheDocument();
		});
	});

	it("should show greyed icon when WIP limit is unset (0)", () => {
		render(<SystemWipQuickSetting {...getMockProps({ wipLimit: 0 })} />);

		const button = screen.getByRole("button", { name: "System WIP Limit" });
		expect(button).toBeInTheDocument();
	});

	it("should open editor dialog when icon is clicked", async () => {
		const user = userEvent.setup();
		render(<SystemWipQuickSetting {...getMockProps()} />);

		const button = screen.getByRole("button", { name: "System WIP Limit" });
		await user.click(button);

		expect(
			screen.getByRole("heading", { name: /System WIP Limit/i }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("spinbutton", { name: /WIP Limit/i }),
		).toBeInTheDocument();
	});

	it("should validate WIP limit is at least 1 when not unsetting", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SystemWipQuickSetting {...getMockProps({ onSave })} />);

		await user.click(screen.getByRole("button", { name: "System WIP Limit" }));

		const wipInput = screen.getByRole("spinbutton", { name: /WIP Limit/i });

		await user.clear(wipInput);
		await user.type(wipInput, "0");
		await user.keyboard("{Enter}");

		// Allow 0 for unset
		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(0);
		});
	});

	it("should call onSave with updated value when Enter is pressed", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(
			<SystemWipQuickSetting {...getMockProps({ wipLimit: 5, onSave })} />,
		);

		await user.click(screen.getByRole("button", { name: "System WIP Limit" }));

		const wipInput = screen.getByRole("spinbutton", { name: /WIP Limit/i });
		await user.clear(wipInput);
		await user.type(wipInput, "10");
		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(10);
		});
	});

	it("should not call onSave when Esc is pressed", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(<SystemWipQuickSetting {...getMockProps({ onSave })} />);

		await user.click(screen.getByRole("button", { name: "System WIP Limit" }));

		const wipInput = screen.getByRole("spinbutton", { name: /WIP Limit/i });
		await user.clear(wipInput);
		await user.type(wipInput, "15");
		await user.keyboard("{Escape}");

		expect(onSave).not.toHaveBeenCalled();
	});

	it("should be disabled when disabled prop is true", () => {
		render(<SystemWipQuickSetting {...getMockProps({ disabled: true })} />);

		const button = screen.getByRole("button", { name: "System WIP Limit" });
		expect(button).toBeDisabled();
	});

	it("should allow unsetting WIP limit by setting to 0", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(
			<SystemWipQuickSetting {...getMockProps({ wipLimit: 5, onSave })} />,
		);

		await user.click(screen.getByRole("button", { name: "System WIP Limit" }));

		const wipInput = screen.getByRole("spinbutton", { name: /WIP Limit/i });
		await user.clear(wipInput);
		await user.type(wipInput, "0");
		await user.keyboard("{Enter}");

		await waitFor(() => {
			expect(onSave).toHaveBeenCalledWith(0);
		});
	});

	it("should not call onSave when value is unchanged", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn().mockResolvedValue(undefined);
		render(
			<SystemWipQuickSetting {...getMockProps({ wipLimit: 5, onSave })} />,
		);

		await user.click(screen.getByRole("button", { name: "System WIP Limit" }));
		await user.keyboard("{Enter}");

		expect(onSave).not.toHaveBeenCalled();
	});
});

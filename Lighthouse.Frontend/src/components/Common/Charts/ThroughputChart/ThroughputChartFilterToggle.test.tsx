import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ThroughputChartFilterToggle, {
	type ThroughputChartFilterToggleProps,
} from "./ThroughputChartFilterToggle";

function getToggleProps(
	overrides: Partial<ThroughputChartFilterToggleProps> = {},
): ThroughputChartFilterToggleProps {
	return {
		isPremium: true,
		hasFilter: true,
		onChange: vi.fn(),
		...overrides,
	};
}

describe("ThroughputChartFilterToggle", () => {
	it("renders the Switch only when the team has a non-empty filter on a premium tenant", () => {
		const { rerender } = render(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: false, hasFilter: true })}
			/>,
		);
		expect(
			screen.queryByLabelText(/use filtered throughput/i),
		).not.toBeInTheDocument();

		rerender(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: true, hasFilter: false })}
			/>,
		);
		expect(
			screen.queryByLabelText(/use filtered throughput/i),
		).not.toBeInTheDocument();

		rerender(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: true, hasFilter: true })}
			/>,
		);
		expect(
			screen.getByLabelText(/use filtered throughput/i),
		).toBeInTheDocument();
	});

	it("defaults the Switch to unchecked so today's raw-throughput behaviour is preserved", () => {
		render(<ThroughputChartFilterToggle {...getToggleProps()} />);
		const toggle = screen.getByLabelText(/use filtered throughput/i);
		expect(toggle).not.toBeChecked();
	});

	it("emits onChange(true) the first time the user flips the Switch on", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		render(<ThroughputChartFilterToggle {...getToggleProps({ onChange })} />);

		await user.click(screen.getByLabelText(/use filtered throughput/i));

		expect(onChange).toHaveBeenCalledExactlyOnceWith(true);
	});

	it("emits onChange(false) when the user flips the Switch back off", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		render(<ThroughputChartFilterToggle {...getToggleProps({ onChange })} />);

		const toggle = screen.getByLabelText(/use filtered throughput/i);
		await user.click(toggle);
		await user.click(toggle);

		expect(onChange).toHaveBeenNthCalledWith(2, false);
	});
});

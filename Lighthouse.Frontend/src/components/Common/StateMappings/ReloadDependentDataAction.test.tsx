import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ReloadDependentDataAction from "./ReloadDependentDataAction";

describe("ReloadDependentDataAction", () => {
	it("renders the one-click reload action when visible", () => {
		render(
			<ReloadDependentDataAction
				visible={true}
				label="Reload throughput now"
				onReload={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Reload throughput now" }),
		).toBeInTheDocument();
	});

	it("renders nothing when not visible", () => {
		render(
			<ReloadDependentDataAction
				visible={false}
				label="Reload throughput now"
				onReload={vi.fn()}
			/>,
		);

		expect(
			screen.queryByRole("button", { name: "Reload throughput now" }),
		).not.toBeInTheDocument();
	});

	it("invokes onReload exactly once per click and never automatically", () => {
		const onReload = vi.fn();

		render(
			<ReloadDependentDataAction
				visible={true}
				label="Reload throughput now"
				onReload={onReload}
			/>,
		);

		expect(onReload).not.toHaveBeenCalled();

		fireEvent.click(
			screen.getByRole("button", { name: "Reload throughput now" }),
		);

		expect(onReload).toHaveBeenCalledTimes(1);
	});
});

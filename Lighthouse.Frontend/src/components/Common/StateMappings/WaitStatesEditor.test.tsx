import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import WaitStatesEditor from "./WaitStatesEditor";

describe("WaitStatesEditor", () => {
	const doingStates = ["Active", "In Review"];
	const stateMappings: IStateMapping[] = [
		{ name: "Waiting for Review", states: ["In Review"] },
		{ name: "", states: [] },
	];

	const openControl = async () => {
		await userEvent.click(
			screen.getByRole("checkbox", { name: /configure wait states/i }),
		);
	};

	it("keeps the add/remove control hidden until the toggle is enabled", async () => {
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={vi.fn()}
			/>,
		);

		expect(
			screen.queryByRole("combobox", { name: /wait state/i }),
		).not.toBeInTheDocument();

		await openControl();

		expect(
			screen.getByRole("combobox", { name: /wait state/i }),
		).toBeInTheDocument();
	});

	it("explains that wait states are idle and how they drive the efficiency formula", async () => {
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={vi.fn()}
			/>,
		);

		await openControl();

		expect(screen.getByText(/idle/i)).toBeInTheDocument();
		expect(screen.getByText(/not active/i)).toBeInTheDocument();
		expect(screen.getByText(/efficiency/i)).toBeInTheDocument();
	});

	it("suggests both raw Doing states and non-empty mapping names", async () => {
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={vi.fn()}
			/>,
		);

		await openControl();

		const combobox = screen.getByRole("combobox", { name: /wait state/i });
		await userEvent.click(combobox);

		const options = screen.getByRole("listbox");
		expect(within(options).getByText("Active")).toBeInTheDocument();
		expect(within(options).getByText("In Review")).toBeInTheDocument();
		expect(within(options).getByText("Waiting for Review")).toBeInTheDocument();
		expect(within(options).getAllByRole("option")).toHaveLength(3);
	});

	it("clears the wait states when the toggle is switched off", async () => {
		const onChange = vi.fn();
		render(
			<WaitStatesEditor
				waitStates={["Active"]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={onChange}
			/>,
		);

		await openControl();

		expect(onChange).toHaveBeenCalledExactlyOnceWith([]);
	});

	it("adds a selected mapping name as a single wait-state chip in one click", async () => {
		const onChange = vi.fn();
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={onChange}
			/>,
		);

		await openControl();
		await userEvent.click(
			screen.getByRole("combobox", { name: /wait state/i }),
		);
		await userEvent.click(screen.getByText("Waiting for Review"));

		expect(onChange).toHaveBeenCalledExactlyOnceWith(["Waiting for Review"]);
	});

	it("trims a typed wait state before adding it", async () => {
		const onChange = vi.fn();
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={onChange}
			/>,
		);

		await openControl();
		const combobox = screen.getByRole("combobox", { name: /wait state/i });
		await userEvent.type(combobox, "  Blocked  {Enter}");

		expect(onChange).toHaveBeenCalledExactlyOnceWith(["Blocked"]);
	});

	it("ignores a whitespace-only wait state", async () => {
		const onChange = vi.fn();
		render(
			<WaitStatesEditor
				waitStates={[]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={onChange}
			/>,
		);

		await openControl();
		const combobox = screen.getByRole("combobox", { name: /wait state/i });
		await userEvent.type(combobox, "   {Enter}");

		expect(onChange).not.toHaveBeenCalled();
	});

	it("removes a wait-state chip when its delete affordance is used", async () => {
		const onChange = vi.fn();
		render(
			<WaitStatesEditor
				waitStates={["Active", "Waiting for Review"]}
				doingStates={doingStates}
				stateMappings={stateMappings}
				onChange={onChange}
			/>,
		);

		const chip = screen.getByText("Active").closest(".MuiChip-root");
		const deleteIcon = chip?.querySelector(".MuiChip-deleteIcon");
		if (!deleteIcon) throw new Error("delete affordance missing");
		await userEvent.click(deleteIcon);

		expect(onChange).toHaveBeenCalledExactlyOnceWith(["Waiting for Review"]);
	});
});

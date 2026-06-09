import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import CumulativeStateTimeScopeControl from "./CumulativeStateTimeScopeControl";

const getMockDefinition = (
	overrides?: Partial<INamedCycleTimeDefinition>,
): INamedCycleTimeDefinition => ({
	id: 1,
	name: "Lead Time",
	isValid: true,
	...overrides,
});

async function openSelector(user: ReturnType<typeof userEvent.setup>) {
	await user.click(screen.getByRole("combobox", { name: /cycle time scope/i }));
	return screen.getByRole("listbox");
}

describe("CumulativeStateTimeScopeControl", () => {
	it("renders nothing when no named cycle times are configured", () => {
		const { container } = render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[]}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});

	it("defaults to the unscoped Default option when nothing is scoped", () => {
		render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[getMockDefinition()]}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("combobox", { name: /cycle time scope/i }),
		).toHaveTextContent("Default");
	});

	it("scopes to the chosen named cycle time", async () => {
		const user = userEvent.setup();
		const onScopeChange = vi.fn();
		render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[
					getMockDefinition({ id: 4, name: "Analyze to Done" }),
				]}
				scopeDefinitionId={null}
				onScopeChange={onScopeChange}
			/>,
		);

		const listbox = await openSelector(user);
		await user.click(within(listbox).getByText("Analyze to Done"));

		expect(onScopeChange).toHaveBeenCalledWith(4);
	});

	it("returns to Default when the Default option is chosen", async () => {
		const user = userEvent.setup();
		const onScopeChange = vi.fn();
		render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[getMockDefinition({ id: 4 })]}
				scopeDefinitionId={4}
				onScopeChange={onScopeChange}
			/>,
		);

		const listbox = await openSelector(user);
		await user.click(within(listbox).getByText("Default"));

		expect(onScopeChange).toHaveBeenCalledWith(null);
	});

	it("disables an invalid definition in the selector", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[
					getMockDefinition({ id: 1, name: "Lead Time", isValid: true }),
					getMockDefinition({ id: 2, name: "Broken", isValid: false }),
				]}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		const listbox = await openSelector(user);
		const brokenOption = within(listbox).getByText(/Broken \(invalid/i);

		expect(brokenOption.closest('[role="option"]')).toHaveAttribute(
			"aria-disabled",
			"true",
		);
	});

	it("resets a scope that points at a now-invalid definition", () => {
		const onScopeChange = vi.fn();
		render(
			<CumulativeStateTimeScopeControl
				namedCycleTimeDefinitions={[
					getMockDefinition({ id: 2, name: "Broken", isValid: false }),
				]}
				scopeDefinitionId={2}
				onScopeChange={onScopeChange}
			/>,
		);

		expect(onScopeChange).toHaveBeenCalledWith(null);
	});
});

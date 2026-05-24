import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type {
	EvaluableWorkItem,
	EvaluatorCondition,
} from "./evaluateCondition";
import ThroughputChartFilterToggle, {
	type ThroughputChartFilterToggleProps,
} from "./ThroughputChartFilterToggle";

const sampleItems: readonly EvaluableWorkItem[] = [
	{
		type: "Bug",
		state: "Done",
		name: "Login crash",
		referenceId: "BUG-1",
		parentReferenceId: "",
		tags: ["maintenance"],
		additionalFieldValues: {},
	},
	{
		type: "UserStory",
		state: "Done",
		name: "Add export",
		referenceId: "US-2",
		parentReferenceId: "EPIC-9",
		tags: [],
		additionalFieldValues: {},
	},
	{
		type: "Bug",
		state: "Done",
		name: "Logout flicker",
		referenceId: "BUG-3",
		parentReferenceId: "",
		tags: ["maintenance"],
		additionalFieldValues: {},
	},
];

const typeIsBugCondition: EvaluatorCondition = {
	fieldKey: "workitem.type",
	operator: "equals",
	value: "Bug",
};

const excludeEverythingCondition: EvaluatorCondition = {
	fieldKey: "workitem.type",
	operator: "equals",
	value: "DoesNotExistAnywhere",
};

function getToggleProps(
	overrides: Partial<ThroughputChartFilterToggleProps> = {},
): ThroughputChartFilterToggleProps {
	return {
		isPremium: true,
		hasFilter: true,
		chartKind: "runChart",
		conditions: [typeIsBugCondition],
		items: sampleItems,
		onClientFiltered: vi.fn(),
		onServerViewChange: vi.fn(),
		...overrides,
	};
}

describe("ThroughputChartFilterToggle", () => {
	it("renders the Show: Raw or Filtered toggle only when the team has a non-empty filter on a premium tenant", () => {
		const { rerender } = render(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: false, hasFilter: true })}
			/>,
		);
		expect(
			screen.queryByRole("group", { name: /throughput filter view/i }),
		).not.toBeInTheDocument();

		rerender(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: true, hasFilter: false })}
			/>,
		);
		expect(
			screen.queryByRole("group", { name: /throughput filter view/i }),
		).not.toBeInTheDocument();

		rerender(
			<ThroughputChartFilterToggle
				{...getToggleProps({ isPremium: true, hasFilter: true })}
			/>,
		);
		expect(
			screen.getByRole("group", { name: /throughput filter view/i }),
		).toBeInTheDocument();
	});

	it("defaults the toggle to Raw on every render to preserve today's chart behaviour", () => {
		const { rerender } = render(
			<ThroughputChartFilterToggle {...getToggleProps()} />,
		);
		expect(
			screen.getByRole("button", { name: /^raw$/i, pressed: true }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /^filtered$/i, pressed: false }),
		).toBeInTheDocument();

		rerender(
			<ThroughputChartFilterToggle
				{...getToggleProps({ items: sampleItems.slice(0, 1) })}
			/>,
		);
		expect(
			screen.getByRole("button", { name: /^raw$/i, pressed: true }),
		).toBeInTheDocument();
	});

	it("flipping the toggle to Filtered re-renders the chart client-side without a network round-trip on the Run Chart", async () => {
		const user = userEvent.setup();
		const onClientFiltered = vi.fn();
		const onServerViewChange = vi.fn();

		render(
			<ThroughputChartFilterToggle
				{...getToggleProps({
					chartKind: "runChart",
					onClientFiltered,
					onServerViewChange,
				})}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /^filtered$/i }));

		expect(onClientFiltered).toHaveBeenCalledTimes(1);
		const [filteredItems, total] = onClientFiltered.mock.calls[0];
		expect(filteredItems).toHaveLength(2);
		expect(total).toBe(3);
		expect(onServerViewChange).not.toHaveBeenCalled();
	});

	it("flipping the toggle to Filtered on PBC issues a request with ?view=filtered", async () => {
		const user = userEvent.setup();
		const onClientFiltered = vi.fn();
		const onServerViewChange = vi.fn();

		render(
			<ThroughputChartFilterToggle
				{...getToggleProps({
					chartKind: "pbc",
					onClientFiltered,
					onServerViewChange,
				})}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /^filtered$/i }));

		expect(onServerViewChange).toHaveBeenCalledWith("filtered");
		expect(onClientFiltered).not.toHaveBeenCalled();
	});

	it("shows the empty-state message when the filter excludes every item in the window (Filtered view)", async () => {
		const user = userEvent.setup();
		render(
			<ThroughputChartFilterToggle
				{...getToggleProps({
					chartKind: "runChart",
					conditions: [excludeEverythingCondition],
				})}
			/>,
		);

		await user.click(screen.getByRole("button", { name: /^filtered$/i }));

		expect(
			screen.getByText(/no items match the throughput filter in this window/i),
		).toBeInTheDocument();
	});
});

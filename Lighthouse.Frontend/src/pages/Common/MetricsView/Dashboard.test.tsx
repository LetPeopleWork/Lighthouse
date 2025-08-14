import { render, screen } from "@testing-library/react";
import { describe, expect, test } from "vitest";

import Dashboard from "./Dashboard";

describe("Dashboard", () => {
	test("renders only non-null items", () => {
		render(
			<Dashboard
				items={[
					{ id: "a", node: <div>Item A</div> },
					{ id: "b", node: null },
					{ id: "c", node: <div>Item C</div> },
				]}
			/>,
		);

		expect(screen.getByText("Item A")).toBeInTheDocument();
		expect(screen.getByText("Item C")).toBeInTheDocument();
		expect(screen.queryByText("Item B")).toBeNull();
	});

	test("applies default width/height for variants and accepts custom width", () => {
		render(
			<Dashboard
				items={[
					{ id: "small", node: <div>Small</div>, colSpan: 3 },
					{ id: "large", node: <div>Large</div>, colSpan: 6 },
					{ id: "custom", node: <div>Custom</div>, colSpan: 6, rowSpan: 3 },
				]}
			/>,
		);

		// find all item containers by presence of data-colspan attribute
		const itemContainers = Array.from(
			document.querySelectorAll("[data-colspan]"),
		);
		expect(itemContainers).toHaveLength(3);

		const small = itemContainers[0];
		const large = itemContainers[1];
		const custom = itemContainers[2];

		// small uses colSpan 3, large 6
		expect(small.getAttribute("data-colspan")).toBe("3");
		expect(large.getAttribute("data-colspan")).toBe("6");

		// custom rowSpan is honored
		expect(custom.getAttribute("data-rowspan")).toBe("3");
	});

	test("preserves item order and renders items in the same sequence as items array", () => {
		render(
			<Dashboard
				items={[
					{ id: "first", node: <div>First</div>, colSpan: 3 },
					{ id: "second", node: <div>Second</div>, colSpan: 3 },
					{ id: "third", node: <div>Third</div>, colSpan: 3 },
				]}
			/>,
		);

		const itemContainers = Array.from(
			document.querySelectorAll("[data-colspan]"),
		);
		expect(itemContainers).toHaveLength(3);

		expect(itemContainers[0]).toHaveTextContent("First");
		expect(itemContainers[1]).toHaveTextContent("Second");
		expect(itemContainers[2]).toHaveTextContent("Third");
	});
});

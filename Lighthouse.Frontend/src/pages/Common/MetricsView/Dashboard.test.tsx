import { render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";

// Mock Grid from MUI so we can inspect the props passed to it.
vi.mock("@mui/material", () => {
	return {
		Grid: ({
			children,
			...props
		}: React.PropsWithChildren<Record<string, unknown>>) => (
			<div data-testid="grid" data-props={JSON.stringify(props)}>
				{children}
			</div>
		),
	};
});

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

	test("applies default sizes for variants and accepts custom size", () => {
		render(
			<Dashboard
				items={[
					{ id: "small", node: <div>Small</div>, variant: "small" },
					{ id: "large", node: <div>Large</div>, variant: "large" },
					{ id: "custom", node: <div>Custom</div>, size: { xs: 2, sm: 3 } },
				]}
			/>,
		);

		const allGrids = screen.getAllByTestId("grid");
		// first grid is the container; item grids have a `size` prop in our mock
		const itemGrids = allGrids.filter(
			(g) => JSON.parse(g.getAttribute("data-props") || "{}").size,
		);
		expect(itemGrids).toHaveLength(3);

		const smallProps = JSON.parse(
			itemGrids[0].getAttribute("data-props") || "{}",
		);
		const largeProps = JSON.parse(
			itemGrids[1].getAttribute("data-props") || "{}",
		);
		const customProps = JSON.parse(
			itemGrids[2].getAttribute("data-props") || "{}",
		);

		// defaultSmall: { xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }
		expect(smallProps.size).toEqual({ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 });

		// defaultLarge: { xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }
		expect(largeProps.size).toEqual({ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 });

		expect(customProps.size).toEqual({ xs: 2, sm: 3 });
	});

	test("preserves item order and renders grids in the same sequence as items array", () => {
		render(
			<Dashboard
				items={[
					{ id: "first", node: <div>First</div>, variant: "small" },
					{ id: "second", node: <div>Second</div>, variant: "small" },
					{ id: "third", node: <div>Third</div>, variant: "small" },
				]}
			/>,
		);

		const allGrids = screen.getAllByTestId("grid");
		const itemGrids = allGrids.filter(
			(g) => JSON.parse(g.getAttribute("data-props") || "{}").size,
		);
		expect(itemGrids).toHaveLength(3);

		// Ensure DOM order matches the items order (skip container grid)
		expect(itemGrids[0]).toHaveTextContent("First");
		expect(itemGrids[1]).toHaveTextContent("Second");
		expect(itemGrids[2]).toHaveTextContent("Third");
	});
});

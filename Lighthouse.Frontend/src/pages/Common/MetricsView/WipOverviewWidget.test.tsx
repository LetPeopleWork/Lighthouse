import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import WipOverviewWidget from "./WipOverviewWidget";

describe("WipOverviewWidget", () => {
	it("renders the WIP count", () => {
		render(<WipOverviewWidget wipCount={5} />);
		expect(screen.getByTestId("wip-overview-count")).toHaveTextContent("5");
	});

	it("renders the system WIP limit when defined", () => {
		render(<WipOverviewWidget wipCount={5} systemWipLimit={8} />);
		expect(screen.getByTestId("wip-overview-limit")).toHaveTextContent("8");
	});

	it("does not render the limit when system WIP limit is 0", () => {
		render(<WipOverviewWidget wipCount={5} systemWipLimit={0} />);
		expect(screen.queryByTestId("wip-overview-limit")).not.toBeInTheDocument();
	});

	it("does not render the limit when system WIP limit is undefined", () => {
		render(<WipOverviewWidget wipCount={3} />);
		expect(screen.queryByTestId("wip-overview-limit")).not.toBeInTheDocument();
	});

	it("renders title text", () => {
		render(<WipOverviewWidget wipCount={5} title="Work Items in Progress" />);
		expect(screen.getByText("Work Items in Progress")).toBeInTheDocument();
	});
});

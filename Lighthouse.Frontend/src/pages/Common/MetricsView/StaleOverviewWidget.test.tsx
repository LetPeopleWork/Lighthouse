import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import StaleOverviewWidget from "./StaleOverviewWidget";

describe("StaleOverviewWidget", () => {
	it("renders the stale count", () => {
		render(<StaleOverviewWidget staleCount={3} />);
		expect(screen.getByTestId("stale-overview-count")).toHaveTextContent("3");
	});

	it("renders zero stale count", () => {
		render(<StaleOverviewWidget staleCount={0} />);
		expect(screen.getByTestId("stale-overview-count")).toHaveTextContent("0");
	});

	it("renders the default title", () => {
		render(<StaleOverviewWidget staleCount={2} />);
		expect(screen.getByText("Stale Items")).toBeInTheDocument();
	});
});

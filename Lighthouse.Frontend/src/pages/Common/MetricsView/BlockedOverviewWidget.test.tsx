import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import BlockedOverviewWidget from "./BlockedOverviewWidget";

describe("BlockedOverviewWidget", () => {
	it("renders the blocked count", () => {
		render(<BlockedOverviewWidget blockedCount={3} />);
		expect(screen.getByTestId("blocked-overview-count")).toHaveTextContent("3");
	});

	it("renders zero blocked count", () => {
		render(<BlockedOverviewWidget blockedCount={0} />);
		expect(screen.getByTestId("blocked-overview-count")).toHaveTextContent("0");
	});

	it("renders title text", () => {
		render(<BlockedOverviewWidget blockedCount={2} title="Blocked" />);
		expect(screen.getByText("Blocked")).toBeInTheDocument();
	});
});

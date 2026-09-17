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

	// --- Epic #4127 slice 02 / Story #6017: how much of this is in trouble ---
	//
	// The count alone says how much work is open. It does not say whether any of it is about to
	// break the promise the team published, which is the thing worth interrupting a standup for.
	describe("what is at risk", () => {
		it("says how many of the open items are more likely than not to miss the target", () => {
			render(
				<WipOverviewWidget
					wipCount={9}
					atRisk={{ count: 3, color: "#c62828" }}
				/>,
			);

			expect(screen.getByTestId("wip-overview-at-risk")).toHaveTextContent(
				"3 at risk",
			);
		});

		it("paints it in the colour the risk column paints its worst rows", () => {
			render(
				<WipOverviewWidget
					wipCount={9}
					atRisk={{ count: 3, color: "#c62828" }}
				/>,
			);

			expect(screen.getByTestId("wip-overview-at-risk")).toHaveStyle(
				"color: #c62828",
			);
		});

		it("says nothing at all when none of them are", () => {
			// Not "0 at risk". A line that is always there stops being read, and the absence of
			// trouble is already what the plain count says.
			render(<WipOverviewWidget wipCount={9} atRisk={{ count: 0 }} />);

			expect(
				screen.queryByTestId("wip-overview-at-risk"),
			).not.toBeInTheDocument();
		});

		it("says nothing when the team published no target", () => {
			// Nothing to be at risk of breaking, so the widget reads exactly as it does today.
			render(<WipOverviewWidget wipCount={9} systemWipLimit={8} />);

			expect(
				screen.queryByTestId("wip-overview-at-risk"),
			).not.toBeInTheDocument();
			expect(screen.getByTestId("wip-overview-limit")).toHaveTextContent("8");
		});

		it("keeps the limit and the risk line side by side without either taking the count's place", () => {
			render(
				<WipOverviewWidget
					wipCount={9}
					systemWipLimit={8}
					atRisk={{ count: 2, color: "#c62828" }}
				/>,
			);

			expect(screen.getByTestId("wip-overview-count")).toHaveTextContent("9");
			expect(screen.getByTestId("wip-overview-limit")).toHaveTextContent("8");
			expect(screen.getByTestId("wip-overview-at-risk")).toHaveTextContent(
				"2 at risk",
			);
		});
	});
});

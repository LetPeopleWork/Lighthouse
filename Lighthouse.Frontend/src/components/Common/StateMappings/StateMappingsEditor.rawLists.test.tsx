import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import StateMappingsEditor from "./StateMappingsEditor";

const HELP_TEXT =
	"Combine several Doing states into one named group. The group shows as one chip in your Doing list and in the charts; its states are still read from your tracker. Delete the group to see its states separately again.";

describe("StateMappingsEditor over raw Doing states", () => {
	it("explains that a group is one chip in Doing and the charts, and that its states stay read from the tracker", () => {
		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={["Code Review", "Testing"]}
				onChange={vi.fn()}
			/>,
		);

		expect(screen.getByText(HELP_TEXT)).toBeInTheDocument();
	});

	it("says nothing about a mapping state that is in none of the lists", () => {
		render(
			<StateMappingsEditor
				stateMappings={[{ name: "Review", states: ["Code Review", "Testing"] }]}
				doingStates={["Code Review"]}
				onChange={vi.fn()}
			/>,
		);

		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		expect(screen.queryByText(/not (listed|in)/i)).not.toBeInTheDocument();
		expect(screen.queryByText(/warning/i)).not.toBeInTheDocument();
	});
});

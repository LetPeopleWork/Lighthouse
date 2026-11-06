import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import ParentSourcesList from "./ParentSourcesList";

const ownParent: ParentSourceEntry = { kind: "TrackerParent" };
const field = (id: number): ParentSourceEntry => ({
	kind: "AdditionalField",
	additionalFieldDefinitionId: id,
});

const TheFields: IAdditionalFieldDefinition[] = [
	{ id: 11, displayName: "Is caused by", reference: "customfield_10500" },
	{ id: 12, displayName: "Program Ref", reference: "customfield_10014" },
	{ id: 13, displayName: "Sponsor", reference: "customfield_10015" },
];

const aListOf = (
	value: ParentSourceEntry[],
	owner: "Team" | "Portfolio" = "Team",
) => {
	const onChange = vi.fn();
	render(
		<ParentSourcesList
			owner={owner}
			value={value}
			tracker="Jira"
			additionalFieldDefinitions={TheFields}
			onChange={onChange}
		/>,
	);
	return onChange;
};

const theRows = () =>
	screen.getAllByRole("listitem").map((row) => row.textContent);

describe("the Parent sources list of a Team", () => {
	it("shows the sources in order and moves one up with its button", () => {
		const onChange = aListOf([field(11), field(12)]);

		expect(theRows()[0]).toContain("Is caused by");
		expect(theRows()[1]).toContain("Program Ref");

		fireEvent.click(screen.getByLabelText("Move Program Ref up"));

		expect(onChange).toHaveBeenCalledWith([field(12), field(11)]);
		expect(screen.getByLabelText("Move Is caused by up")).toBeDisabled();
		expect(screen.getByLabelText("Move Program Ref down")).toBeDisabled();
	});

	it("will not let the only source be removed, and says why on its tooltip", async () => {
		const onChange = aListOf([field(11)]);

		const remove = screen.getByLabelText("Remove Is caused by");
		expect(remove).toHaveAttribute("aria-disabled", "true");

		fireEvent.click(remove);
		expect(onChange).not.toHaveBeenCalled();

		fireEvent.mouseOver(remove);
		expect(
			await screen.findByText("A list needs at least one source"),
		).toBeInTheDocument();
	});

	it("lets a keyboard and a screen reader reach why the only source cannot be removed", async () => {
		aListOf([field(11)]);
		const remove = screen.getByRole("button", {
			name: "Remove Is caused by",
		});

		expect(remove).toHaveAccessibleDescription(
			"A list needs at least one source",
		);

		await userEvent.tab();

		expect(remove).toHaveFocus();
	});

	it("describes no reason on a source that can be removed", () => {
		aListOf([field(11), field(12)]);

		expect(
			screen.getByRole("button", { name: "Remove Is caused by" }),
		).toHaveAccessibleDescription("");
	});

	describe("keeps keyboard focus through a move", () => {
		const aListThatTakesItsEdits = () => {
			const TheList = () => {
				const [list, setList] = useState<readonly ParentSourceEntry[]>([
					field(11),
					field(12),
					field(13),
				]);
				return (
					<ParentSourcesList
						owner="Team"
						value={list}
						tracker="Jira"
						additionalFieldDefinitions={TheFields}
						onChange={setList}
					/>
				);
			};
			render(<TheList />);
		};

		const press = (label: string) => {
			const button = screen.getByLabelText(label);
			button.focus();
			fireEvent.click(button);
		};

		it("stays on the moved row's button while the row can go on moving", () => {
			aListThatTakesItsEdits();

			press("Move Is caused by down");

			expect(theRows()[1]).toContain("Is caused by");
			expect(screen.getByLabelText("Move Is caused by down")).toHaveFocus();
		});

		it("goes to the row's other button when the row reaches the end of the list", () => {
			aListThatTakesItsEdits();

			press("Move Program Ref down");

			expect(theRows()[2]).toContain("Program Ref");
			expect(screen.getByLabelText("Move Program Ref up")).toHaveFocus();
		});

		it("goes to the row's other button when the row reaches the top of the list", () => {
			aListThatTakesItsEdits();

			press("Move Program Ref up");

			expect(theRows()[0]).toContain("Program Ref");
			expect(screen.getByLabelText("Move Program Ref down")).toHaveFocus();
		});
	});

	it("offers only what is not listed yet, stops at five with its reason, and names a field it cannot find", () => {
		const onChange = aListOf([field(11), ownParent]);

		fireEvent.mouseDown(screen.getByRole("combobox", { name: "Add a source" }));
		const offered = within(screen.getByRole("listbox"))
			.getAllByRole("option")
			.map((option) => option.textContent);
		expect(offered).toEqual(["Program Ref", "Sponsor"]);

		fireEvent.click(
			within(screen.getByRole("listbox")).getByRole("option", {
				name: "Sponsor",
			}),
		);
		expect(onChange).toHaveBeenCalledWith([field(11), ownParent, field(13)]);
	});

	it("at five says why nothing more can be added, names a field it cannot find, and says what leaving out the tracker's own parent costs", () => {
		aListOf([field(11), field(12), field(13), field(14), field(15)]);

		expect(
			screen.getByRole("combobox", { name: "Add a source" }),
		).toHaveAttribute("aria-disabled", "true");
		expect(
			screen.getByText("A list holds at most 5 sources"),
		).toBeInTheDocument();
		expect(screen.getByText("Unknown field (id 14)")).toBeInTheDocument();
		expect(
			screen.getByText(
				/Work Items whose only parent is Jira's own parent get no Feature/,
			),
		).toBeInTheDocument();
	});
});

describe("the words of the Parent sources list", () => {
	it("speaks of Work Items and their Feature on a Team", () => {
		aListOf([field(11)], "Team");

		expect(
			screen.getByText(
				"The first source that has a value gives the parent; the rest are ignored for that Work Item.",
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				"Not in the list: Parent (Jira's own). Work Items whose only parent is Jira's own parent get no Feature.",
			),
		).toBeInTheDocument();
	});

	it("speaks of Features and their parent on a Portfolio", () => {
		aListOf([field(11)], "Portfolio");

		expect(
			screen.getByText(
				"The first source that has a value gives the parent; the rest are ignored for that Feature.",
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				"Not in the list: Parent (Jira's own). Features whose only parent is Jira's own parent get no parent.",
			),
		).toBeInTheDocument();
	});
});

describe("the Parent sources list on a settings page", () => {
	const aFollowedList = (value: ParentSourceEntry[]) => {
		render(
			<ParentSourcesList
				owner="Team"
				value={value}
				tracker="Jira"
				additionalFieldDefinitions={TheFields}
				onChange={vi.fn()}
				followed
				sourceChoice={<div>The choice</div>}
				info={{ text: "Read on each item." }}
			/>,
		);
	};

	it("keeps the add picker off when nothing is left to add", () => {
		aListOf([field(11), field(12), field(13), ownParent]);

		expect(
			screen.getByRole("combobox", { name: "Add a source" }),
		).toHaveAttribute("aria-disabled", "true");
	});

	it("shows a followed list without its rows and picker, keeping the source choice and the tip", () => {
		aFollowedList([field(11)]);

		expect(screen.getByText("The choice")).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "About Parent sources" }),
		).toBeInTheDocument();
		expect(screen.queryAllByRole("listitem")).toHaveLength(0);
		expect(
			screen.queryByRole("combobox", { name: "Add a source" }),
		).not.toBeInTheDocument();
	});
});

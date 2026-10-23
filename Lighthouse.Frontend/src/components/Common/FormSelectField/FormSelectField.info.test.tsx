import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	expectLearnMoreToOpen,
	openTheTip,
	theAboutButtonOf,
	theInfoIconsIn,
} from "../../../tests/FieldTipsTestKit";
import FormSelectField from "./FormSelectField";

/**
 * Any field list can carry an info button beside it, named after the field ("About Dependency Field"). Opening it
 * shows the tip and, when there is one, a "Learn More" link to the docs. A field list given nothing to explain looks
 * exactly as it always has.
 */

const LABEL = "Dependency Field";
const TIP = "Read on the item that waits.";
const DOCS = "https://docs.lighthouse.letpeople.work/portfolios/edit.html";
const Fields = [{ id: 1, label: "Waits On" }];

const aFieldList = (info?: { text: string; learnMoreUrl?: string }) =>
	render(
		<FormSelectField<number>
			label={LABEL}
			value=""
			onChange={vi.fn()}
			options={Fields}
			info={info}
		/>,
	);

describe("Story 6352: a field list with something to explain", () => {
	// @contract-shape:pure-function
	it("carries an info button named after the field, closed until asked", () => {
		aFieldList({ text: TIP });

		expect(theAboutButtonOf(LABEL)).toHaveAttribute("aria-expanded", "false");
		expect(screen.getByRole("combobox", { name: LABEL })).toBeInTheDocument();
		expect(screen.queryByText(TIP)).not.toBeInTheDocument();
	});

	// @contract-shape:pure-function
	it("opening the button shows the tip and a Learn More opening the docs in a new tab", async () => {
		aFieldList({ text: TIP, learnMoreUrl: DOCS });

		const tip = await openTheTip(userEvent.setup(), theAboutButtonOf(LABEL));

		expect(tip).toHaveTextContent(TIP);
		expectLearnMoreToOpen(tip, DOCS);
	});

	// @contract-shape:pure-function
	it("the button says it opens a dialog, and the open tip is that dialog, named after the button", async () => {
		aFieldList({ text: TIP, learnMoreUrl: DOCS });

		const button = theAboutButtonOf(LABEL);
		expect(button).toHaveAttribute("aria-haspopup", "dialog");
		const tip = await openTheTip(userEvent.setup(), button);

		expect(screen.getByRole("dialog", { name: `About ${LABEL}` })).toBe(tip);
	});

	// @edge @contract-shape:pure-function
	it("a tip with nowhere to lead shows its text and no Learn More", async () => {
		aFieldList({ text: TIP });

		const tip = await openTheTip(userEvent.setup(), theAboutButtonOf(LABEL));

		expect(tip).toHaveTextContent(TIP);
		expect(
			screen.queryByRole("link", { name: "Learn More" }),
		).not.toBeInTheDocument();
	});

	// @edge @contract-shape:pure-function
	it("Escape closes the tip again", async () => {
		const user = userEvent.setup();
		aFieldList({ text: TIP, learnMoreUrl: DOCS });

		await openTheTip(user, theAboutButtonOf(LABEL));
		await user.keyboard("{Escape}");

		await waitFor(() =>
			expect(screen.queryByText(TIP)).not.toBeInTheDocument(),
		);
		expect(theAboutButtonOf(LABEL)).toHaveAttribute("aria-expanded", "false");
		expect(theAboutButtonOf(LABEL)).toHaveFocus();
	});
});

describe("Story 6352: a field list with nothing to explain", () => {
	// @error @active-guard @contract-shape:unbounded-preservation
	it("shows no info icon and no button beside it", () => {
		const { container } = aFieldList();

		expect(theInfoIconsIn(container)).toHaveLength(0);
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});
});

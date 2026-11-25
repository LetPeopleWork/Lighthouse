import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ItemListManager from "./ItemListManager";

const title = "State";
const suggestions = ["Waiting for Deployment", "Waiting for Approval"];

describe("ItemListManager entry confirmed with Enter", () => {
	const onAddItem = vi.fn();

	beforeEach(() => {
		onAddItem.mockClear();
	});

	const renderList = () => {
		render(
			<ItemListManager
				title={title}
				items={["Doing"]}
				suggestions={suggestions}
				onAddItem={onAddItem}
				onRemoveItem={vi.fn()}
			/>,
		);
		return {
			user: userEvent.setup(),
			input: screen.getByLabelText(`New ${title}`),
		};
	};

	it("adds the pasted value, not a suggestion the mouse highlighted earlier", async () => {
		const { user, input } = renderList();

		await user.click(input);
		await user.hover(
			await screen.findByRole("option", { name: "Waiting for Deployment" }),
		);
		await user.clear(input);
		await user.paste("Waiting for Reproduction");
		await user.keyboard("{Enter}");

		expect(
			onAddItem.mock.calls,
			"Bug 6698: Enter must add what the user pasted; it added a suggestion highlighted before the paste. Add the entered text unless the highlight belongs to the current input.",
		).toEqual([["Waiting for Reproduction"]]);
	});

	it("adds the typed value, not a suggestion the keyboard highlighted earlier", async () => {
		const { user, input } = renderList();

		await user.click(input);
		await user.keyboard("{ArrowDown}");
		await user.clear(input);
		await user.type(input, "Waiting for Reproduction");
		await user.keyboard("{Enter}");

		expect(
			onAddItem.mock.calls,
			"Bug 6698: Enter must add what the user typed; it added a suggestion highlighted before typing. Add the entered text unless the highlight belongs to the current input.",
		).toEqual([["Waiting for Reproduction"]]);
	});

	it("adds the suggestion the user highlighted with the arrow keys while the typed text still matches it", async () => {
		const { user, input } = renderList();

		await user.type(input, "Waiting for A");
		await user.keyboard("{ArrowDown}{Enter}");

		expect(
			onAddItem.mock.calls,
			"A suggestion highlighted and confirmed while still shown must be added as before.",
		).toEqual([["Waiting for Approval"]]);
	});

	it("gives a typed value that matches a suggestion in other casing the suggestion's casing", async () => {
		const { user, input } = renderList();

		await user.type(input, "waiting for approval");
		await user.keyboard("{Enter}");

		expect(
			onAddItem.mock.calls,
			"A case-insensitive match must be added with the suggestion's own casing.",
		).toEqual([["Waiting for Approval"]]);
	});
});

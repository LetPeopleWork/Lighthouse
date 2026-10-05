import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import WarningsIcon from "./WarningsIcon";

const WAITS_ON_ANOTHER = "Waits on GR-012, which is not done yet.";
const NO_ESTIMATE = "Has no estimate.";

const hoverTheIcon = async () => {
	await userEvent.setup().hover(screen.getByTestId("warnings"));
	return await screen.findByRole("tooltip");
};

describe("the warnings of a row", () => {
	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("lists a reason given twice only once, since saying it again adds nothing", async () => {
		const consoleError = vi.spyOn(console, "error");
		render(
			<WarningsIcon
				warnings={[WAITS_ON_ANOTHER, NO_ESTIMATE, WAITS_ON_ANOTHER]}
			/>,
		);

		const listed = within(await hoverTheIcon()).getAllByRole("listitem");

		expect(listed.map((item) => item.textContent)).toEqual([
			WAITS_ON_ANOTHER,
			NO_ESTIMATE,
		]);
		expect(screen.getByTestId("warnings")).toHaveAccessibleName(
			`${WAITS_ON_ANOTHER} ${NO_ESTIMATE}`,
		);
		expect(consoleError).not.toHaveBeenCalled();
	});

	it("reads one reason given twice as the single sentence it is", async () => {
		render(<WarningsIcon warnings={[WAITS_ON_ANOTHER, WAITS_ON_ANOTHER]} />);

		const tooltip = await hoverTheIcon();

		expect(tooltip).toHaveTextContent(WAITS_ON_ANOTHER);
		expect(within(tooltip).queryByRole("list")).toBeNull();
		expect(screen.getByTestId("warnings")).toHaveAccessibleName(
			WAITS_ON_ANOTHER,
		);
	});
});

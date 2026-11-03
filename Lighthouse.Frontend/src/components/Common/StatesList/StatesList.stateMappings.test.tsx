import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { actAndSettle } from "../../../tests/actAndSettle";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../tests/MockApiServiceProvider";
import StatesList from "./StatesList";

const review: IStateMapping = {
	name: "Review",
	states: ["Code Review", "Testing"],
};

const REVIEW_TOOLTIP =
	"State mapping: Code Review, Testing. Delete it under State Mappings to remove it.";

describe("StatesList with state mappings over raw states", () => {
	const onAddDoingState = vi.fn();
	const onRemoveDoingState = vi.fn();
	const onReorderDoingStates = vi.fn();

	const suggestionService = createMockSuggestionService();
	suggestionService.getStatesForTeams = vi.fn().mockResolvedValue({
		toDoStates: [],
		doingStates: ["Review", "Deploy"],
		doneStates: [],
	});
	const context = createMockApiServiceContext({ suggestionService });

	const renderDoing = async (
		doingStates: string[],
		stateMappings: IStateMapping[] = [review],
	) => {
		await actAndSettle(() => {
			render(
				<ApiServiceContext.Provider value={context}>
					<StatesList
						toDoStates={["Backlog"]}
						onAddToDoState={vi.fn()}
						onRemoveToDoState={vi.fn()}
						doingStates={doingStates}
						onAddDoingState={onAddDoingState}
						onRemoveDoingState={onRemoveDoingState}
						onReorderDoingStates={onReorderDoingStates}
						doneStates={["Done"]}
						onAddDoneState={vi.fn()}
						onRemoveDoneState={vi.fn()}
						stateMappings={stateMappings}
					/>
				</ApiServiceContext.Provider>,
			);
		});
	};

	const chipOf = (label: string) => {
		const chip = screen.getByText(label).closest(".MuiChip-root");
		if (!chip) throw new Error(`No chip labelled ${label}`);
		return chip as HTMLElement;
	};

	const drag = (from: string, onto: string) => {
		fireEvent.dragStart(chipOf(from), {
			dataTransfer: { effectAllowed: "move", setData: vi.fn() },
		});
		fireEvent.dragOver(chipOf(onto));
		fireEvent.drop(chipOf(onto), {
			dataTransfer: { dropEffect: "move", getData: vi.fn() },
		});
	};

	beforeEach(() => {
		onAddDoingState.mockClear();
		onRemoveDoingState.mockClear();
		onReorderDoingStates.mockClear();
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("shows a mapping as one chip with a group icon where its first state is", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing", "Waiting"]);

		const chips = screen
			.getAllByText(/^(Analysis|Review|Waiting|Code Review|Testing)$/)
			.map((label) => label.textContent);
		expect(chips).toEqual(["Analysis", "Review", "Waiting"]);
		expect(
			within(chipOf("Review")).getByTestId("state-mapping-chip-icon"),
		).toBeInTheDocument();
		expect(chipOf("Review")).toHaveAttribute(
			"data-testid",
			"state-mapping-chip",
		);
	});

	it("shows a mapping whose states are apart at the position of its first state", async () => {
		await renderDoing(["Code Review", "Analysis", "Testing", "Waiting"]);

		const chips = screen
			.getAllByText(/^(Analysis|Review|Waiting|Code Review|Testing)$/)
			.map((label) => label.textContent);
		expect(chips).toEqual(["Review", "Analysis", "Waiting"]);
	});

	it("names the mapping's states in a tooltip that says how to remove it", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing"]);

		await userEvent.hover(chipOf("Review"));

		expect((await screen.findByRole("tooltip")).textContent).toBe(
			REVIEW_TOOLTIP,
		);
	});

	it("shows the tooltip to a keyboard user and keeps the mapping's name as the chip's name", async () => {
		// jsdom knows no :focus-visible, which the tooltip asks before it opens on focus; a tab key focus is visible.
		const matches = Element.prototype.matches;
		vi.spyOn(Element.prototype, "matches").mockImplementation(function (
			this: Element,
			selector: string,
		) {
			return selector === ":focus-visible"
				? this === document.activeElement
				: matches.call(this, selector);
		});
		await renderDoing(["Analysis", "Code Review", "Testing"]);
		const user = userEvent.setup();
		const chip = chipOf("Review");

		const tabTo = async (target: Element, triesLeft: number): Promise<void> => {
			if (document.activeElement === target || triesLeft === 0) return;
			await user.tab();
			return tabTo(target, triesLeft - 1);
		};
		await tabTo(chip, 20);

		expect(chip).toHaveFocus();
		expect((await screen.findByRole("tooltip")).textContent).toBe(
			REVIEW_TOOLTIP,
		);
		expect(chip).toHaveAccessibleName("Review");
		expect(chip).toHaveAccessibleDescription(REVIEW_TOOLTIP);
	});

	it("offers no way to remove a mapping chip, and still one for a plain state", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing"]);

		expect(
			within(chipOf("Review")).queryByTestId("CancelIcon"),
		).not.toBeInTheDocument();
		expect(
			within(chipOf("Analysis")).getByTestId("CancelIcon"),
		).toBeInTheDocument();
	});

	it("removes a plain state by its own name", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing"]);

		fireEvent.click(within(chipOf("Analysis")).getByTestId("CancelIcon"));

		expect(onRemoveDoingState).toHaveBeenCalledWith("Analysis");
	});

	it("moves all of a mapping's states together when the chip is dragged ahead of another", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing"]);

		drag("Review", "Analysis");

		expect(onReorderDoingStates).toHaveBeenCalledWith([
			"Code Review",
			"Testing",
			"Analysis",
		]);
	});

	it("lands a dragged mapping where it is dropped, even when its states were apart", async () => {
		await renderDoing(["Code Review", "Analysis", "Testing", "Waiting"]);

		drag("Review", "Waiting");

		expect(onReorderDoingStates).toHaveBeenCalledWith([
			"Analysis",
			"Waiting",
			"Code Review",
			"Testing",
		]);
	});

	it("drags a plain state ahead of a mapping without splitting the mapping", async () => {
		await renderDoing(["Code Review", "Testing", "Waiting"]);

		drag("Waiting", "Review");

		expect(onReorderDoingStates).toHaveBeenCalledWith([
			"Waiting",
			"Code Review",
			"Testing",
		]);
	});

	it("shows an empty mapping list as plain states", async () => {
		await renderDoing(["Analysis", "Code Review"], []);

		expect(screen.queryByTestId("state-mapping-chip")).not.toBeInTheDocument();
		expect(
			within(chipOf("Code Review")).getByTestId("CancelIcon"),
		).toBeInTheDocument();
	});

	it("does not offer a state named like one of the mappings as a suggestion", async () => {
		await renderDoing(["Analysis", "Code Review", "Testing"]);

		await userEvent.click(screen.getByLabelText("New Doing States"));

		const options = (await screen.findAllByRole("option")).map(
			(option) => option.textContent,
		);
		expect(options).toEqual(["Deploy"]);
	});
});

import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";
import CumulativeStateTimeItemPicker from "./CumulativeStateTimeItemPicker";

const getMockCandidate = (
	overrides?: Partial<ICumulativeStateTimeCandidateRow>,
): ICumulativeStateTimeCandidateRow => ({
	workItemId: 1,
	referenceId: "ITEM-1",
	title: "Build the thing",
	workItemType: "Story",
	...overrides,
});

const threeCandidates: ICumulativeStateTimeCandidateRow[] = [
	getMockCandidate({
		workItemId: 1,
		referenceId: "ALPHA-100",
		title: "Payment gateway integration",
	}),
	getMockCandidate({
		workItemId: 2,
		referenceId: "BETA-200",
		title: "Onboarding wizard polish",
	}),
	getMockCandidate({
		workItemId: 3,
		referenceId: "GAMMA-300",
		title: "Search indexing rework",
	}),
];

async function openPicker(user: ReturnType<typeof userEvent.setup>) {
	const combobox = screen.getByRole("combobox", {
		name: /select contributing work items/i,
	});
	await user.click(combobox);
	return combobox;
}

describe("CumulativeStateTimeItemPicker", () => {
	it("lists every candidate as a selectable option when opened", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
			/>,
		);

		await openPicker(user);

		const options = screen.getAllByRole("option");
		expect(options).toHaveLength(3);
		expect(screen.getByText("Payment gateway integration")).toBeInTheDocument();
		expect(screen.getByText("Onboarding wizard polish")).toBeInTheDocument();
		expect(screen.getByText("Search indexing rework")).toBeInTheDocument();
	});

	it.each([
		{
			name: "reference id",
			query: "BETA-200",
			expectedTitle: "Onboarding wizard polish",
		},
		{
			name: "title",
			query: "indexing",
			expectedTitle: "Search indexing rework",
		},
	])("filters candidates by $name match only", async ({
		query,
		expectedTitle,
	}) => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
			/>,
		);

		const combobox = await openPicker(user);
		await user.type(combobox, query);

		const options = screen.getAllByRole("option");
		expect(options).toHaveLength(1);
		expect(within(options[0]).getByText(expectedTitle)).toBeInTheDocument();
	});

	it("does not match candidates by non-reference, non-title attributes", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeItemPicker
				candidates={[
					getMockCandidate({
						workItemId: 1,
						referenceId: "ALPHA-100",
						title: "Payment gateway integration",
						workItemType: "Bug",
					}),
				]}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
			/>,
		);

		const combobox = await openPicker(user);
		await user.type(combobox, "Bug");

		expect(screen.queryByRole("option")).not.toBeInTheDocument();
	});

	it("emits the selected work item ids when a candidate is chosen", async () => {
		const user = userEvent.setup();
		const onSelectionChange = vi.fn();
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[]}
				onSelectionChange={onSelectionChange}
				onOpen={vi.fn()}
			/>,
		);

		await openPicker(user);
		await user.click(
			screen.getByRole("option", { name: /Onboarding wizard polish/i }),
		);

		expect(onSelectionChange).toHaveBeenCalledWith([2]);
	});

	it("shows a disabled empty state when there are no candidates", async () => {
		render(
			<CumulativeStateTimeItemPicker
				candidates={[]}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
				candidatesLoaded={true}
			/>,
		);

		const combobox = screen.getByRole("combobox", {
			name: /select contributing work items/i,
		});
		expect(combobox).toBeDisabled();
		expect(
			screen.getByText(/no contributing work items in this window/i),
		).toBeInTheDocument();
	});

	it("renders the current selection as removable chips", async () => {
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[2]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
			/>,
		);

		expect(screen.getByText("BETA-200")).toBeInTheDocument();
	});

	it("notifies the parent to load candidates the first time the picker opens", async () => {
		const user = userEvent.setup();
		const onOpen = vi.fn();
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={onOpen}
			/>,
		);

		await openPicker(user);

		expect(onOpen).toHaveBeenCalledTimes(1);
	});

	it("is reachable and openable by keyboard", async () => {
		const user = userEvent.setup();
		render(
			<CumulativeStateTimeItemPicker
				candidates={threeCandidates}
				selectedItemIds={[]}
				onSelectionChange={vi.fn()}
				onOpen={vi.fn()}
			/>,
		);

		await user.tab();
		const combobox = screen.getByRole("combobox", {
			name: /select contributing work items/i,
		});
		expect(combobox).toHaveFocus();

		await user.keyboard("{ArrowDown}");
		expect(screen.getAllByRole("option").length).toBeGreaterThan(0);
	});
});

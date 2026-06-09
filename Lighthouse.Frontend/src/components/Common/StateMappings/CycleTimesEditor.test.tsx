import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import CycleTimesEditor from "./CycleTimesEditor";

vi.mock("../../../hooks/useLicenseRestrictions");
const mockedUseLicenseRestrictions = vi.mocked(useLicenseRestrictions);

const setPremium = (canUsePremiumFeatures: boolean) => {
	mockedUseLicenseRestrictions.mockReturnValue({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { hasLicense: true, isValid: true, canUsePremiumFeatures },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	});
};

const mapping: IStateMapping = { name: "Validation", states: ["Active"] };

const renderEditor = (
	cycleTimeDefinitions: ICycleTimeDefinition[],
	onChange = vi.fn(),
) => {
	render(
		<CycleTimesEditor
			cycleTimeDefinitions={cycleTimeDefinitions}
			toDoStates={["New"]}
			doingStates={["Active"]}
			doneStates={["Done"]}
			stateMappings={[mapping]}
			onChange={onChange}
		/>,
	);
	return onChange;
};

const definition = (
	overrides: Partial<ICycleTimeDefinition> = {},
): ICycleTimeDefinition => ({
	id: 0,
	name: "Active to Done",
	startState: "Active",
	endState: "Done",
	...overrides,
});

describe("CycleTimesEditor", () => {
	beforeEach(() => {
		setPremium(true);
	});

	it("renders nothing for a non-premium user", () => {
		setPremium(false);
		renderEditor([definition()]);

		expect(screen.queryByText("Cycle Times")).not.toBeInTheDocument();
	});

	it("offers raw states and State-Mapping names in workflow order in a boundary picker", async () => {
		renderEditor([definition({ startState: "", endState: "" })]);

		await userEvent.click(
			screen.getByRole("combobox", { name: /start state/i }),
		);

		const options = screen
			.getByRole("listbox")
			.querySelectorAll('[role="option"]');
		expect(Array.from(options).map((option) => option.textContent)).toEqual([
			"New",
			"Active",
			"Done",
			"Validation",
		]);
	});

	it("appends a blank definition when Add Cycle Time is clicked", async () => {
		const onChange = renderEditor([]);

		await userEvent.click(
			screen.getByRole("button", { name: "Add Cycle Time" }),
		);

		expect(onChange).toHaveBeenCalledWith([
			expect.objectContaining({ name: "", startState: "", endState: "" }),
		]);
	});

	it("threads an edited name through onChange", () => {
		const onChange = renderEditor([definition()]);

		fireEvent.change(screen.getByRole("textbox", { name: "Name" }), {
			target: { value: "Renamed" },
		});

		expect(onChange).toHaveBeenCalledWith([
			expect.objectContaining({ name: "Renamed" }),
		]);
	});

	it("surfaces an inline error when the end state is not after the start state", () => {
		renderEditor([definition({ startState: "Done", endState: "Active" })]);

		expect(
			screen.getByText(
				"End state must come after the start state in the workflow",
			),
		).toBeInTheDocument();
	});

	it("surfaces an inline duplicate-name error", () => {
		renderEditor([
			definition({ name: "Same" }),
			definition({ name: "Same", startState: "New" }),
		]);

		expect(
			screen.getAllByText(/Duplicate cycle time name 'Same'/),
		).not.toHaveLength(0);
	});

	it("removes a definition when its delete button is clicked", async () => {
		const onChange = renderEditor([
			definition({ name: "Keep" }),
			definition({ name: "Drop" }),
		]);

		const deleteButtons = screen.getAllByRole("button", {
			name: /delete cycle time/i,
		});
		await userEvent.click(deleteButtons[1]);

		expect(onChange).toHaveBeenCalledWith([
			expect.objectContaining({ name: "Keep" }),
		]);
	});

	it("shows the read-only default cycle time chip when premium", () => {
		renderEditor([]);

		expect(
			within(screen.getByText("Cycle Times").closest("div") as HTMLElement),
		).toBeTruthy();
		expect(screen.getByText("Default Cycle Time")).toBeInTheDocument();
	});
});

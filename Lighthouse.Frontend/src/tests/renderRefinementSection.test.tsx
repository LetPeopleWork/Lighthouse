import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ReadinessSettings from "../components/Common/Team/ReadinessSettings";
import type { IReadinessSetting } from "../models/Refinement/Refinement";
import { renderRefinementSection } from "./renderRefinementSection";

const STORED: IReadinessSetting = {
	minYes: 3,
	minVoters: 5,
	discussWhen: { no: 1, yesIf: 2 },
};

const renderReadiness = () =>
	renderRefinementSection(STORED, (readiness, onChange) => (
		<ReadinessSettings readiness={readiness} onChange={onChange} />
	));

describe("The Refinement section render", () => {
	it("shows the value the Team was stored with until the admin edits it", () => {
		renderReadiness();

		expect(
			screen.getByRole("spinbutton", { name: "Yes votes needed" }),
		).toHaveValue(3);
		expect(
			screen.getByRole("spinbutton", { name: "Voters needed" }),
		).toHaveValue(5);
	});

	it("shows a change the admin makes as its new value", async () => {
		const { user } = renderReadiness();
		const yesVotes = screen.getByRole("spinbutton", {
			name: "Yes votes needed",
		});

		await user.clear(yesVotes);
		await user.type(yesVotes, "2");

		expect(yesVotes).toHaveValue(2);
	});

	it("builds a second edit on the first edit's result, as the form does", async () => {
		const { user, latest } = renderReadiness();
		const yesVotes = screen.getByRole("spinbutton", {
			name: "Yes votes needed",
		});
		const voters = screen.getByRole("spinbutton", { name: "Voters needed" });

		await user.clear(yesVotes);
		await user.type(yesVotes, "2");
		await user.clear(voters);
		await user.type(voters, "4");

		expect(latest()).toEqual({
			minYes: 2,
			minVoters: 4,
			discussWhen: { no: 1, yesIf: 2 },
		});
	});

	it("lets a test read the value the admin has now", async () => {
		const { user, latest } = renderReadiness();
		expect(latest()).toEqual(STORED);

		await user.click(
			screen.getByRole("checkbox", { name: "Send to discussion on No votes" }),
		);

		expect(latest().discussWhen).toEqual({ no: null, yesIf: 2 });
	});
});

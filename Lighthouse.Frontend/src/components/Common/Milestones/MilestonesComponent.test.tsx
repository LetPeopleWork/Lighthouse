import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { type IMilestone, Milestone } from "../../../models/Project/Milestone";
import MilestonesComponent from "./MilestonesComponent";

describe("MilestonesComponent", () => {
	const initialMilestones: IMilestone[] = [
		(() => {
			const milestone = new Milestone();
			milestone.id = 1;
			milestone.name = "Milestone 1";
			milestone.date = new Date("2024-08-01");
			return milestone;
		})(),
		(() => {
			const milestone = new Milestone();
			milestone.id = 2;
			milestone.name = "Milestone 2";
			milestone.date = new Date("2024-09-01");
			return milestone;
		})(),
	];

	const mockAddMilestone = vi.fn();
	const mockRemoveMilestone = vi.fn();
	const mockUpdateMilestone = vi.fn();

	it("renders correctly with initial milestones", () => {
		render(
			<MilestonesComponent
				milestones={initialMilestones}
				onAddMilestone={mockAddMilestone}
				onRemoveMilestone={mockRemoveMilestone}
				onUpdateMilestone={mockUpdateMilestone}
			/>,
		);

		for (const milestone of initialMilestones) {
			expect(screen.getByDisplayValue(milestone.name)).toBeInTheDocument();
			expect(
				screen.getByDisplayValue(milestone.date.toISOString().slice(0, 10)),
			).toBeInTheDocument();
		}
	});

	it("adds a new milestone correctly", () => {
		render(
			<MilestonesComponent
				milestones={initialMilestones}
				onAddMilestone={mockAddMilestone}
				onRemoveMilestone={mockRemoveMilestone}
				onUpdateMilestone={mockUpdateMilestone}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/New Milestone Name/i), {
			target: { value: "New Milestone" },
		});
		fireEvent.change(screen.getByLabelText(/New Milestone Date/i), {
			target: { value: "2024-10-01" },
		});
		fireEvent.click(screen.getByText(/Add Milestone/i));

		const expectedMilestone = (() => {
			const milestone = new Milestone();
			milestone.id = 0;
			milestone.name = "New Milestone";
			milestone.date = new Date("2024-10-01");
			return milestone;
		})();
		expect(mockAddMilestone).toHaveBeenCalledWith(expectedMilestone);
	});

	it("updates a milestone correctly", async () => {
		render(
			<MilestonesComponent
				milestones={initialMilestones}
				onAddMilestone={mockAddMilestone}
				onRemoveMilestone={mockRemoveMilestone}
				onUpdateMilestone={mockUpdateMilestone}
			/>,
		);

		fireEvent.change(screen.getByDisplayValue("Milestone 1"), {
			target: { value: "Updated Milestone 1" },
		});
		fireEvent.change(screen.getByDisplayValue("2024-08-01"), {
			target: { value: "2024-08-15" },
		});

		// We have to await this as we don't instantly update the name
		await new Promise((resolve) => setTimeout(resolve, 1000));

		expect(mockUpdateMilestone).toHaveBeenCalledWith("Milestone 1", {
			name: "Updated Milestone 1",
		});
		expect(mockUpdateMilestone).toHaveBeenCalledWith("Milestone 1", {
			date: new Date("2024-08-15"),
		});
	});

	it("removes a milestone correctly", () => {
		render(
			<MilestonesComponent
				milestones={initialMilestones}
				onAddMilestone={mockAddMilestone}
				onRemoveMilestone={mockRemoveMilestone}
				onUpdateMilestone={mockUpdateMilestone}
			/>,
		);

		fireEvent.click(screen.getAllByRole("button", { name: /delete/i })[0]);

		expect(mockRemoveMilestone).toHaveBeenCalledWith("Milestone 1");
	});
});

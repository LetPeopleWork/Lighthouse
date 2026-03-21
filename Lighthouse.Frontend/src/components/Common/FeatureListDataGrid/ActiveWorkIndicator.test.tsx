import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import type { IEntityReference } from "../../../models/EntityReference";
import ActiveWorkIndicator from "./ActiveWorkIndicator";

const createTeam = (id: number, name: string): IEntityReference => ({
	id,
	name,
});

describe("ActiveWorkIndicator", () => {
	it("should render hourglass icon when teams list is empty", () => {
		render(
			<MemoryRouter>
				<ActiveWorkIndicator teams={[]} />
			</MemoryRouter>,
		);

		expect(screen.getByTestId("no-active-work")).toBeInTheDocument();
	});

	it("should render engineering icon when teams list has one team", () => {
		render(
			<MemoryRouter>
				<ActiveWorkIndicator teams={[createTeam(1, "Team Alpha")]} />
			</MemoryRouter>,
		);

		expect(screen.getByTestId("active-work-indicator")).toBeInTheDocument();
	});

	it("should render engineering icon when teams list has multiple teams", () => {
		render(
			<MemoryRouter>
				<ActiveWorkIndicator
					teams={[createTeam(1, "Team Alpha"), createTeam(2, "Team Beta")]}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByTestId("active-work-indicator")).toBeInTheDocument();
	});

	it("should have accessible aria-label on the indicator button", () => {
		render(
			<MemoryRouter>
				<ActiveWorkIndicator teams={[createTeam(1, "Team Alpha")]} />
			</MemoryRouter>,
		);

		const button = screen.getByTestId("active-work-indicator");
		expect(button).toHaveAttribute("aria-label");
	});
});

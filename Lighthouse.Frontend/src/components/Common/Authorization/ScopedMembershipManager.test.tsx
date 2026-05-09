import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ScopedMembershipManager from "./ScopedMembershipManager";

describe("ScopedMembershipManager", () => {
	it("renders member roles and assignment actions", () => {
		render(
			<ScopedMembershipManager
				title="Team Access"
				members={[
					{
						userProfileId: 4,
						subject: "auth0|viewer",
						displayName: "Viewer User",
						email: "viewer@example.com",
						role: "Viewer",
					},
				]}
				allowedRoles={["TeamAdmin", "Viewer"]}
				loading={false}
				onAssignRole={vi.fn().mockResolvedValue(undefined)}
				onRemoveRole={vi.fn().mockResolvedValue(undefined)}
			/>,
		);

		expect(screen.getByTestId("scoped-members-title")).toHaveTextContent(
			"Team Access",
		);
		expect(screen.getByTestId("scoped-member-row-4")).toBeInTheDocument();
		expect(screen.getByTestId("assign-Viewer-4")).toBeInTheDocument();
		expect(screen.getByTestId("assign-TeamAdmin-4")).toBeInTheDocument();
	});

	it("invokes callbacks when assigning or removing roles", async () => {
		const user = userEvent.setup();
		const onAssignRole = vi.fn().mockResolvedValue(undefined);
		const onRemoveRole = vi.fn().mockResolvedValue(undefined);

		render(
			<ScopedMembershipManager
				title="Portfolio Access"
				members={[
					{
						userProfileId: 9,
						subject: "auth0|portfolio-admin",
						displayName: "Portfolio Admin",
						email: "portfolio-admin@example.com",
						role: "PortfolioAdmin",
					},
				]}
				allowedRoles={["PortfolioAdmin", "Viewer"]}
				loading={false}
				onAssignRole={onAssignRole}
				onRemoveRole={onRemoveRole}
			/>,
		);

		await user.click(screen.getByTestId("assign-Viewer-9"));
		await user.click(screen.getByTestId("remove-member-9"));

		expect(onAssignRole).toHaveBeenCalledWith(9, "Viewer");
		expect(onRemoveRole).toHaveBeenCalledWith(9);
	});
});

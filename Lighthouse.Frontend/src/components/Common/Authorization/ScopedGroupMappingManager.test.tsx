import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { RbacGroupMapping } from "../../../models/Authorization/RbacModels";
import { ApiError } from "../../../services/Api/ApiError";
import ScopedGroupMappingManager from "./ScopedGroupMappingManager";

const teamMappings: RbacGroupMapping[] = [
	{
		id: 11,
		groupValue: "team-12-admins",
		role: "TeamAdmin",
		scopeType: "Team",
		scopeId: 12,
	},
	{
		id: 12,
		groupValue: "team-12-viewers",
		role: "Viewer",
		scopeType: "Team",
		scopeId: 12,
	},
];

describe("ScopedGroupMappingManager", () => {
	it("loads team group mappings via scoped endpoint without error for Team Admin", async () => {
		const groupMappingsFetcher = vi.fn().mockResolvedValue(teamMappings);

		render(
			<ScopedGroupMappingManager
				title="Team Group Access"
				allowedRoles={["TeamAdmin", "Viewer"]}
				groupMappingsFetcher={groupMappingsFetcher}
				onCreateMapping={vi.fn().mockResolvedValue(undefined)}
				onRemoveMapping={vi.fn().mockResolvedValue(undefined)}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("scoped-group-row-11")).toBeInTheDocument();
		});
		expect(screen.getByTestId("scoped-group-row-12")).toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		expect(groupMappingsFetcher).toHaveBeenCalledTimes(1);
	});

	it("displays specific permission error when fetcher rejects with 403", async () => {
		const groupMappingsFetcher = vi
			.fn()
			.mockRejectedValue(new ApiError(403, "Forbidden"));

		render(
			<ScopedGroupMappingManager
				title="Team Group Access"
				allowedRoles={["TeamAdmin", "Viewer"]}
				groupMappingsFetcher={groupMappingsFetcher}
				onCreateMapping={vi.fn().mockResolvedValue(undefined)}
				onRemoveMapping={vi.fn().mockResolvedValue(undefined)}
			/>,
		);

		const alert = await screen.findByRole("alert");
		expect(alert).toHaveTextContent(/permission/i);
		expect(alert).not.toHaveTextContent(/failed to load/i);
	});

	it("displays generic error message when fetcher rejects with non-403 error", async () => {
		const groupMappingsFetcher = vi
			.fn()
			.mockRejectedValue(new ApiError(500, "Server Error"));

		render(
			<ScopedGroupMappingManager
				title="Team Group Access"
				allowedRoles={["TeamAdmin", "Viewer"]}
				groupMappingsFetcher={groupMappingsFetcher}
				onCreateMapping={vi.fn().mockResolvedValue(undefined)}
				onRemoveMapping={vi.fn().mockResolvedValue(undefined)}
			/>,
		);

		const alert = await screen.findByRole("alert");
		expect(alert).toHaveTextContent(/failed to load/i);
		expect(alert).not.toHaveTextContent(/permission/i);
	});
});

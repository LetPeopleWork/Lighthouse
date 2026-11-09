import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { RbacGroupMapping } from "../../../models/Authorization/RbacModels";
import ScopedGroupMappingManager from "./ScopedGroupMappingManager";

// The prop the manager takes to show its list and nothing else. It does not exist yet, so it is passed
// through a spread: the expectations below fail on what the page shows, not on a type error.
const readOnly = { readOnly: true };

const zenithRules: RbacGroupMapping[] = [
	{
		id: 5,
		groupValue: "zenith-leads",
		role: "TeamAdmin",
		scopeType: "Team",
		scopeId: 11,
	},
	{
		id: 6,
		groupValue: "zenith-readers",
		role: "Viewer",
		scopeType: "Team",
		scopeId: 11,
	},
];

const showTheGroupsOfZenithReadOnly = () =>
	render(
		<ScopedGroupMappingManager
			title="Team Group Access"
			allowedRoles={["TeamAdmin", "Viewer"]}
			groupMappingsFetcher={() => Promise.resolve(zenithRules)}
			onCreateMapping={vi.fn()}
			onRemoveMapping={vi.fn()}
			{...readOnly}
		/>,
	);

describe("A Team Admin looking at the groups of their Team", () => {
	it("sees the list of groups and roles, with no form to add one and no column of actions", async () => {
		showTheGroupsOfZenithReadOnly();

		const table = await screen.findByTestId("scoped-groups-table");
		expect(
			within(table)
				.getAllByRole("columnheader")
				.map((header) => header.textContent),
		).toEqual(["Group", "Role"]);
		expect(
			within(table)
				.getAllByRole("row")
				.slice(1)
				.map((row) =>
					within(row)
						.getAllByRole("cell")
						.map((cell) => cell.textContent),
				),
		).toEqual([
			["zenith-leads", "Team Admin"],
			["zenith-readers", "Viewer"],
		]);
		expect(screen.queryByLabelText(/^Group value$/)).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Add Group Mapping$/ }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Remove$/ }),
		).not.toBeInTheDocument();
	});
});

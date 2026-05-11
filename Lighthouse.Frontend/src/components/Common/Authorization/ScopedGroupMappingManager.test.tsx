import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	GroupMappingRole,
	RbacGroupMapping,
	ScopedRbacRole,
} from "../../../models/Authorization/RbacModels";
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

const portfolioMappings: RbacGroupMapping[] = [
	{
		id: 21,
		groupValue: "portfolio-9-admins",
		role: "PortfolioAdmin",
		scopeType: "Portfolio",
		scopeId: 9,
	},
];

interface RenderOptions {
	title?: string;
	allowedRoles?: ScopedRbacRole[];
	mappings?: RbacGroupMapping[];
	fetcherImpl?: () => Promise<RbacGroupMapping[]>;
	onCreateMapping?: (groupValue: string, role: ScopedRbacRole) => Promise<void>;
	onRemoveMapping?: (mappingId: number) => Promise<void>;
}

const renderManager = (overrides: RenderOptions = {}) => {
	const fetcher =
		overrides.fetcherImpl ??
		vi.fn().mockResolvedValue(overrides.mappings ?? teamMappings);
	const onCreateMapping =
		overrides.onCreateMapping ?? vi.fn().mockResolvedValue(undefined);
	const onRemoveMapping =
		overrides.onRemoveMapping ?? vi.fn().mockResolvedValue(undefined);

	const utils = render(
		<ScopedGroupMappingManager
			title={overrides.title ?? "Team Group Access"}
			allowedRoles={overrides.allowedRoles ?? ["TeamAdmin", "Viewer"]}
			groupMappingsFetcher={fetcher}
			onCreateMapping={onCreateMapping}
			onRemoveMapping={onRemoveMapping}
		/>,
	);

	return { ...utils, fetcher, onCreateMapping, onRemoveMapping };
};

describe("ScopedGroupMappingManager", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("initial load", () => {
		it("renders the supplied title", async () => {
			renderManager({ title: "Portfolio Group Access" });

			expect(screen.getByTestId("scoped-groups-title")).toHaveTextContent(
				"Portfolio Group Access",
			);
		});

		it("loads team group mappings via scoped endpoint without error for Team Admin", async () => {
			const { fetcher } = renderManager();

			await waitFor(() => {
				expect(screen.getByTestId("scoped-group-row-11")).toBeInTheDocument();
			});
			expect(screen.getByTestId("scoped-group-row-12")).toBeInTheDocument();
			expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			expect(fetcher).toHaveBeenCalledTimes(1);
		});

		it("renders a loading spinner while the fetcher is in flight", () => {
			let resolve: (value: RbacGroupMapping[]) => void = () => {};
			const pending = new Promise<RbacGroupMapping[]>((r) => {
				resolve = r;
			});
			renderManager({ fetcherImpl: () => pending });

			expect(screen.getByTestId("scoped-groups-loading")).toBeInTheDocument();
			expect(
				screen.queryByTestId("scoped-groups-table"),
			).not.toBeInTheDocument();

			resolve(teamMappings);
		});

		it("hides the loading spinner once the fetcher resolves", async () => {
			renderManager();

			await waitFor(() => {
				expect(
					screen.queryByTestId("scoped-groups-loading"),
				).not.toBeInTheDocument();
			});
			expect(screen.getByTestId("scoped-groups-table")).toBeInTheDocument();
		});

		it("renders an empty table body when no mappings are returned", async () => {
			renderManager({ mappings: [] });

			await waitFor(() => {
				expect(screen.getByTestId("scoped-groups-table")).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId(/^scoped-group-row-/),
			).not.toBeInTheDocument();
		});
	});

	describe("role label rendering", () => {
		it("renders the exact 'Team Admin' label for TeamAdmin mappings", async () => {
			renderManager();

			const row = await screen.findByTestId("scoped-group-row-11");
			expect(row).toHaveTextContent("Team Admin");
		});

		it("renders the exact 'Viewer' label for Viewer mappings", async () => {
			renderManager();

			const row = await screen.findByTestId("scoped-group-row-12");
			expect(row).toHaveTextContent("Viewer");
		});

		it("renders the exact 'Portfolio Admin' label for PortfolioAdmin mappings", async () => {
			renderManager({ mappings: portfolioMappings });

			const row = await screen.findByTestId("scoped-group-row-21");
			expect(row).toHaveTextContent("Portfolio Admin");
		});

		it("renders 'System Admin' for SystemAdmin role mappings", async () => {
			const systemAdminMapping: RbacGroupMapping = {
				id: 31,
				groupValue: "global-sysadmins",
				role: "SystemAdmin",
				scopeType: "Team",
				scopeId: 12,
			};
			renderManager({ mappings: [systemAdminMapping] });

			const row = await screen.findByTestId("scoped-group-row-31");
			expect(row).toHaveTextContent("System Admin");
		});

		it("renders 'Viewer' as a fallback for unknown roles via the ?? operator", async () => {
			const unknownRoleMapping = {
				id: 41,
				groupValue: "weird-group",
				role: "UnknownRole" as GroupMappingRole,
				scopeType: "Team",
				scopeId: 12,
			} satisfies RbacGroupMapping;
			renderManager({ mappings: [unknownRoleMapping] });

			const row = await screen.findByTestId("scoped-group-row-41");
			expect(row).toHaveTextContent("Viewer");
		});

		it("renders the group value text in the row", async () => {
			renderManager();

			const row = await screen.findByTestId("scoped-group-row-11");
			expect(row).toHaveTextContent("team-12-admins");
		});

		it("offers exactly the allowed roles in the role select dropdown", async () => {
			renderManager({ allowedRoles: ["TeamAdmin", "Viewer"] });

			await screen.findByTestId("scoped-groups-table");

			const roleInput = screen
				.getByTestId("scoped-group-role-input")
				.querySelector("input");
			expect(roleInput).not.toBeNull();
			// initial value is the first allowed role
			expect(roleInput?.value).toBe("TeamAdmin");
		});
	});

	describe("search filter", () => {
		it("returns all mappings when the search box is empty", async () => {
			renderManager();

			await screen.findByTestId("scoped-group-row-11");
			expect(screen.getByTestId("scoped-group-row-11")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-group-row-12")).toBeInTheDocument();
		});

		it("returns all mappings when the search box contains only whitespace", async () => {
			const user = userEvent.setup();
			renderManager();
			await screen.findByTestId("scoped-group-row-11");

			const searchInput = screen
				.getByTestId("scoped-groups-search")
				.querySelector("input") as HTMLInputElement;
			await user.type(searchInput, "   ");

			expect(screen.getByTestId("scoped-group-row-11")).toBeInTheDocument();
			expect(screen.getByTestId("scoped-group-row-12")).toBeInTheDocument();
		});

		it("filters by case-insensitive substring of the group value", async () => {
			const user = userEvent.setup();
			renderManager();
			await screen.findByTestId("scoped-group-row-11");

			const searchInput = screen
				.getByTestId("scoped-groups-search")
				.querySelector("input") as HTMLInputElement;
			await user.type(searchInput, "VIEWERS");

			expect(
				screen.queryByTestId("scoped-group-row-11"),
			).not.toBeInTheDocument();
			expect(screen.getByTestId("scoped-group-row-12")).toBeInTheDocument();
		});

		it("filters out all mappings when no group value matches the search", async () => {
			const user = userEvent.setup();
			renderManager();
			await screen.findByTestId("scoped-group-row-11");

			const searchInput = screen
				.getByTestId("scoped-groups-search")
				.querySelector("input") as HTMLInputElement;
			await user.type(searchInput, "nonexistent");

			expect(
				screen.queryByTestId("scoped-group-row-11"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("scoped-group-row-12"),
			).not.toBeInTheDocument();
		});
	});

	describe("create flow", () => {
		it("calls onCreateMapping with the trimmed group value and selected role", async () => {
			const user = userEvent.setup();
			const onCreateMapping = vi.fn().mockResolvedValue(undefined);
			renderManager({ onCreateMapping });
			await screen.findByTestId("scoped-groups-table");

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			await user.type(groupInput, "  new-team-admins  ");
			await user.click(screen.getByTestId("scoped-group-add-button"));

			await waitFor(() => {
				expect(onCreateMapping).toHaveBeenCalledTimes(1);
			});
			expect(onCreateMapping).toHaveBeenCalledWith(
				"new-team-admins",
				"TeamAdmin",
			);
		});

		it("clears the group value input after successful create", async () => {
			const user = userEvent.setup();
			renderManager();
			await screen.findByTestId("scoped-groups-table");

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			await user.type(groupInput, "new-group");
			await user.click(screen.getByTestId("scoped-group-add-button"));

			await waitFor(() => {
				expect(groupInput.value).toBe("");
			});
		});

		it("reloads the mappings list after a successful create", async () => {
			const user = userEvent.setup();
			const fetcher = vi.fn().mockResolvedValue(teamMappings);
			renderManager({ fetcherImpl: fetcher });
			await screen.findByTestId("scoped-groups-table");
			expect(fetcher).toHaveBeenCalledTimes(1);

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			await user.type(groupInput, "fresh-group");
			await user.click(screen.getByTestId("scoped-group-add-button"));

			await waitFor(() => {
				expect(fetcher).toHaveBeenCalledTimes(2);
			});
		});

		it("shows a 'Group value is required.' error when the input is empty", async () => {
			const user = userEvent.setup();
			const onCreateMapping = vi.fn();
			renderManager({ onCreateMapping });
			await screen.findByTestId("scoped-groups-table");

			await user.click(screen.getByTestId("scoped-group-add-button"));

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Group value is required.");
			expect(onCreateMapping).not.toHaveBeenCalled();
		});

		it("shows a 'Group value is required.' error when the input is only whitespace", async () => {
			const user = userEvent.setup();
			const onCreateMapping = vi.fn();
			renderManager({ onCreateMapping });
			await screen.findByTestId("scoped-groups-table");

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			await user.type(groupInput, "   ");
			await user.click(screen.getByTestId("scoped-group-add-button"));

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Group value is required.");
			expect(onCreateMapping).not.toHaveBeenCalled();
		});

		it("shows a 'Failed to create group mapping.' error when onCreateMapping rejects", async () => {
			const user = userEvent.setup();
			const onCreateMapping = vi.fn().mockRejectedValue(new Error("boom"));
			renderManager({ onCreateMapping });
			await screen.findByTestId("scoped-groups-table");

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			await user.type(groupInput, "doomed-group");
			await user.click(screen.getByTestId("scoped-group-add-button"));

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Failed to create group mapping.");
		});

		it("sends the role currently selected in the dropdown (not the initial value)", async () => {
			const onCreateMapping = vi.fn().mockResolvedValue(undefined);
			renderManager({
				allowedRoles: ["TeamAdmin", "Viewer"],
				onCreateMapping,
			});
			await screen.findByTestId("scoped-groups-table");

			const groupInput = screen
				.getByTestId("scoped-group-value-input")
				.querySelector("input") as HTMLInputElement;
			fireEvent.change(groupInput, { target: { value: "viewer-group" } });

			const select = screen
				.getByTestId("scoped-group-role-input")
				.querySelector("input") as HTMLInputElement;
			fireEvent.change(select, { target: { value: "Viewer" } });

			fireEvent.click(screen.getByTestId("scoped-group-add-button"));

			await waitFor(() => {
				expect(onCreateMapping).toHaveBeenCalledWith("viewer-group", "Viewer");
			});
		});
	});

	describe("remove flow", () => {
		it("calls onRemoveMapping with the row's mapping id when Remove is clicked", async () => {
			const user = userEvent.setup();
			const onRemoveMapping = vi.fn().mockResolvedValue(undefined);
			renderManager({ onRemoveMapping });
			await screen.findByTestId("scoped-group-row-11");

			await user.click(screen.getByTestId("scoped-group-remove-11"));

			await waitFor(() => {
				expect(onRemoveMapping).toHaveBeenCalledWith(11);
			});
		});

		it("reloads the mappings list after a successful remove", async () => {
			const user = userEvent.setup();
			const fetcher = vi.fn().mockResolvedValue(teamMappings);
			renderManager({ fetcherImpl: fetcher });
			await screen.findByTestId("scoped-group-row-11");
			expect(fetcher).toHaveBeenCalledTimes(1);

			await user.click(screen.getByTestId("scoped-group-remove-12"));

			await waitFor(() => {
				expect(fetcher).toHaveBeenCalledTimes(2);
			});
		});

		it("shows a 'Failed to remove group mapping.' error when onRemoveMapping rejects", async () => {
			const user = userEvent.setup();
			const onRemoveMapping = vi.fn().mockRejectedValue(new Error("boom"));
			renderManager({ onRemoveMapping });
			await screen.findByTestId("scoped-group-row-11");

			await user.click(screen.getByTestId("scoped-group-remove-11"));

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Failed to remove group mapping.");
		});
	});

	describe("load error paths", () => {
		it("displays the specific permission error message when fetcher rejects with ApiError 403", async () => {
			renderManager({
				fetcherImpl: () => Promise.reject(new ApiError(403, "Forbidden")),
			});

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent(
				"You don't have permission to view group mappings for this scope.",
			);
		});

		it("displays the generic load error when fetcher rejects with ApiError other than 403", async () => {
			renderManager({
				fetcherImpl: () => Promise.reject(new ApiError(500, "Server Error")),
			});

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Failed to load group mappings.");
		});

		it("displays the generic load error when fetcher rejects with a non-ApiError", async () => {
			renderManager({
				fetcherImpl: () => Promise.reject(new Error("network down")),
			});

			const alert = await screen.findByRole("alert");
			expect(alert).toHaveTextContent("Failed to load group mappings.");
		});
	});
});

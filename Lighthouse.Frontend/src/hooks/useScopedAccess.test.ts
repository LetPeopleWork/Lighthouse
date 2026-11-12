import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { describe, expect, it, type Mock, vi } from "vitest";
import type {
	RbacGroupMapping,
	RbacScopedMemberSummary,
	ScopedRbacRole,
} from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IRbacService } from "../services/Api/RbacService";
import { TerminologyProvider } from "../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTerminologyService,
} from "../tests/MockApiServiceProvider";
import {
	type ScopedAccess,
	type ScopedAccessKind,
	useScopedAccess,
} from "./useScopedAccess";

const SQUAD_AND_PROGRAM: Record<string, string> = {
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
};

const termsAnswer = (terms: Record<string, string>) =>
	Object.entries(terms).map(([key, value], index) => ({
		id: index + 1,
		key,
		defaultValue: value,
		description: "",
		value,
	}));

const scopes = [
	{
		kind: "Team",
		id: 7,
		defaultWord: "team",
		renamedWord: "squad",
		adminRole: "TeamAdmin",
		getMembers: "getTeamMembers",
		upsert: "upsertTeamMember",
		remove: "removeTeamMember",
		getMappings: "getTeamGroupMappings",
		otherScopeMethods: [
			"getPortfolioMembers",
			"upsertPortfolioMember",
			"removePortfolioMember",
			"getPortfolioGroupMappings",
		],
	},
	{
		kind: "Portfolio",
		id: 9,
		defaultWord: "portfolio",
		renamedWord: "program",
		adminRole: "PortfolioAdmin",
		getMembers: "getPortfolioMembers",
		upsert: "upsertPortfolioMember",
		remove: "removePortfolioMember",
		getMappings: "getPortfolioGroupMappings",
		otherScopeMethods: [
			"getTeamMembers",
			"upsertTeamMember",
			"removeTeamMember",
			"getTeamGroupMappings",
		],
	},
] as const satisfies readonly {
	kind: ScopedAccessKind;
	id: number;
	defaultWord: string;
	renamedWord: string;
	adminRole: ScopedRbacRole;
	getMembers: keyof IRbacService;
	upsert: keyof IRbacService;
	remove: keyof IRbacService;
	getMappings: keyof IRbacService;
	otherScopeMethods: readonly (keyof IRbacService)[];
}[];

type Scope = (typeof scopes)[number];

const termSets = [
	{
		name: "the default terms",
		terms: {} as Record<string, string>,
		word: (scope: Scope) => scope.defaultWord,
	},
	{
		name: "Team renamed to Squad and Portfolio to Program",
		terms: SQUAD_AND_PROGRAM,
		word: (scope: Scope) => scope.renamedWord,
	},
] as const;

const aMember = (userProfileId: number): RbacScopedMemberSummary => ({
	userProfileId,
	subject: `subject-${userProfileId}`,
	displayName: `Member ${userProfileId}`,
	role: "Viewer",
});

const serviceCall = (rbacService: IRbacService, method: keyof IRbacService) =>
	rbacService[method] as Mock;

/** Renders the hook the way a page does: inside the services and the terminology of the instance, once those terms have arrived. */
const renderScopedAccess = async (
	scope: Scope,
	{
		terms = {},
		rbacService = createMockRbacService(),
		beforeTheScopeIsKnown = false,
	}: {
		terms?: Record<string, string>;
		rbacService?: IRbacService;
		beforeTheScopeIsKnown?: boolean;
	} = {},
) => {
	const terminologyService = {
		...createMockTerminologyService(),
		getAllTerminology: vi.fn().mockResolvedValue(termsAnswer(terms)),
	};
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(
			QueryClientProvider,
			{ client: queryClient },
			createElement(
				ApiServiceContext.Provider,
				{
					value: createMockApiServiceContext({
						rbacService,
						terminologyService,
					}),
				},
				createElement(TerminologyProvider, null, children),
			),
		);

	const rendered = renderHook(
		() =>
			useScopedAccess(scope.kind, beforeTheScopeIsKnown ? undefined : scope.id),
		{
			wrapper,
		},
	);
	await waitFor(() =>
		expect(queryClient.getQueryData(["terminology-database"])).toBeDefined(),
	);

	return { access: () => rendered.result.current, rbacService };
};

const settle = async (action: () => Promise<unknown>) => {
	await act(async () => {
		await action();
	});
};

describe("The members and group mappings of a Team or a Portfolio", () => {
	describe.each(scopes)("for a $kind", (scope) => {
		it("loads the members of that scope and no other, and shows them", async () => {
			const rbacService = createMockRbacService();
			serviceCall(rbacService, scope.getMembers).mockResolvedValue([
				aMember(1),
			]);
			const { access } = await renderScopedAccess(scope, { rbacService });

			await settle(() => access().loadMembers());

			expect(
				serviceCall(rbacService, scope.getMembers),
				`Loading the members of a ${scope.kind} must ask for that ${scope.kind}'s members, or the Access tab shows the wrong list.`,
			).toHaveBeenCalledWith(scope.id);
			expect(access().members).toEqual([aMember(1)]);
			expect(access().membersLoading).toBe(false);
			expect(access().membersError).toBeUndefined();
			for (const other of scope.otherScopeMethods) {
				expect(serviceCall(rbacService, other)).not.toHaveBeenCalled();
			}
		});

		it("assigns a role and then shows the members as the server now has them", async () => {
			const rbacService = createMockRbacService();
			serviceCall(rbacService, scope.getMembers)
				.mockResolvedValueOnce([])
				.mockResolvedValue([aMember(42)]);
			serviceCall(rbacService, scope.upsert).mockResolvedValue(undefined);
			const { access } = await renderScopedAccess(scope, { rbacService });
			await settle(() => access().loadMembers());

			await settle(() => access().assignRole(42, scope.adminRole));

			expect(
				serviceCall(rbacService, scope.upsert),
				`Assigning a role must call the ${scope.kind} upsert with the scope id, the member and the role.`,
			).toHaveBeenCalledWith(scope.id, 42, scope.adminRole);
			expect(
				access().members,
				"After an assignment the members are loaded again; without that the tab keeps showing the old list.",
			).toEqual([aMember(42)]);
			expect(access().membersError).toBeUndefined();
		});

		it("removes a member and then shows the members as the server now has them", async () => {
			const rbacService = createMockRbacService();
			serviceCall(rbacService, scope.getMembers)
				.mockResolvedValueOnce([aMember(1), aMember(2)])
				.mockResolvedValue([aMember(2)]);
			serviceCall(rbacService, scope.remove).mockResolvedValue(undefined);
			const { access } = await renderScopedAccess(scope, { rbacService });
			await settle(() => access().loadMembers());

			await settle(() => access().removeMember(1));

			expect(
				serviceCall(rbacService, scope.remove),
				`Removing a member must call the ${scope.kind} removal with the scope id and the member.`,
			).toHaveBeenCalledWith(scope.id, 1);
			expect(
				access().members,
				"After a removal the members are loaded again; without that the tab keeps showing the removed member.",
			).toEqual([aMember(2)]);
		});

		it("lists, creates and removes group mappings of that scope", async () => {
			const rbacService = createMockRbacService();
			const mapping: RbacGroupMapping = {
				id: 5,
				groupValue: "leads",
				role: scope.adminRole,
				scopeType: scope.kind,
				scopeId: scope.id,
			};
			serviceCall(rbacService, scope.getMappings).mockResolvedValue([mapping]);
			const { access } = await renderScopedAccess(scope, { rbacService });

			const listed = await access().loadGroupMappings();
			await settle(() => access().createGroupMapping("leads", scope.adminRole));
			await settle(() => access().removeGroupMapping(5));

			expect(listed).toEqual([mapping]);
			expect(serviceCall(rbacService, scope.getMappings)).toHaveBeenCalledWith(
				scope.id,
			);
			expect(
				rbacService.createGroupMapping,
				`A group mapping created from a ${scope.kind} must be scoped to that ${scope.kind}.`,
			).toHaveBeenCalledWith({
				groupValue: "leads",
				role: scope.adminRole,
				scopeType: scope.kind,
				scopeId: scope.id,
			});
			expect(rbacService.removeGroupMapping).toHaveBeenCalledWith(5);
		});

		it("lets a failed group mapping call reach the caller, which shows its own message", async () => {
			const rbacService = createMockRbacService();
			rbacService.createGroupMapping = vi
				.fn()
				.mockRejectedValue(new Error("create refused"));
			rbacService.removeGroupMapping = vi
				.fn()
				.mockRejectedValue(new Error("remove refused"));
			const { access } = await renderScopedAccess(scope, { rbacService });

			await expect(
				access().createGroupMapping("leads", scope.adminRole),
			).rejects.toThrow("create refused");
			await expect(access().removeGroupMapping(5)).rejects.toThrow(
				"remove refused",
			);
			expect(access().membersError).toBeUndefined();
		});

		it("does nothing while there is no scope yet", async () => {
			const rbacService = createMockRbacService();
			const { access } = await renderScopedAccess(scope, {
				rbacService,
				beforeTheScopeIsKnown: true,
			});

			await settle(async () => {
				await access().assignRole(42, scope.adminRole);
				await access().removeMember(42);
				await access().createGroupMapping("leads", scope.adminRole);
				await access().removeGroupMapping(5);
			});

			for (const method of [
				scope.upsert,
				scope.remove,
				"createGroupMapping",
				"removeGroupMapping",
			] as const) {
				expect(
					serviceCall(rbacService, method),
					"Without a scope to act on, no handler may call the service.",
				).not.toHaveBeenCalled();
			}
		});

		it.each([
			{
				name: "an assignment",
				run: (a: ScopedAccess) => a.assignRole(42, scope.adminRole),
			},
			{ name: "a removal", run: (a: ScopedAccess) => a.removeMember(42) },
		])(
			"says the members could not be loaded when $name worked but the reload failed",
			async ({ run }) => {
				const rbacService = createMockRbacService();
				serviceCall(rbacService, scope.getMembers).mockRejectedValue(
					new Error("down"),
				);
				serviceCall(rbacService, scope.upsert).mockResolvedValue(undefined);
				serviceCall(rbacService, scope.remove).mockResolvedValue(undefined);
				const { access } = await renderScopedAccess(scope, { rbacService });

				await settle(() => run(access()));

				expect(
					access().membersError,
					"The write went through, so the only failure left to report is the reload of the members.",
				).toBe(`Failed to load ${scope.defaultWord} members.`);
			},
		);

		it("drops the failure message once the members load again", async () => {
			const rbacService = createMockRbacService();
			serviceCall(rbacService, scope.getMembers)
				.mockRejectedValueOnce(new Error("down"))
				.mockResolvedValue([aMember(1)]);
			const { access } = await renderScopedAccess(scope, { rbacService });

			await settle(() => access().loadMembers());
			expect(access().membersError).toBe(
				`Failed to load ${scope.defaultWord} members.`,
			);
			expect(access().membersLoading).toBe(false);

			await settle(() => access().loadMembers());
			expect(access().membersError).toBeUndefined();
			expect(access().members).toEqual([aMember(1)]);
		});

		describe.each(termSets)("with $name", (termSet) => {
			const actions = [
				{
					name: "loading the members",
					fails: scope.getMembers,
					run: (access: ScopedAccess) => access.loadMembers(),
					text: (word: string) => `Failed to load ${word} members.`,
				},
				{
					name: "assigning a role",
					fails: scope.upsert,
					run: (access: ScopedAccess) => access.assignRole(42, scope.adminRole),
					text: (word: string) => `Failed to update ${word} member role.`,
				},
				{
					name: "removing a member",
					fails: scope.remove,
					run: (access: ScopedAccess) => access.removeMember(42),
					text: (word: string) => `Failed to remove ${word} member.`,
				},
			];

			it.each(actions)(
				"says in the configured word that $name failed",
				async (action) => {
					const rbacService = createMockRbacService();
					serviceCall(rbacService, action.fails).mockRejectedValue(
						new Error("down"),
					);
					const { access } = await renderScopedAccess(scope, {
						rbacService,
						terms: termSet.terms,
					});

					await settle(() => action.run(access()));

					expect(
						access().membersError,
						`The failure text must name the ${scope.kind} with the instance's own word, lower-cased, and read exactly as before with the default terms.`,
					).toBe(action.text(termSet.word(scope)));
				},
			);
		});
	});
});

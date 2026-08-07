import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../models/Feature";
import type {
	FeatureMoveGate,
	FeatureOrderingPolicy,
} from "../models/FeatureOrdering";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockSettingsService,
	createMockTeamService,
} from "../tests/MockApiServiceProvider";
import { useFeatureOrdering } from "./useFeatureOrdering";

/**
 * Epic 5375 slice 03 — AC-3.7, AC-3.8, AC-3.9 and AC-3.10 are four reasons for one visual state, and
 * ADR-134 SA-12 puts them in one place. Four scattered `if`s over the same question is the frontend
 * twin of the backend failure this epic exists to prevent, and it is also how one of the four quietly
 * stops being checked.
 */
const renderTheGateOn = (instance: {
	policy: FeatureOrderingPolicy;
	premium: boolean;
}) => {
	const settingsService = createMockSettingsService();
	settingsService.getFeatureOrdering = vi
		.fn()
		.mockResolvedValue(instance.policy);

	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = vi.fn().mockResolvedValue({
		hasLicense: instance.premium,
		isValid: instance.premium,
		canUsePremiumFeatures: instance.premium,
	});

	// The shipped licence hook counts Teams and Portfolios alongside the licence itself, and answers
	// "no premium" if any of the three throws. Without these the gate would report `not-premium`
	// everywhere and every case below would pass for the wrong reason.
	const teamService = createMockTeamService();
	teamService.getTeams = vi.fn().mockResolvedValue([]);

	const portfolioService = createMockPortfolioService();
	portfolioService.getPortfolios = vi.fn().mockResolvedValue([]);

	const apiContext = createMockApiServiceContext({
		settingsService,
		licensingService,
		teamService,
		portfolioService,
	});

	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider value={apiContext}>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);

	const { result } = renderHook(() => useFeatureOrdering(), { wrapper });

	/**
	 * The policy and the licence arrive on separate promises, and the policy cannot stand in for both:
	 * `SourceOrder` is also the value the hook starts with, so a wait on the policy alone would pass
	 * before the licence had been read, and every refusal would read `not-premium`.
	 */
	return async (
		feature: IFeature,
		options: { isSortActive: boolean } = { isSortActive: false },
	): Promise<FeatureMoveGate> => {
		let gate: FeatureMoveGate = { enabled: true };

		await waitFor(() => {
			expect(result.current.policy).toBe(instance.policy);

			gate = result.current.resolveMoveGate(feature, options);
			if (instance.premium) {
				expect(gate.enabled || gate.reason !== "not-premium").toBe(true);
			}
		});

		return gate;
	};
};

const anInstanceThatOwnsItsOrder = {
	policy: "ManualOrder" as const,
	premium: true,
};

const aFeature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 7,
		name: "Rebuild the search index",
		projects: [{ id: 1, name: "Launch Alignment" }],
		canMove: true,
		blockingPortfolios: [],
		forecasts: [],
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

describe("useFeatureOrdering — the one place a move is allowed or refused", () => {
	it("lets the move through when nothing stands in its way", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(await theGateFor(aFeature())).toEqual({ enabled: true });
	});

	// AC-3.10, first half.
	it("refuses on an instance without a premium licence", async () => {
		const theGateFor = renderTheGateOn({
			policy: "ManualOrder",
			premium: false,
		});

		expect(await theGateFor(aFeature())).toMatchObject({
			enabled: false,
			reason: "not-premium",
		});
	});

	// AC-3.10, second half. Nothing to change while the tracker owns the order.
	it("refuses while the tracker owns the order", async () => {
		const theGateFor = renderTheGateOn({
			policy: "SourceOrder",
			premium: true,
		});

		expect(await theGateFor(aFeature())).toMatchObject({
			enabled: false,
			reason: "policy-off",
		});
	});

	// AC-3.9 / D14 — "up" has no predictable meaning in a list sorted by Name.
	it("refuses a relative move while the grid is sorted by a column", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(await theGateFor(aFeature(), { isSortActive: true })).toMatchObject({
			enabled: false,
			reason: "sorted",
		});
	});

	// AC-3.7 / AC-3.8 — the server's verdict, carried through untouched. The hook does not consult RBAC
	// and does not look at `projects`: both fail open (ADR-136 SA-10).
	it("carries the server's refusal through, with the Portfolio it named", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(
			await theGateFor(
				aFeature({
					canMove: false,
					moveBlockReason: "no-write",
					blockingPortfolios: [{ id: 2, name: "New Product Initiative" }],
				}),
			),
		).toEqual({
			enabled: false,
			reason: "no-write",
			blockingPortfolios: ["New Product Initiative"],
		});
	});

	// DDD-9 — a Feature in no Portfolio is movable by nobody, and the row cannot tell you which one to
	// ask, because there isn't one. The empty `projects` is in the fixture to show it is NOT what decides:
	// the reason comes off the server's verdict, because `projects` is read-filtered and an empty one
	// means "none you can see", not "none" (ADR-136 SA-10).
	it("refuses a Feature that belongs to no Portfolio, naming none", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(
			await theGateFor(
				aFeature({
					canMove: false,
					moveBlockReason: "orphan",
					projects: [],
					blockingPortfolios: [],
				}),
			),
		).toMatchObject({ enabled: false, reason: "orphan" });
	});

	// The mirror of the case above, and the reason the hook may not read `projects`: a Feature the caller
	// half-owns arrives with an EMPTY `projects` too whenever the Portfolio standing in the way is one it
	// may not read. Only the server's reason separates the two.
	it("does not mistake a half-owned Feature for one that belongs to nobody", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(
			await theGateFor(
				aFeature({
					canMove: false,
					moveBlockReason: "no-write",
					projects: [],
					blockingPortfolios: [],
				}),
			),
		).toMatchObject({ enabled: false, reason: "no-write" });
	});

	// A row that arrived before the server computed verdicts must not read as movable. Absent is not
	// permission.
	it("refuses a row that carries no verdict at all", async () => {
		const theGateFor = renderTheGateOn(anInstanceThatOwnsItsOrder);

		expect(await theGateFor(aFeature({ canMove: undefined }))).toMatchObject({
			enabled: false,
		});
	});

	// Precedence, stated rather than left to whichever `if` happens to come first: an instance-wide
	// reason removes the actions entirely (AC-3.10), so it outranks a per-row or per-grid one.
	it("reports the instance-wide reason first when several apply at once", async () => {
		const theGateFor = renderTheGateOn({
			policy: "ManualOrder",
			premium: false,
		});

		expect(
			await theGateFor(aFeature({ canMove: false }), { isSortActive: true }),
		).toMatchObject({ enabled: false, reason: "not-premium" });
	});
});

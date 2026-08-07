import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../models/Feature";
import type { FeatureOrderingPolicy } from "../models/FeatureOrdering";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockSettingsService,
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

	const apiContext = createMockApiServiceContext({
		settingsService,
		licensingService,
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

	return renderHook(() => useFeatureOrdering(), { wrapper });
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

// describe.skip = RED scaffold; DELIVER enables it (ADR-025).
describe.skip("useFeatureOrdering — the one place a move is allowed or refused", () => {
	it("lets the move through when nothing stands in its way", async () => {
		const { result } = renderTheGateOn(anInstanceThatOwnsItsOrder);

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(aFeature(), { isSortActive: false }),
		).toEqual({ enabled: true });
	});

	// AC-3.10, first half.
	it("refuses on an instance without a premium licence", async () => {
		const { result } = renderTheGateOn({
			policy: "ManualOrder",
			premium: false,
		});

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(aFeature(), { isSortActive: false }),
		).toMatchObject({ enabled: false, reason: "not-premium" });
	});

	// AC-3.10, second half. Nothing to change while the tracker owns the order.
	it("refuses while the tracker owns the order", async () => {
		const { result } = renderTheGateOn({
			policy: "SourceOrder",
			premium: true,
		});

		await waitFor(() => expect(result.current.policy).toBe("SourceOrder"));

		expect(
			result.current.resolveMoveGate(aFeature(), { isSortActive: false }),
		).toMatchObject({ enabled: false, reason: "policy-off" });
	});

	// AC-3.9 / D14 — "up" has no predictable meaning in a list sorted by Name.
	it("refuses a relative move while the grid is sorted by a column", async () => {
		const { result } = renderTheGateOn(anInstanceThatOwnsItsOrder);

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(aFeature(), { isSortActive: true }),
		).toMatchObject({ enabled: false, reason: "sorted" });
	});

	// AC-3.7 / AC-3.8 — the server's verdict, carried through untouched. The hook does not consult RBAC
	// and does not look at `projects`: both fail open (ADR-136 SA-10).
	it("carries the server's refusal through, with the Portfolio it named", async () => {
		const { result } = renderTheGateOn(anInstanceThatOwnsItsOrder);

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(
				aFeature({
					canMove: false,
					blockingPortfolios: [{ id: 2, name: "New Product Initiative" }],
				}),
				{ isSortActive: false },
			),
		).toEqual({
			enabled: false,
			reason: "no-write",
			blockingPortfolios: ["New Product Initiative"],
		});
	});

	// DDD-9 — a Feature in no Portfolio is movable by nobody, and the row cannot tell you which one to
	// ask, because there isn't one.
	it("refuses a Feature that belongs to no Portfolio, naming none", async () => {
		const { result } = renderTheGateOn(anInstanceThatOwnsItsOrder);

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(
				aFeature({ canMove: false, projects: [], blockingPortfolios: [] }),
				{ isSortActive: false },
			),
		).toMatchObject({ enabled: false, reason: "orphan" });
	});

	// A row that arrived before the server computed verdicts existed must not read as movable. Absent is
	// not permission.
	it("refuses a row that carries no verdict at all", async () => {
		const { result } = renderTheGateOn(anInstanceThatOwnsItsOrder);

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(aFeature({ canMove: undefined }), {
				isSortActive: false,
			}),
		).toMatchObject({ enabled: false });
	});

	// Precedence, stated rather than left to whichever `if` happens to come first: an instance-wide
	// reason removes the actions entirely (AC-3.10), so it outranks a per-row or per-grid one.
	it("reports the instance-wide reason first when several apply at once", async () => {
		const { result } = renderTheGateOn({
			policy: "ManualOrder",
			premium: false,
		});

		await waitFor(() => expect(result.current.policy).toBe("ManualOrder"));

		expect(
			result.current.resolveMoveGate(aFeature({ canMove: false }), {
				isSortActive: true,
			}),
		).toMatchObject({ enabled: false, reason: "not-premium" });
	});
});

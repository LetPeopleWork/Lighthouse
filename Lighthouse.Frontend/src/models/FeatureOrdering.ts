// __SCAFFOLD__ — Epic 5375 slice 02. Types only: the zod schema and the parse boundary are DELIVER's,
// because wiring them here would make part of the slice green before anybody implemented it.

/**
 * Who decides the order this instance forecasts in. An enum rather than a boolean, because
 * "manual sorting on/off" names a switch in the UI, not the thing being decided (ADR-132).
 */
export type FeatureOrderingPolicy = "SourceOrder" | "ManualOrder";

export interface IFeatureOrdering {
	policy: FeatureOrderingPolicy;
}

/**
 * Standard terminology keys used throughout the application.
 * These keys correspond to configurable terminology entries in the database.
 */
export const TERMINOLOGY_KEYS = {
	WORK_ITEM: "workItem",
	WORK_ITEMS: "workItems",
	FEATURE: "feature",
	FEATURES: "features",
	CYCLE_TIME: "cycleTime",
	THROUGHPUT: "throughput",
	WORK_IN_PROGRESS: "workInProgress",
	WIP: "wip",
	WORK_ITEM_AGE: "workItemAge",
	TAG: "tag",
	WORK_TRACKING_SYSTEM: "workTrackingSystem",
	WORK_TRACKING_SYSTEMS: "workTrackingSystems",
	QUERY: "query",
	BLOCKED: "blocked",
	SERVICE_LEVEL_EXPECTATION: "serviceLevelExpectation",
	SLE: "sle",
	TEAM: "team",
	TEAMS: "teams",
	PORTFOLIO: "portfolio",
	PORTFOLIOS: "portfolios",
	DELIVERY: "delivery",
	DELIVERIES: "deliveries",
} as const;

export type TerminologyKey =
	(typeof TERMINOLOGY_KEYS)[keyof typeof TERMINOLOGY_KEYS];

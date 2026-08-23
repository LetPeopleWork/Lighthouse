import { describe, expect, it } from "vitest";
import { Feature } from "../Feature";
import {
	DeliverySourceOptionSchema,
	DeliverySourcePreviewSchema,
	DeliverySourceSchema,
} from "./DeliverySource";

const datedRelease = {
	id: "10042",
	name: "Release 44",
	date: "2026-09-30T00:00:00Z",
	projectKey: "PROJ",
	projectName: "Project Phoenix",
	isSelectable: true,
};

const undatedRelease = {
	id: "10043",
	name: "Release 45",
	projectKey: "PROJ",
	projectName: "Project Phoenix",
	isSelectable: false,
	blockedBecause: "NoDateSet",
};

const featureWireRow = {
	name: "Checkout rewrite",
	id: 42,
	referenceId: "FTR-42",
	state: "In Progress",
	type: "Feature",
	stateCategory: "Doing",
	lastUpdated: "2026-08-20T00:00:00Z",
	startedDate: "2026-08-01T00:00:00Z",
	closedDate: null,
	cycleTime: 12,
	workItemAge: 19,
	size: 20,
	owningTeam: "Team Alpha",
	isUsingDefaultFeatureSize: false,
	parentWorkItemReference: "",
	remainingWork: { 1: 5 },
	totalWork: { 1: 20 },
	forecasts: [],
};

const previewWire = {
	name: "Release 44",
	date: "2026-09-30T00:00:00Z",
	features: [featureWireRow],
	emptyBecause: "None",
};

const epoch = new Date(0);

describe("delivery source list", () => {
	it("reads the key a request is built from and the name the tab shows", () => {
		const source = DeliverySourceSchema.parse({
			key: "jira-release",
			displayName: "Jira Release",
		});

		expect(source.key).toBe("jira-release");
		expect(source.displayName).toBe("Jira Release");
	});

	it("refuses a source that arrives without the key a request needs", () => {
		expect(() =>
			DeliverySourceSchema.parse({ displayName: "Jira Release" }),
		).toThrow();
	});
});

describe("delivery source option", () => {
	it("A Release option with no release date parses to a source with no date rather than an invalid one", () => {
		const option = DeliverySourceOptionSchema.parse(undatedRelease);

		expect(option.date).toBeNull();
		expect(option.date).not.toEqual(epoch);
		expect(option.date).not.toBeInstanceOf(Date);
	});

	it("keeps an explicitly empty date empty instead of dating it to 1970", () => {
		const option = DeliverySourceOptionSchema.parse({
			...datedRelease,
			date: null,
		});

		expect(option.date).toBeNull();
		expect(option.date).not.toEqual(epoch);
	});

	it("reads the release date of a Release that has one", () => {
		const option = DeliverySourceOptionSchema.parse(datedRelease);

		expect(option.date).toEqual(new Date("2026-09-30T00:00:00Z"));
	});

	it("refuses a date that is not one, rather than passing on an unreadable one", () => {
		expect(() =>
			DeliverySourceOptionSchema.parse({
				...datedRelease,
				date: "whenever they get round to it",
			}),
		).toThrow();
	});

	it("names a Release that cannot be bound because nobody dated it", () => {
		const option = DeliverySourceOptionSchema.parse(undatedRelease);

		expect(option.blockedBecause).toBe("NoDateSet");
		expect(option.isSelectable).toBe(false);
	});

	it("leaves a bindable Release with no reason it could not be bound", () => {
		const option = DeliverySourceOptionSchema.parse(datedRelease);

		expect(option.blockedBecause).toBeNull();
		expect(option.isSelectable).toBe(true);
	});

	it("refuses a reason nobody has written a sentence for", () => {
		expect(() =>
			DeliverySourceOptionSchema.parse({
				...datedRelease,
				blockedBecause: "SomethingElseEntirely",
			}),
		).toThrow();
	});

	it("tells apart two Releases that share a name in different projects", () => {
		const inOneProject = DeliverySourceOptionSchema.parse(datedRelease);
		const inAnother = DeliverySourceOptionSchema.parse({
			...datedRelease,
			id: "20044",
			projectKey: "JUSTATEST",
			projectName: "Just A Test",
		});

		expect(inOneProject.name).toBe(inAnother.name);
		expect(inOneProject.id).not.toBe(inAnother.id);
		expect(inOneProject.projectKey).toBe("PROJ");
		expect(inAnother.projectKey).toBe("JUSTATEST");
	});

	it("refuses an option with no id to bind to", () => {
		const { id, ...withoutId } = datedRelease;
		void id;

		expect(() => DeliverySourceOptionSchema.parse(withoutId)).toThrow();
	});

	it("refuses an option whose project is missing, so no row can render nameless", () => {
		const { projectName, ...withoutProjectName } = datedRelease;
		void projectName;

		expect(() =>
			DeliverySourceOptionSchema.parse(withoutProjectName),
		).toThrow();
	});
});

describe("delivery source preview", () => {
	it("reads the name and date the Delivery would take", () => {
		const preview = DeliverySourcePreviewSchema.parse(previewWire);

		expect(preview.name).toBe("Release 44");
		expect(preview.date).toEqual(new Date("2026-09-30T00:00:00Z"));
	});

	it("refuses a preview with no date instead of dating it to 1970", () => {
		const { date, ...withoutDate } = previewWire;
		void date;

		expect(() => DeliverySourcePreviewSchema.parse(withoutDate)).toThrow();
		expect(() =>
			DeliverySourcePreviewSchema.parse({ ...previewWire, date: null }),
		).toThrow();
	});

	it("hands back the same Feature the grid already knows how to render", () => {
		const preview = DeliverySourcePreviewSchema.parse(previewWire);

		expect(preview.features).toHaveLength(1);
		expect(preview.features[0]).toBeInstanceOf(Feature);
		expect(preview.features[0].referenceId).toBe("FTR-42");
		expect(preview.features[0].getTotalWorkForFeature()).toBe(20);
	});

	it("says nothing is tagged against the Release when the board carries no tag", () => {
		const preview = DeliverySourcePreviewSchema.parse({
			...previewWire,
			features: [],
			emptyBecause: "NothingTaggedAgainstTheSource",
		});

		expect(preview.features).toEqual([]);
		expect(preview.emptyBecause).toBe("NothingTaggedAgainstTheSource");
	});

	it("says the tagged work sits outside this Portfolio when that is the other problem", () => {
		const preview = DeliverySourcePreviewSchema.parse({
			...previewWire,
			features: [],
			emptyBecause: "TaggedWorkNotTrackedByThisPortfolio",
		});

		expect(preview.emptyBecause).toBe("TaggedWorkNotTrackedByThisPortfolio");
	});

	it("says nothing is wrong when Features did come along", () => {
		const preview = DeliverySourcePreviewSchema.parse(previewWire);

		expect(preview.emptyBecause).toBe("None");
	});

	it("refuses an emptiness nobody has written a sentence for", () => {
		expect(() =>
			DeliverySourcePreviewSchema.parse({
				...previewWire,
				emptyBecause: "SomeReasonFromANewerServer",
			}),
		).toThrow();
	});

	it("refuses a preview whose Features are not Features", () => {
		expect(() =>
			DeliverySourcePreviewSchema.parse({
				...previewWire,
				features: [{ name: "Checkout rewrite" }],
			}),
		).toThrow();
	});
});

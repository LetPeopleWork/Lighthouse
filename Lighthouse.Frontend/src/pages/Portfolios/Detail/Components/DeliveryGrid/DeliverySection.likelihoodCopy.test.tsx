import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import type { IWorkItem } from "../../../../../models/WorkItem";
import DeliverySection from "./DeliverySection";

/**
 * Story #5587 (ADR-113), slice-03 — both surfaces say WHICH probability they are showing.
 *
 * D1 is locked by the maintainer and is not re-designed here:
 *   header chip  → `All {featuresTerm} by {delivery.getFormattedDate()}: NN%`
 *                  info affordance → "P(ALL of these land by the date)"
 *   grid column  → "Likelihood (each on its own)"
 *                  header affordance → "P(this one lands), ignoring the others"
 *
 * Constraint A (terminology): every new domain noun comes from `getTerm(TERMINOLOGY_KEYS.FEATURES)`.
 * The terminology mock below is deliberately parameterised per test — a mock that hardcodes "Feature"
 * cannot fail on the defect it is meant to catch, which is a literal surviving a rename.
 *
 * Constraint B (no false promise): the header is <= every row but MAY EQUAL one (D5). Nothing in the
 * copy may claim it is lower than every row.
 *
 * The second block holds the regression guards for the states slice-03 must leave alone (AC-03.5,
 * AC-03.6, AC-03.7) - they were green before the relabel and must stay green after it.
 */

const { terminology } = vi.hoisted(() => ({
	terminology: { current: {} as Record<string, string> },
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.current[key] ?? key,
	}),
}));

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: {
			featureService: {
				getFeatureWorkItems: vi.fn(),
			},
		},
	},
}));

vi.mock(
	"../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator",
	() => ({
		default: ({ feature }: { feature: { id: number } }) => (
			<span data-testid={`progress-${feature.id}`} />
		),
	}),
);

vi.mock(
	"../../../../../components/Common/WorkItemsDialog/WorkItemsDialog",
	() => ({
		default: ({ open }: { items: IWorkItem[]; open: boolean }) =>
			open ? <div data-testid="work-items-dialog" /> : null,
	}),
);

const DEFAULT_TERMINOLOGY: Record<string, string> = {
	feature: "Feature",
	features: "Features",
	delivery: "Delivery",
	deliveries: "Deliveries",
	workItems: "Work Items",
};

const HEADER_TOOLTIP = "P(ALL of these land by the date)";
const COLUMN_TOOLTIP = "P(this one lands), ignoring the others";

const teams: IEntityReference[] = [{ id: 1, name: "Team Alpha" }];

function deliveryWith(overrides: Partial<Delivery>): Delivery {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.name = "Q3 Launch";
	delivery.date = new Date("2025-01-31").toISOString();
	delivery.features = [1, 2];
	delivery.likelihoodPercentage = 72;
	delivery.teamsWithoutForecast = [];
	delivery.progress = 40;
	delivery.remainingWork = 6;
	delivery.totalWork = 10;
	delivery.hasSufficientData = true;
	delivery.completionDates = [];
	delivery.featureLikelihoods = [
		{ featureId: 1, likelihoodPercentage: 72, hasSufficientData: true },
		{ featureId: 2, likelihoodPercentage: 95, hasSufficientData: true },
	];

	return Object.assign(delivery, overrides);
}

function featureNamed(id: number, name: string): Feature {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.remainingWork = { "1": 3 };
	feature.totalWork = { "1": 5 };
	feature.forecasts = [];

	return feature;
}

function renderSection(delivery: Delivery) {
	return render(
		<MemoryRouter>
			<DeliverySection
				delivery={delivery}
				features={[featureNamed(1, "Checkout"), featureNamed(2, "Reporting")]}
				isExpanded={true}
				isLoadingFeatures={false}
				onToggleExpanded={vi.fn()}
				onDelete={vi.fn()}
				onEdit={vi.fn()}
				teams={teams}
			/>
		</MemoryRouter>,
	);
}

beforeEach(() => {
	terminology.current = { ...DEFAULT_TERMINOLOGY };
});

describe("DeliverySection joint/marginal copy (Story #5587 slice-03)", () => {
	it("labels the header with the joint framing, the renamable plural term and the delivery date (AC-03.1, AC-03.8)", () => {
		const delivery = deliveryWith({});

		renderSection(delivery);

		// The literal is pinned on purpose. A test that rebuilds the expected string from the same
		// consts the component uses is self-satisfying — blanking the copy to "" survives it
		// (ci-learnings, mutation section). The date comes from getFormattedDate(), the SAME call the
		// "Delivery Date:" text beside it makes, so the two can never disagree (AC-03.8).
		expect(
			screen.getByText(`All Features by ${delivery.getFormattedDate()}: 72%`),
		).toBeInTheDocument();
		expect(
			screen.getByText(`Delivery Date: ${delivery.getFormattedDate()}`),
		).toBeInTheDocument();
	});

	it("explains on the header what ALL means (AC-03.1)", () => {
		renderSection(deliveryWith({}));

		// DT-14: the affordance carries the copy in a native `title`, the same mechanism the header
		// chip already uses for cannotForecastReason. Queryable from RTL and from Playwright without
		// a hover, which an MUI <Tooltip> is not.
		expect(screen.getByTitle(HEADER_TOOLTIP)).toBeInTheDocument();
	});

	it("frames the breakdown column as the per-feature probability and says what it ignores (AC-03.2)", () => {
		renderSection(deliveryWith({}));

		expect(
			screen.getByText(/Likelihood \(each on its own\)/),
		).toBeInTheDocument();
		expect(screen.getByTitle(COLUMN_TOOLTIP)).toBeInTheDocument();
	});

	it("builds the header from the renamed vocabulary rather than a literal (AC-03.3)", () => {
		terminology.current = { ...DEFAULT_TERMINOLOGY, features: "Epics" };
		const delivery = deliveryWith({});

		renderSection(delivery);

		expect(
			screen.getByText(`All Epics by ${delivery.getFormattedDate()}: 72%`),
		).toBeInTheDocument();
		// The failure this catches: a hardcoded "Features" that survives the rename and silently
		// contradicts the rest of the UI for every org that renamed the term.
		expect(screen.queryByText(/All Features by/)).not.toBeInTheDocument();
	});

	it("keeps the full label reachable under a long renamed term (deferred question 8)", () => {
		// The slice-03 learning hypothesis. jsdom has no layout, so this asserts only that the whole
		// string is RENDERED and reachable — it cannot prove the chip does not visually truncate at a
		// common viewport width. That half stays a manual/Playwright check and is recorded as such in
		// distill-red-classification.md; do not read a green here as "the copy fits".
		terminology.current = {
			...DEFAULT_TERMINOLOGY,
			features: "Programme Increment Epics",
		};
		const delivery = deliveryWith({});

		renderSection(delivery);

		expect(
			screen.getByText(
				`All Programme Increment Epics by ${delivery.getFormattedDate()}: 72%`,
			),
		).toBeInTheDocument();
	});
});

describe("DeliverySection states slice-03 must leave alone (Story #5587)", () => {
	it("never claims the header is lower than every row (AC-03.4, D1 constraint B)", () => {
		// The three-way fixture renders header 72 % with rows 72 % and 95 % — the delivery EQUALS its
		// governing row. Equality is legitimate (D5), so copy promising "lower than every feature"
		// would be false on the very fixture DISCUSS chose.
		//
		// Deliberately NOT in the skipped block: it is a constraint on copy that does not exist yet, so
		// it cannot be RED — today it passes vacuously. It sits here so it is running the moment
		// DELIVER writes the label and fails on the first draft that over-promises. A vacuous guard is
		// honest only when it is labelled as one.
		const delivery = deliveryWith({});

		const { container } = renderSection(delivery);

		const copy = [
			container.textContent ?? "",
			...Array.from(container.querySelectorAll("[title]")).map(
				(element) => element.getAttribute("title") ?? "",
			),
		].join(" ");

		expect(copy).not.toMatch(/lower than/i);
		expect(copy).not.toMatch(/less than/i);
		expect(copy).not.toMatch(/below (every|any|each)/i);
	});

	it("keeps the cannot-forecast label and its team-naming tooltip, without the joint framing (AC-03.5)", () => {
		const delivery = deliveryWith({
			likelihoodPercentage: null,
			teamsWithoutForecast: ["Team Meridian"],
		});

		renderSection(delivery);

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
		expect(
			screen.getByTitle(/No throughput history for Team Meridian/),
		).toBeInTheDocument();
		expect(screen.queryByText(/^All /)).not.toBeInTheDocument();

		// The affordance carries its copy in a title ATTRIBUTE, which queryByText cannot see - so
		// without this the whole "only in the numeric state" rule is invisible and a "Cannot forecast"
		// chip could ship beside a tooltip promising odds.
		expect(screen.queryByTitle(HEADER_TOOLTIP)).not.toBeInTheDocument();
	});

	it("keeps the not-enough-data label, without the joint framing (AC-03.5, AC-02.6)", () => {
		// AC-02.6 lands here too: slice-02 flips this flag on more deliveries but reuses this exact
		// rendering — no new indicator, no new colour.
		const delivery = deliveryWith({ hasSufficientData: false });

		renderSection(delivery);

		expect(screen.getByText(/not enough data/i)).toBeInTheDocument();
		expect(screen.queryByText(/^All /)).not.toBeInTheDocument();
		expect(screen.queryByTitle(HEADER_TOOLTIP)).not.toBeInTheDocument();
	});

	it("keeps the per-row chip's own cannot-forecast tooltip alongside the column header (AC-03.6)", () => {
		const delivery = deliveryWith({
			featureLikelihoods: [
				{
					featureId: 1,
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Meridian"],
				},
				{ featureId: 2, likelihoodPercentage: 95, hasSufficientData: true },
			],
		});

		renderSection(delivery);

		// FeatureLikelihoodChip wraps the unforecastable chip in an MUI Tooltip, which exposes the
		// reason as the element's accessible label. The new column-header affordance must not clobber
		// it — the two coexist.
		expect(
			screen.getByLabelText(/No throughput history for Team Meridian/),
		).toBeInTheDocument();
	});

	it("keeps the header chip's size and ForecastLevel colour (AC-03.7)", () => {
		const delivery = deliveryWith({});

		const { container } = renderSection(delivery);

		const chip = container.querySelector(".MuiChip-sizeSmall");

		expect(chip).not.toBeNull();
		expect(chip).toHaveStyle({
			backgroundColor: new ForecastLevel(72).color,
		});
	});
});

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import WarningsIndicator from "./WarningsIndicator";

const { terms } = vi.hoisted(() => ({
	terms: {
		workItems: "Work Items",
		feature: "Feature",
		portfolio: "Portfolio",
		blocked: "Blocked",
	} as Record<string, string>,
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms[key] ?? key,
	}),
}));

const aWarning = (
	overrides: Partial<IFeatureDependency>,
): IFeatureDependency => ({
	referenceId: "F-9",
	name: "Warehouse sync",
	url: "https://tracker.example/F-9",
	source: "TrackerLink",
	isWithheld: false,
	notHonouredReason: null,
	blockerPositionedBelow: false,
	...overrides,
});

const renderIndicator = (
	props: Partial<React.ComponentProps<typeof WarningsIndicator>> = {},
) =>
	render(
		<WarningsIndicator
			isDoneWithRemainingWork={false}
			isUsingDefaultFeatureSize={false}
			{...props}
		/>,
	);

const whatItSays = (): string =>
	screen.getByTestId("warnings").getAttribute("aria-label") ?? "";

const DONE_WITH_WORK_LEFT =
	"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.";

const NO_CHILDREN_FOUND =
	"No child Work Items were found for this Feature. The remaining Work Items displayed are based on the default Feature size specified in the advanced project settings.";

describe("WarningsIndicator", () => {
	it("shows the all-clear when there is nothing to say", () => {
		renderIndicator();

		expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
		expect(screen.queryByTestId("warnings")).not.toBeInTheDocument();
	});

	// A row either needs attention or it does not. Two icons say nothing the first did not, and turn
	// "does this row need me" into a counting exercise.
	it("shows one warning however many things are wrong", () => {
		renderIndicator({
			isDoneWithRemainingWork: true,
			isUsingDefaultFeatureSize: true,
			dependencies: [
				aWarning({ notHonouredReason: "InALoop" }),
				aWarning({
					referenceId: "F-8",
					notHonouredReason: "OutsideThisPortfolio",
				}),
				aWarning({ referenceId: "F-7", blockerPositionedBelow: true }),
			],
		});

		expect(screen.getAllByRole("button")).toHaveLength(1);
		expect(screen.queryByTestId("no-warnings")).not.toBeInTheDocument();
	});

	// Everything wrong with the row is readable in the one place, or the icon that replaced four icons
	// has thrown three reasons away.
	it("says every one of them, not just the first", () => {
		renderIndicator({
			isDoneWithRemainingWork: true,
			isUsingDefaultFeatureSize: true,
			dependencies: [
				aWarning({ notHonouredReason: "InALoop" }),
				aWarning({
					referenceId: "F-8",
					name: "Payment gateway",
					notHonouredReason: "BlockerCannotBeForecast",
				}),
			],
		});

		const said = whatItSays();

		expect(said).toContain(DONE_WITH_WORK_LEFT);
		expect(said).toContain(NO_CHILDREN_FOUND);
		expect(said).toContain("Warehouse sync are waiting on each other");
		expect(said).toContain("Payment gateway has no measured delivery");
	});

	it("says a Feature is marked done with work still left on it", () => {
		renderIndicator({ isDoneWithRemainingWork: true });

		expect(whatItSays()).toBe(DONE_WITH_WORK_LEFT);
	});

	it("says a Feature has no children and is being sized by the default", () => {
		renderIndicator({ isUsingDefaultFeatureSize: true });

		expect(whatItSays()).toBe(NO_CHILDREN_FOUND);
	});

	describe("dependency warnings", () => {
		// Having a dependency is not a warning. A row waiting on something perfectly ordinary still
		// reads as clear, or the column stops being a way to find the rows that need attention.
		it("still shows the all-clear when a dependency has nothing wrong with it", () => {
			renderIndicator({ dependencies: [aWarning({})] });

			expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
		});

		it("names the Feature waited on and says the forecast leaves it out", () => {
			renderIndicator({
				dependencies: [aWarning({ notHonouredReason: "OutsideThisPortfolio" })],
			});

			expect(whatItSays()).toContain("Warehouse sync");
			expect(whatItSays()).toContain("is not included in the forecast");
		});

		it("says two Features are waiting on each other", () => {
			renderIndicator({
				dependencies: [aWarning({ notHonouredReason: "InALoop" })],
			});

			expect(whatItSays()).toContain("waiting on each other");
		});

		it("says the wait cannot be given a date", () => {
			renderIndicator({
				dependencies: [
					aWarning({ notHonouredReason: "BlockerCannotBeForecast" }),
				],
			});

			expect(whatItSays()).toContain("no measured delivery to forecast from");
		});

		it("says a Feature waited on sits lower down in the order", () => {
			renderIndicator({
				dependencies: [aWarning({ blockerPositionedBelow: true })],
			});

			expect(whatItSays()).toContain("sits below it in the order");
		});

		// A reader who may not see the Feature is still told there is something wrong, and is told
		// nothing about what it is. Naming it here would be the disclosure the payload refused.
		it("says something is waited on without naming it when it is withheld", () => {
			renderIndicator({
				dependencies: [
					aWarning({
						isWithheld: true,
						referenceId: "",
						name: "",
						notHonouredReason: "OutsideThisPortfolio",
					}),
				],
			});

			expect(whatItSays()).toContain("a Feature you do not have access to");
		});

		// A choice somebody made is not a broken link. Warning about every Feature in a Portfolio that
		// set its dependencies aside would teach the reader to stop looking at the column.
		it("says nothing at all about a dependency the Portfolio set aside", () => {
			renderIndicator({
				dependencies: [
					aWarning({ notHonouredReason: "IgnoredByPortfolio" }),
					aWarning({
						referenceId: "F-8",
						notHonouredReason: "IgnoredByPortfolio",
						blockerPositionedBelow: true,
					}),
				],
			});

			expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
		});

		// The switch quietens the dependency warnings and nothing else. A Feature marked done with work
		// left on it is still wrong, and has nothing to do with what it waits on.
		it("leaves the warnings that existed before dependencies did exactly as they were", () => {
			renderIndicator({
				isDoneWithRemainingWork: true,
				dependencies: [aWarning({ notHonouredReason: "IgnoredByPortfolio" })],
			});

			expect(whatItSays()).toBe(DONE_WITH_WORK_LEFT);
		});

		// The word the instance reserves for an item held up right now already means something else on
		// this very grid, and it is renameable - two meanings following one rename land side by side.
		it("never uses the word that already names something else", () => {
			renderIndicator({
				dependencies: [
					aWarning({ notHonouredReason: "OutsideThisPortfolio" }),
					aWarning({ referenceId: "F-8", blockerPositionedBelow: true }),
					aWarning({ referenceId: "F-7", notHonouredReason: "InALoop" }),
					aWarning({
						referenceId: "F-6",
						notHonouredReason: "BlockerCannotBeForecast",
					}),
				],
			});

			expect(whatItSays().toLowerCase()).not.toContain("block");
		});

		// The other half of the same claim: not only is that word absent, renaming it moves nothing here.
		// A warning that quietly followed the rename would put two meanings of one word on one row.
		it("renders the same words whatever that term is renamed to", () => {
			const everyKind = [
				aWarning({ notHonouredReason: "OutsideThisPortfolio" }),
				aWarning({ referenceId: "F-8", blockerPositionedBelow: true }),
				aWarning({ referenceId: "F-7", notHonouredReason: "InALoop" }),
				aWarning({
					referenceId: "F-6",
					notHonouredReason: "BlockerCannotBeForecast",
				}),
			];

			const { unmount } = renderIndicator({ dependencies: everyKind });
			const beforeTheRename = whatItSays();
			unmount();

			terms.blocked = "Held Up";
			renderIndicator({ dependencies: everyKind });
			const afterTheRename = whatItSays();
			terms.blocked = "Blocked";

			expect(afterTheRename).toEqual(beforeTheRename);
		});
	});
});

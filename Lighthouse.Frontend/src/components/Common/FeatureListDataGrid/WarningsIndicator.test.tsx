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

describe("WarningsIndicator", () => {
	it("should render check icon when no warnings apply", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
	});

	it("should render warning icon when isDoneWithRemainingWork is true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		expect(
			screen.getByTestId("warning-done-with-remaining-work"),
		).toBeInTheDocument();
	});

	it("should render warning icon when isUsingDefaultFeatureSize is true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		expect(
			screen.getByTestId("warning-default-feature-size"),
		).toBeInTheDocument();
	});

	it("should render two warning icons when both conditions are true", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		expect(
			screen.getByTestId("warning-done-with-remaining-work"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("warning-default-feature-size"),
		).toBeInTheDocument();
	});

	it("should have accessible aria-label on done with remaining work warning", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={true}
				isUsingDefaultFeatureSize={false}
			/>,
		);

		const button = screen.getByTestId("warning-done-with-remaining-work");
		expect(button).toHaveAttribute(
			"aria-label",
			"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.",
		);
	});

	it("should have accessible aria-label on default feature size warning", () => {
		render(
			<WarningsIndicator
				isDoneWithRemainingWork={false}
				isUsingDefaultFeatureSize={true}
			/>,
		);

		const button = screen.getByTestId("warning-default-feature-size");
		expect(button).toHaveAttribute(
			"aria-label",
			"No child Work Items were found for this Feature. The remaining Work Items displayed are based on the default Feature size specified in the advanced project settings.",
		);
	});

	describe("dependency warnings", () => {
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

		// Having a dependency is not a warning. A row waiting on something perfectly ordinary still
		// reads as clear, or the column stops being a way to find the rows that need attention.
		it("still shows the all-clear when a dependency has nothing wrong with it", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[aWarning({})]}
				/>,
			);

			expect(screen.getByTestId("no-warnings")).toBeInTheDocument();
		});

		// The tooltip and the label a screen reader announces are the same sentence, said once. A row that
		// showed one thing and announced another would be two warnings to keep in step.
		it("says the same thing on hover as it says to a screen reader", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[aWarning({ notHonouredReason: "InALoop" })]}
				/>,
			);

			expect(
				screen.getByTestId("warning-dependency-in-a-loop"),
			).toHaveAttribute(
				"aria-label",
				"This Feature and Warehouse sync are waiting on each other. That dependency is not included in the forecast.",
			);
		});

		it("names the Feature waited on and says the forecast leaves it out", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[
						aWarning({ notHonouredReason: "OutsideThisPortfolio" }),
					]}
				/>,
			);

			const warning = screen.getByTestId(
				"warning-dependency-outside-portfolio",
			);
			expect(warning.getAttribute("aria-label")).toContain("Warehouse sync");
			expect(warning.getAttribute("aria-label")).toContain(
				"is not included in the forecast",
			);
		});

		it("says a Feature waited on sits lower down and that nothing was moved", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[aWarning({ blockerPositionedBelow: true })]}
				/>,
			);

			const warning = screen.getByTestId("warning-dependency-positioned-below");
			expect(warning.getAttribute("aria-label")).toContain(
				"sits below it in the order",
			);
			expect(warning.getAttribute("aria-label")).toContain("nothing was moved");
		});

		it("says two Features are waiting on each other", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[aWarning({ notHonouredReason: "InALoop" })]}
				/>,
			);

			const warning = screen.getByTestId("warning-dependency-in-a-loop");
			expect(warning.getAttribute("aria-label")).toContain(
				"waiting on each other",
			);
		});

		it("says the wait cannot be given a date", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[
						aWarning({ notHonouredReason: "BlockerCannotBeForecast" }),
					]}
				/>,
			);

			const warning = screen.getByTestId(
				"warning-dependency-cannot-be-forecast",
			);
			expect(warning.getAttribute("aria-label")).toContain(
				"no measured delivery to forecast from",
			);
		});

		// A reader who may not see the Feature is still told there is something wrong, and is told
		// nothing about what it is. Naming it here would be the disclosure the payload refused.
		it("says something is waited on without naming it when it is withheld", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[
						aWarning({
							isWithheld: true,
							referenceId: "",
							name: "",
							notHonouredReason: "OutsideThisPortfolio",
						}),
					]}
				/>,
			);

			const warning = screen.getByTestId(
				"warning-dependency-outside-portfolio",
			);
			expect(warning.getAttribute("aria-label")).toContain(
				"a Feature you do not have access to",
			);
		});

		it("shows a dependency warning beside the warnings that already existed", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={true}
					isUsingDefaultFeatureSize={true}
					dependencies={[aWarning({ notHonouredReason: "InALoop" })]}
				/>,
			);

			expect(
				screen.getByTestId("warning-done-with-remaining-work"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("warning-default-feature-size"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("warning-dependency-in-a-loop"),
			).toBeInTheDocument();
		});

		// The word the instance reserves for an item held up right now already means something else on
		// this very grid, and it is renameable - two meanings following one rename land side by side.
		it("never uses the word that already names something else", () => {
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={[
						aWarning({ notHonouredReason: "OutsideThisPortfolio" }),
						aWarning({ blockerPositionedBelow: true }),
						aWarning({ notHonouredReason: "InALoop" }),
						aWarning({ notHonouredReason: "BlockerCannotBeForecast" }),
					]}
				/>,
			);

			for (const warning of screen.getAllByRole("button")) {
				expect(warning.getAttribute("aria-label")?.toLowerCase()).not.toContain(
					"block",
				);
			}
		});

		// The other half of the same claim: not only is that word absent, renaming it moves nothing here.
		// A warning that quietly followed the rename would put two meanings of one word on one row.
		it("renders the same words whatever that term is renamed to", () => {
			const everyKind = [
				aWarning({ notHonouredReason: "OutsideThisPortfolio" }),
				aWarning({ blockerPositionedBelow: true }),
				aWarning({ notHonouredReason: "InALoop" }),
				aWarning({ notHonouredReason: "BlockerCannotBeForecast" }),
			];

			const { unmount } = render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={everyKind}
				/>,
			);
			const beforeTheRename = screen
				.getAllByRole("button")
				.map((warning) => warning.getAttribute("aria-label"));
			unmount();

			terms.blocked = "Held Up";
			render(
				<WarningsIndicator
					isDoneWithRemainingWork={false}
					isUsingDefaultFeatureSize={false}
					dependencies={everyKind}
				/>,
			);
			const afterTheRename = screen
				.getAllByRole("button")
				.map((warning) => warning.getAttribute("aria-label"));
			terms.blocked = "Blocked";

			expect(afterTheRename).toEqual(beforeTheRename);
		});
	});
});

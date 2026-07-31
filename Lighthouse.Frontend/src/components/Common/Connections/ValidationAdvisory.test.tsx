import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ValidationAdvisory from "./ValidationAdvisory";

// Story #5577, ADR-118 decision 5.
//
// This is the banner an administrator reads when their connection works but their account cannot
// supply history. The honesty obligation of the whole slice rests on it actually reaching the
// screen, so these cases render it rather than trusting that it does.
describe("ValidationAdvisory", () => {
	const testId = "create-wizard-validation-advisory";

	it("puts the advisory the backend wrote in front of the administrator", () => {
		const advisory =
			"Grant the integration account the itil role so time in progress can be measured.";

		render(<ValidationAdvisory advisory={advisory} testId={testId} />);

		expect(screen.getByTestId(testId)).toHaveTextContent(advisory);
	});

	it("speaks on the informational channel, because the connection it describes works", () => {
		render(
			<ValidationAdvisory
				advisory="Something worth knowing."
				testId={testId}
			/>,
		);

		// Using the failure channel would tell the administrator their setup is broken when it is
		// not — the connection validated, it just cannot do one thing.
		const alert = screen.getByTestId(testId);
		expect(alert.className).toMatch(/MuiAlert-colorInfo/);
		expect(alert.className).not.toMatch(/MuiAlert-colorError/);
	});

	it("says nothing when the connector had nothing to say", () => {
		render(<ValidationAdvisory advisory={null} testId={testId} />);

		expect(screen.queryByTestId(testId)).not.toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});
});

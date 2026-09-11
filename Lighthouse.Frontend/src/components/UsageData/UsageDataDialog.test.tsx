import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { UsageDataDialog, type UsageDataDialogProps } from "./UsageDataDialog";

const THE_FIVE_FIELDS = [
	"Lighthouse version",
	"How Lighthouse is deployed",
	"Whether the licence is Community or Premium",
	"When this instance was installed",
	"A random identifier for this instance",
] as const;

const renderDialog = (overrides?: Partial<UsageDataDialogProps>) => {
	const onDecision = vi.fn();
	const onClose = vi.fn();

	render(
		<UsageDataDialog
			open={true}
			collectorName="PostHog"
			dataResidency="Frankfurt, Germany"
			fields={THE_FIVE_FIELDS}
			willAskAgain={false}
			onDecision={onDecision}
			onClose={onClose}
			{...overrides}
		/>,
	);

	return { onDecision, onClose };
};

describe.skip("UsageDataDialog", () => {
	it.each(THE_FIVE_FIELDS)(
		"names %s as something that would be sent",
		(field) => {
			renderDialog();

			expect(screen.getByText(new RegExp(field, "i"))).toBeInTheDocument();
		},
	);

	it("names who would hold the data, not just that it is sent", () => {
		renderDialog();

		expect(screen.getByText(/PostHog/)).toBeInTheDocument();
	});

	it("says where the data would rest, in a place a reader can check", () => {
		renderDialog();

		expect(screen.getByText(/Frankfurt, Germany/)).toBeInTheDocument();
	});

	// The dialog is bound to claim no more than the controls can demonstrate. The instance's IP
	// reaches the collector's edge on any HTTPS request, and this audience is engineers: a promise
	// that it does not is both false and the kind of false that gets noticed. The controls suppress
	// what is *stored and enriched*, which is a different sentence and the only one we may write.
	it.each([
		/your ip (address )?is not (transmitted|sent)/i,
		/we never see your ip/i,
		/never leaves your (machine|computer|server)/i,
		/no data leaves/i,
		/completely anonymous/i,
		/we cannot identify/i,
	])("never claims %s", (forbidden) => {
		renderDialog();

		expect(document.body.textContent ?? "").not.toMatch(forbidden);
	});

	it("tells a reader who will be asked again that they will be asked again", () => {
		renderDialog({ willAskAgain: true });

		expect(screen.getByText(/ask (you )?again/i)).toBeInTheDocument();
	});

	it("tells a reader who will not be asked again that this is the last time", () => {
		renderDialog({ willAskAgain: false });

		expect(
			screen.getByText(/(not|never) ask (you )?again/i),
		).toBeInTheDocument();
	});

	it("does not say it will ask again to someone it will never ask again", () => {
		renderDialog({ willAskAgain: false });

		expect(document.body.textContent ?? "").not.toMatch(
			/we will ask you again in a few months/i,
		);
	});

	it("reports a grant when the reader agrees", async () => {
		const { onDecision } = renderDialog();

		await userEvent.click(screen.getByRole("button", { name: /yes/i }));

		expect(onDecision).toHaveBeenCalledWith("granted");
	});

	it("reports a refusal when the reader declines", async () => {
		const { onDecision } = renderDialog();

		await userEvent.click(screen.getByRole("button", { name: /no/i }));

		expect(onDecision).toHaveBeenCalledWith("declined");
	});

	it("offers a way out that is not a decision, because a dialog nobody can leave is a dark pattern", async () => {
		const { onClose, onDecision } = renderDialog();

		await userEvent.keyboard("{Escape}");

		expect(onClose).toHaveBeenCalled();
		expect(onDecision).not.toHaveBeenCalled();
	});

	it("renders nothing at all when it is closed", () => {
		renderDialog({ open: false });

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});

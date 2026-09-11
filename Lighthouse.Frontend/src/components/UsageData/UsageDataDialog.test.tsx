import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { UsageDataDialog, type UsageDataDialogProps } from "./UsageDataDialog";

// AC-02.1 and AC-04.2 both fix the payload as exactly these five. An earlier version of this file
// said "When this instance was installed" — the install timestamp is a SOURCE the instance holds
// (S12), not a field that is sent, and naming it here would have put a wrong list in front of the
// person being asked to consent to it.
const THE_FIVE_FIELDS = [
	"A random identifier for this instance",
	"Lighthouse version",
	"How Lighthouse is deployed",
	"Whether the licence is Community or Premium",
	"When the data was sent",
] as const;

// AC-02.2 is a POSITIVE requirement: the dialog must say these are never sent.
const NEVER_SENT = [
	"work item titles",
	"queries",
	"names",
	"URLs",
	"email addresses",
	"free text",
] as const;

const DOCS_URL =
	"https://docs.lighthouse.letpeople.work/settings/usagedata.html";

const renderDialog = (overrides?: Partial<UsageDataDialogProps>) => {
	const onDecision = vi.fn();
	const onClose = vi.fn();

	render(
		<UsageDataDialog
			open={true}
			collectorName="PostHog"
			dataResidency="Frankfurt, Germany"
			fields={THE_FIVE_FIELDS}
			neverSent={NEVER_SENT}
			docsUrl={DOCS_URL}
			willAskAgain={false}
			onDecision={onDecision}
			onClose={onClose}
			{...overrides}
		/>,
	);

	return { onDecision, onClose };
};

// Every negative assertion below needs this first. A test that asserts "the copy does not say X"
// passes trivially when the dialog renders nothing at all — which is exactly the state a RED
// scaffold is in. SurveyNudge.test.tsx uses the same anchor for the same reason.
const anchorOnRenderedDialog = () =>
	expect(screen.getByRole("dialog")).toBeInTheDocument();

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

	it.each(NEVER_SENT)(
		"says plainly that %s are never sent, rather than leaving it to be inferred",
		(category) => {
			renderDialog();

			expect(screen.getByText(new RegExp(category, "i"))).toBeInTheDocument();
		},
	);

	it("links the full list, so the dialog is not the only place the answer exists", () => {
		renderDialog();

		expect(screen.getByRole("link", { name: /usage data/i })).toHaveAttribute(
			"href",
			DOCS_URL,
		);
	});

	// The dialog is bound to claim no more than the controls can demonstrate. The instance's IP
	// reaches the collector's edge on any HTTPS request, and this audience is engineers: a promise
	// that it does not is both false and the kind of false that gets noticed. The controls suppress
	// what is *stored and enriched*, which is a different sentence and the only one we may write.
	//
	// These are structural rather than six fixed sentences. An earlier version matched
	// /your ip (address )?is not (transmitted|sent)/ and would have missed "Your IP address is not
	// stored or transmitted", "We do not send your IP address" and "your IP is never shared" — the
	// phrasings someone would actually write.
	it.each([
		[
			"that the IP is not sent, seen, shared or stored",
			/\bip\b[^.]{0,60}\b(not|never)\b[^.]{0,60}(transmit|sent|send|leav|shar|stor|see|collect)/i,
		],
		[
			"that the IP never reaches anyone",
			/\b(not|never|no)\b[^.]{0,40}\bip\b[^.]{0,40}(transmit|sent|send|leav|shar|stor|collect)/i,
		],
		[
			"that nothing leaves the machine",
			/never leaves your (machine|computer|server|instance)/i,
		],
		["that no data leaves at all", /no data (ever )?leaves/i],
		[
			"that it is completely anonymous",
			/(completely|fully|entirely|100%) anonymous/i,
		],
		[
			"that you cannot be identified",
			/(cannot|can't|could not) (be )?identif/i,
		],
		["that nothing is tracked", /(never|not|no) track/i],
		// Added 2026-09-11 after reading the PostHog DPA, which reserves processing "outside of the
		// Protected Area including in the US". Data RESTS in Frankfurt and the named sub-processors
		// are EU — but that is not "never leaves the EU", and a reader who infers the second from
		// the first has been misled by omission. Same failure class as the IP claim above.
		[
			"that the data never leaves the EU",
			/\b(never|not|doesn't|does not|no)\b[^.]{0,40}(leave|leaves)[^.]{0,20}(the )?(eu|europe|germany)/i,
		],
		[
			"that the data stays only in the EU",
			/\b(stays?|remains?|kept|stored)\b[^.]{0,30}\b(only|solely|exclusively|entirely)\b[^.]{0,20}(in )?(the )?(eu|europe|germany)/i,
		],
	])("never claims %s", (_name, forbidden) => {
		renderDialog();
		anchorOnRenderedDialog();

		expect(document.body.textContent ?? "").not.toMatch(forbidden);
	});

	it("tells a reader who will be asked again that they will be asked again", () => {
		renderDialog({ willAskAgain: true });
		anchorOnRenderedDialog();

		// Anchored on the negation, not just the phrase: /ask (you )?again/ is a substring of
		// "we will never ask you again", so the loose form passed against the opposite copy.
		expect(document.body.textContent ?? "").toMatch(/\bask(ed)? (you )?again/i);
		expect(document.body.textContent ?? "").not.toMatch(
			/\b(never|not|won't|will not)\b[^.]{0,30}ask/i,
		);
	});

	it("tells a reader who will not be asked again that this is the last time", () => {
		renderDialog({ willAskAgain: false });
		anchorOnRenderedDialog();

		expect(document.body.textContent ?? "").toMatch(
			/\b(never|won't|will not|not)\b[^.]{0,30}ask/i,
		);
	});

	it("does not promise a Community reader silence it cannot deliver", () => {
		renderDialog({ willAskAgain: true });
		anchorOnRenderedDialog();

		expect(document.body.textContent ?? "").not.toMatch(
			/\b(never|won't|will not)\b[^.]{0,30}ask/i,
		);
	});

	it("reports a grant when the reader agrees", async () => {
		const { onDecision } = renderDialog();

		await userEvent.click(screen.getByRole("button", { name: /^yes/i }));

		expect(onDecision).toHaveBeenCalledWith("granted");
	});

	it("reports a refusal when the reader declines", async () => {
		const { onDecision } = renderDialog();

		// Anchored: /no/i is a substring of "anonymous", so an unanchored matcher can resolve to
		// the grant button or throw on multiple matches, either way naming the wrong cause.
		await userEvent.click(screen.getByRole("button", { name: /^no/i }));

		expect(onDecision).toHaveBeenCalledWith("declined");
	});

	// AC-02.6, and the criterion D2's whole ePrivacy Article 5(3) argument rests on: the token is
	// written only AFTER the click, which is what keeps it inside the strictly-necessary exemption.
	// Nothing may touch browser storage while the dialog is merely being read.
	it("writes nothing to the browser while it is only being read", async () => {
		const { onDecision } = renderDialog();
		anchorOnRenderedDialog();

		const storageBefore = { ...localStorage };
		const sessionBefore = { ...sessionStorage };

		await userEvent.keyboard("{Escape}");

		expect({ ...localStorage }).toEqual(storageBefore);
		expect({ ...sessionStorage }).toEqual(sessionBefore);
		expect(onDecision).not.toHaveBeenCalled();
	});

	it("offers a way out that is not a decision, because a dialog nobody can leave is a dark pattern", async () => {
		const { onClose, onDecision } = renderDialog();

		await userEvent.keyboard("{Escape}");

		expect(onClose).toHaveBeenCalled();
		expect(onDecision).not.toHaveBeenCalled();
	});

	it("renders nothing at all when it is closed", () => {
		renderDialog({ open: false });

		// Not queryByRole('dialog') alone: that excludes hidden elements by default, so a MUI
		// dialog left mounted with keepMounted would pass while the whole field list and the
		// collector's name sat in the DOM.
		const rendered = document.body.textContent ?? "";
		expect(rendered).not.toContain("PostHog");
		for (const field of THE_FIVE_FIELDS) {
			expect(rendered).not.toContain(field);
		}
	});
});

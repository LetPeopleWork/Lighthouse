import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { UsageDataDialog, type UsageDataDialogProps } from "./UsageDataDialog";

// The dialog must say these are never sent, out loud, rather than leaving a reader to infer it from
// a list of what is. This copy is kept separate from the production constant on purpose: a test
// that imported it would only assert that the code equals itself.
const NEVER_SENT = [
	"work item titles",
	"queries",
	"names",
	"URLs",
	"email addresses",
	"or any free text you have typed",
] as const;

const DOCS_URL =
	"https://docs.lighthouse.letpeople.work/settings/usagedata.html";

const renderDialog = (overrides?: Partial<UsageDataDialogProps>) => {
	const onDecision = vi.fn();
	const onClose = vi.fn();

	render(
		<UsageDataDialog
			open={true}
			neverSent={NEVER_SENT}
			docsUrl={DOCS_URL}
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

describe("UsageDataDialog", () => {
	it("says the feature is off until somebody turns it on", () => {
		renderDialog();

		expect(document.body.textContent ?? "").toMatch(
			/off unless you switch it on/i,
		);
	});

	// Two earlier drafts of this sentence were wrong in opposite ways. "You are identified only by a
	// random identifier" told a reader they were identified and then argued about the manner of it.
	// Replacing it with "nothing we send identifies you personally" contradicted the next sentence,
	// which introduces an identifier. The value is for telling a repeat visit from a new one, so
	// saying that is both true and free of the contradiction.
	it("says what is counted rather than who, and says what the random value is for", () => {
		renderDialog();

		const rendered = document.body.textContent ?? "";
		expect(rendered).toMatch(/how lighthouse is used, not who uses it/i);
		expect(rendered).toMatch(/repeat visit from a new one/i);
		expect(rendered).not.toMatch(/you are identified/i);
	});

	// Asking for something without saying why it is wanted is how consent dialogs earn their
	// reputation. This section is the answer, and its absence is a defect rather than a style choice.
	it("says why the data is wanted and what it changes", () => {
		renderDialog();

		expect(screen.getByText(/why we ask for this/i)).toBeInTheDocument();
		expect(document.body.textContent ?? "").toMatch(
			/evidence rather than intuition/i,
		);
	});

	it("says the decision can be reversed later, and from where", () => {
		renderDialog();

		expect(document.body.textContent ?? "").toMatch(
			/change your mind at any time from the footer/i,
		);
	});

	// Reversible is not retroactive, and a reader who hears the first as the second has been misled.
	it("does not let a reversible choice read as an erasable one", () => {
		renderDialog();

		expect(document.body.textContent ?? "").toMatch(
			/does not erase what was already sent/i,
		);
	});

	// The dialog used to enumerate the payload and name the processor. Both now live on the linked
	// page, because both change as the feature grows and a stale list in a dialog still looks
	// authoritative. If either comes back here, it comes back with a way to keep it true.
	it.each([
		["how often anything is sent", /once a (day|week)|daily|every day/i],
		["a count of what is sent", /\b(three|four|five|six) things\b/i],
		["who holds the data", /posthog/i],
		["where the data rests", /frankfurt|germany/i],
		["how long it is kept", /\bfor (one|a) year\b|\b\d+ (months|days)\b/i],
	])("leaves %s to the page that can be kept current", (_name, forbidden) => {
		renderDialog();
		anchorOnRenderedDialog();

		expect(document.body.textContent ?? "").not.toMatch(forbidden);
	});

	it.each(NEVER_SENT)(
		"says plainly that %s are never sent, rather than leaving it to be inferred",
		(category) => {
			renderDialog();

			expect(screen.getByText(new RegExp(category, "i"))).toBeInTheDocument();
		},
	);

	// Each category is separately checked above, but only as an "appears somewhere" match. Run the
	// six together and a mutation that drops the separator turns the sentence into
	// "work item titlesqueriesnames", which every one of those matches still passes.
	it("reads as a sentence rather than a run-on list", () => {
		renderDialog();

		expect(document.body.textContent ?? "").toContain(
			"We never send work item titles, queries, names, URLs, email addresses, or any free text you have typed.",
		);
	});

	it("says nothing about a failure it has not had", () => {
		renderDialog();

		anchorOnRenderedDialog();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	// Closing on a failure would tell somebody their choice had been taken when it had not, so the
	// dialog stays open and has to say why it is still there.
	it("says the answer was not saved, and that nothing changed", () => {
		renderDialog({ failedToRecord: true });

		expect(screen.getByRole("alert")).toHaveTextContent(
			/could not be saved, so nothing has changed/i,
		);
	});

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

	// The dialog says nothing about whether the question will come back, on purpose and for
	// everybody. It briefly did: a sentence picked by whether this reader would be asked again.
	// Two problems, and the second is why it is gone rather than corrected.
	//
	// It was wrong for Premium. The boolean it read answers "given what you have already said, will
	// you be asked again" - which for somebody who has said nothing is yes, whatever their tier. So
	// a Premium reader, for whom a refusal is final, was promised we would come back.
	//
	// And a promise about a cadence somebody has not chosen yet does not help them choose. It is
	// one more sentence to read on a screen that already asks a lot, and the footer icon - which the
	// dialog does mention - is the answer to "how do I change my mind" either way.
	it("makes no promise about being asked again, in either direction", () => {
		renderDialog();
		anchorOnRenderedDialog();

		expect(document.body.textContent ?? "").not.toMatch(
			/\bask(ed|ing)? (you )?again\b/i,
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
		// dialog left mounted with keepMounted would pass while the whole of the copy sat in the DOM.
		const rendered = document.body.textContent ?? "";
		expect(rendered).not.toMatch(/random value/i);
		expect(rendered).not.toMatch(/why we ask for this/i);
		for (const category of NEVER_SENT) {
			expect(rendered).not.toContain(category);
		}
	});
});

/**
 * Epic 5733 slice 03 (ADO #5836) - AC-06.6 and AC-06.7 where the reader meets them, plus the
 * sentence that tells everybody the switch exists.
 *
 * Skipped until DELIVER wires the prop through. The dialog is deliberately still shown and still
 * readable while an administrator holds the veto: hiding it would make the reader's own decision
 * disappear along with the instance's, and it is only suspended.
 *
 * The dialog is served without authentication and has never been told this instance's licence
 * tier, so the sentence about the switch is the same for everybody. A tier-conditional version
 * would disclose the tier to an anonymous caller, which is the shape C5 already ruled out for the
 * cadence sentence.
 */
describe("UsageDataDialog, where an administrator has stopped usage data", () => {
	it("is still shown and still readable, because the decision is suspended rather than taken away", () => {
		renderDialog({ administratorDisabled: true });

		anchorOnRenderedDialog();
		expect(document.body.textContent ?? "").toMatch(/why we ask for this/i);
	});

	// Dispatched with fireEvent rather than userEvent on purpose. userEvent refuses to touch an
	// element the browser would not let a pointer reach, so it throws before the click - which
	// proves the button is unreachable but never asks the question this test is about, which is
	// whether the handler behind it can still run.
	it("will not take a yes it could not act on", () => {
		const { onDecision } = renderDialog({ administratorDisabled: true });

		const agree = screen.getByRole("button", { name: /yes, send it/i });

		expect(agree).toBeDisabled();

		fireEvent.click(agree);
		expect(onDecision).not.toHaveBeenCalled();
	});

	it("will not take a no either, because refusing something already stopped records a decision nobody made", () => {
		const { onDecision } = renderDialog({ administratorDisabled: true });

		const decline = screen.getByRole("button", { name: /no, thank you/i });

		expect(decline).toBeDisabled();

		fireEvent.click(decline);
		expect(onDecision).not.toHaveBeenCalled();
	});

	// Asserted through the alert rather than through the dialog's whole text. The sentence telling
	// everybody the switch exists also says "administrator" and is rendered either way, so a match
	// against the body would pass with no hint at all.
	it("says why the buttons cannot be used, rather than leaving them dead", () => {
		renderDialog({ administratorDisabled: true });

		const hint = screen.getByRole("alert");

		// Both halves, because either alone is satisfied by the wrong sentence: the one telling
		// everybody the switch exists also says "administrator", and a bare "not being sent" leaves
		// the reader to assume it was their own doing. The wording itself is pinned where it is
		// written; asserting it here as well would only check that the code equals itself.
		expect(hint).toHaveTextContent(/administrator/i);
		expect(hint).toHaveTextContent(/not being sent/i);
	});

	it("stays quiet about it when nobody has stopped anything", () => {
		renderDialog();

		anchorOnRenderedDialog();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("does not tell a reader they declined something they never answered", () => {
		renderDialog({ administratorDisabled: true });

		anchorOnRenderedDialog();
		const rendered = (document.body.textContent ?? "").toLowerCase();
		expect(rendered).not.toContain("you declined");
		expect(rendered).not.toContain("your choice");
	});

	it("leaves the buttons alone when no administrator has stopped anything", async () => {
		const { onDecision } = renderDialog();

		await userEvent.click(
			screen.getByRole("button", { name: /yes, send it/i }),
		);

		expect(onDecision).toHaveBeenCalledWith("granted");
	});

	it("tells everybody the switch exists, whatever this instance's licence is", () => {
		renderDialog();

		anchorOnRenderedDialog();
		expect(document.body.textContent ?? "").toMatch(/premium/i);
	});

	it("still says the switch exists once somebody has used it", () => {
		renderDialog({ administratorDisabled: true });

		anchorOnRenderedDialog();
		expect(document.body.textContent ?? "").toMatch(/premium/i);
	});
});

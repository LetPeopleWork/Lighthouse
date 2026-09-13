/**
 * The two sentences that talk about the administrator's veto, kept together and away from the
 * components that show them.
 *
 * They are a pair on purpose. One says usage data has been stopped for this whole instance and that
 * the reader did not do it; the other says that stopping it is something an instance can do. Written
 * inline they would drift apart, and the failure that produces is quiet: a reader told "not being
 * sent" with no subject reads it as their own refusal, and a reader told nothing about the switch
 * never learns the control exists.
 *
 * Neither sentence knows what licence this instance holds. The dialog is served without
 * authentication and has never been told the tier - saying "your administrator could turn this off
 * with Premium" to everybody is both true and the only version that does not leak it.
 */

const notImplemented = (name: string): never => {
	throw new Error(`${name} is not implemented`);
};

/**
 * Shown where usage data has been stopped for the whole instance. Says who did it, because "not
 * being sent" on its own reads as the reader's own decision.
 */
export const administratorStoppedItSentence = (): string =>
	notImplemented("administratorStoppedItSentence");

/**
 * Shown to everybody reading the dialog. Tells a reader on a licensed instance that their
 * administrator holds this lever, and a reader on an unlicensed one that the lever exists.
 */
export const theVetoIsAvailableSentence = (): string =>
	notImplemented("theVetoIsAvailableSentence");

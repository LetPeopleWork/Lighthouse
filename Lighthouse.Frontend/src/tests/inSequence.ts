/**
 * Runs `step` for each item, one after the other: each step starts only once the one
 * before it has finished, in the order the items are given. For test actions that must
 * not overlap, such as key presses, clicks or turns of the event loop.
 */
export function inSequence<T>(
	items: Iterable<T>,
	step: (item: T) => Promise<unknown>,
): Promise<void> {
	return Array.from(items).reduce<Promise<void>>(
		(previous, item) =>
			previous.then(async () => {
				await step(item);
			}),
		Promise.resolve(),
	);
}

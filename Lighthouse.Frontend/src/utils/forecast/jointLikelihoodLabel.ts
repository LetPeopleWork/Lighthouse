/**
 * The joint framing from ADR-113 D1: a delivery's number answers "will ALL of these land", so both
 * surfaces that render it say so. One definition, because the copy is one piece of knowledge - the
 * detail header and the portfolio overview chip drifted apart the first time R1 added the second one.
 */
export function jointLikelihoodLabel({
	term,
	value,
	date,
	count,
}: {
	term: string;
	value: string;
	date?: string;
	count?: number;
}): string {
	const subject = count === undefined ? term : `${count} ${term}`;
	const by = date === undefined ? "" : ` by ${date}`;

	return `All ${subject}${by}: ${value}`;
}

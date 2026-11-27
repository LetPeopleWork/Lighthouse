// The server decides how many closed Work Items a Within SLE share needs and sends that number with every
// answer. This value only covers an answer that arrives without it, such as one from an older server.
const MINIMUM_CLOSED_WHEN_NOT_SENT = 5;

export function minimumClosedOf(answer: {
	readonly minimumClosed?: number;
}): number {
	return answer.minimumClosed ?? MINIMUM_CLOSED_WHEN_NOT_SENT;
}

/**
 * Computes a human-readable blocked duration from an ISO 8601 date string.
 * Returns "Xd Yh" format (e.g., "2d 3h", "0d 5h", "1d 0h").
 */
export const formatBlockedSince = (
	blockedSince: string | undefined | null,
	now: Date = new Date(),
): string | null => {
	if (!blockedSince) return null;

	const since = new Date(blockedSince);
	if (Number.isNaN(since.getTime())) return null;

	const diffMs = now.getTime() - since.getTime();
	if (diffMs < 0) return null;

	const totalHours = Math.floor(diffMs / (1000 * 60 * 60));
	const days = Math.floor(totalHours / 24);
	const hours = totalHours % 24;

	return `${days}d ${hours}h`;
};

import type {
	ConnectionHealthState,
	IConnectionHealth,
} from "../../../../services/Api/ConnectionHealthService";

const ACTIVITY_LABEL = "Activity";

/**
 * What an administrator reads instead of the state's own name. "Not checked yet" rather than
 * "Unknown" because the absence of evidence is the whole of what that state means, and a word that
 * sounds like a fault invites somebody to go looking for one.
 */
export const CONNECTION_STATE_WORDING: Record<ConnectionHealthState, string> = {
	Unknown: "Not checked yet",
	Healthy: "Healthy",
	Unreachable: "Unreachable",
	AuthenticationFailed: "Authentication failed",
};

/**
 * How each state is drawn. The fill and the shape carry it, not the colour: an icon set where
 * not-checked is a muted tick would let a connection nobody has asked about read as one that answered
 * yes, which is the defect the Unknown state was introduced to remove. A ring is an absence of evidence
 * and looks like one.
 */
export type ConnectionStateDrawing = "absent" | "confirmed" | "alarm";

export const CONNECTION_STATE_DRAWING: Record<
	ConnectionHealthState,
	ConnectionStateDrawing
> = {
	Unknown: "absent",
	Healthy: "confirmed",
	Unreachable: "alarm",
	AuthenticationFailed: "alarm",
};

export const isBroken = (connection: IConnectionHealth): boolean =>
	connection.state === "AuthenticationFailed" ||
	connection.state === "Unreachable";

/**
 * One icon has to answer both questions at a glance, so the colour is the worst thing there is to
 * say. A credential that was refused outranks a tracker that could not be reached: one of them is
 * something an administrator can go and fix.
 */
export const badgeColourFor = (
	connections: IConnectionHealth[],
): "primary" | "warning" | "error" => {
	if (connections.some((c) => c.state === "AuthenticationFailed")) {
		return "error";
	}

	return connections.some((c) => c.state === "Unreachable")
		? "warning"
		: "primary";
};

/**
 * The tooltip and the accessible name of the icon. Naming the connection is the point: "something is
 * wrong" sends an administrator into the popover to find out which one, which is the navigation this
 * surface exists to avoid.
 */
export const describeHeaderState = (
	connections: IConnectionHealth[],
): string => {
	const broken = connections.filter(isBroken);

	if (broken.length === 0) {
		return ACTIVITY_LABEL;
	}

	const named = broken
		.map((c) => `${c.connectionName} (${CONNECTION_STATE_WORDING[c.state]})`)
		.join(", ");

	return `${ACTIVITY_LABEL} — ${named}`;
};

export { ACTIVITY_LABEL };

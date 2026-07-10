export type TrendDirection = "up" | "down" | "flat" | "none";

export type TrendDetailRow = {
	readonly label: string;
	readonly currentValue: string;
	readonly previousValue: string;
};

export type TrendPayload = {
	readonly direction: TrendDirection;
	readonly metricLabel: string;
	readonly currentLabel?: string;
	readonly currentValue?: string;
	readonly previousLabel?: string;
	readonly previousValue?: string;
	readonly percentageDelta?: string;
	readonly detailRows?: readonly TrendDetailRow[];
	/**
	 * Set when a directional comparison cannot be computed because there is no previous-period
	 * baseline yet. The chrome renders a neutral "—" placeholder plus {@link hintText} instead of an
	 * arrow, so the widget reads as "awaiting baseline" rather than inert.
	 */
	readonly noBaseline?: boolean;
	/** Explanatory line shown in the trend tooltip (used to explain the {@link noBaseline} state). */
	readonly hintText?: string;
};

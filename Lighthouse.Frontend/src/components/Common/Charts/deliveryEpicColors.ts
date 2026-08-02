import type { DeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import { getColorMapForKeys } from "../../../utils/theme/colors";

/**
 * One colour per epic, shared by every chart on a delivery's Metrics tab.
 *
 * Keyed off the whole recorded breakdown rather than each chart's own subset: the fever chart drops
 * un-forecastable epics and the size chart drops sizeless ones, so a per-chart map would paint the
 * same epic two different colours on the same tab.
 */
export const deliveryEpicColors = (
	history: DeliveryMetricsHistory,
): Record<string, string> =>
	getColorMapForKeys(
		history.points.flatMap((point) =>
			point.featureBreakdown.map((metric) => metric.referenceId),
		),
	);

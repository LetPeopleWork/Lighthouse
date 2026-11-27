import type { MetricsOwnerKey } from "../../../hooks/metricsOwnerKey";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

interface WithinSleOverTimeWidgetProps {
	owner: MetricsOwnerKey;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	startDate: Date;
	endDate: Date;
}

const WithinSleOverTimeWidget = (
	_props: WithinSleOverTimeWidgetProps,
): never => {
	throw new Error("The Within SLE Over Time chart is not built yet.");
};

export default WithinSleOverTimeWidget;

import type { IFeature } from "../../models/Feature";
import {
	BaseMetricsService,
	type IProjectMetricsService,
} from "./MetricsService";

export class ProjectMetricsService
	extends BaseMetricsService<IFeature>
	implements IProjectMetricsService
{
	constructor() {
		super("projects");
	}
}

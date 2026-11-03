import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { parseRuleSet } from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import type { WidgetStatus } from "../../Common/MetricsView/widgetStatus";

interface PortfolioMetricsViewProps {
	portfolio: IPortfolio;
}

interface DoingColumns {
	readonly portfolioId: number;
	readonly status: WidgetStatus;
	readonly states: string[];
}

const PortfolioMetricsView: React.FC<PortfolioMetricsViewProps> = ({
	portfolio,
}) => {
	const [doingColumns, setDoingColumns] = useState<DoingColumns>();
	const [waitStates, setWaitStates] = useState<string[]>([]);
	const [stateMappings, setStateMappings] = useState<IStateMapping[]>([]);
	const [hasBlockedConfig, setHasBlockedConfig] = useState(false);
	const [stalenessThresholdDays, setStalenessThresholdDays] = useState<
		number | undefined
	>(undefined);
	const [blockedStalenessThresholdDays, setBlockedStalenessThresholdDays] =
		useState<number | undefined>(undefined);
	const [cycleTimeDefinitions, setCycleTimeDefinitions] = useState<
		ICycleTimeDefinition[]
	>([]);
	const { portfolioMetricsService, portfolioService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		let isCurrentPortfolio = true;
		const fetchPortfolioSettings = async () => {
			try {
				const settings = await portfolioService.getPortfolioSettings(
					portfolio.id,
				);
				if (!isCurrentPortfolio) return;
				setDoingColumns({
					portfolioId: portfolio.id,
					status: "ready",
					states: settings.doingDisplayStates ?? [],
				});
				setWaitStates(settings.waitStates ?? []);
				setStateMappings(settings.stateMappings);
				setHasBlockedConfig(
					(parseRuleSet(settings.blockedRuleSetJson)?.conditions.length ?? 0) >
						0,
				);
				setStalenessThresholdDays(settings.stalenessThresholdDays);
				setBlockedStalenessThresholdDays(
					settings.blockedStalenessThresholdDays,
				);
				setCycleTimeDefinitions(settings.cycleTimeDefinitions ?? []);
			} catch (error) {
				console.error("Error fetching portfolio settings:", error);
				if (isCurrentPortfolio) {
					setDoingColumns({
						portfolioId: portfolio.id,
						status: "error",
						states: [],
					});
				}
			}
		};

		fetchPortfolioSettings().catch((error) => {
			console.error("Error fetching portfolio settings:", error);
		});
		return () => {
			isCurrentPortfolio = false;
		};
	}, [portfolio.id, portfolioService]);

	// Until this portfolio's own answer arrives, the columns of a portfolio shown before are not its columns.
	const columnsOfThisPortfolio =
		doingColumns?.portfolioId === portfolio.id ? doingColumns : undefined;

	return (
		<BaseMetricsView
			entity={portfolio}
			metricsService={portfolioMetricsService}
			title={featuresTerm}
			defaultDateRange={90}
			hasBlockedConfig={hasBlockedConfig}
			doingStates={columnsOfThisPortfolio?.states ?? []}
			doingStatesStatus={columnsOfThisPortfolio?.status ?? "loading"}
			waitStates={waitStates}
			stateMappings={stateMappings}
			cycleTimeDefinitions={cycleTimeDefinitions}
			stalenessThresholdDays={stalenessThresholdDays}
			blockedStalenessThresholdDays={blockedStalenessThresholdDays}
		/>
	);
};

export default PortfolioMetricsView;

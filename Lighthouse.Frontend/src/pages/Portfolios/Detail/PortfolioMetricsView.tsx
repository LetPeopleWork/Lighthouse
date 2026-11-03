import type React from "react";
import { useCallback, useContext } from "react";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import { useOwnerMetricsSettings } from "../../Common/MetricsView/useOwnerMetricsSettings";

interface PortfolioMetricsViewProps {
	portfolio: IPortfolio;
}

const PortfolioMetricsView: React.FC<PortfolioMetricsViewProps> = ({
	portfolio,
}) => {
	const { portfolioMetricsService, portfolioService } =
		useContext(ApiServiceContext);
	const getPortfolioSettings = useCallback(
		(portfolioId: number) => portfolioService.getPortfolioSettings(portfolioId),
		[portfolioService],
	);
	const {
		doingStates,
		doingStatesStatus,
		waitStates,
		stateMappings,
		cycleTimeDefinitions,
		hasBlockedConfig,
		stalenessThresholdDays,
		blockedStalenessThresholdDays,
	} = useOwnerMetricsSettings(portfolio.id, getPortfolioSettings, "portfolio");

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	return (
		<BaseMetricsView
			entity={portfolio}
			metricsService={portfolioMetricsService}
			title={featuresTerm}
			defaultDateRange={90}
			hasBlockedConfig={hasBlockedConfig}
			doingStates={doingStates}
			doingStatesStatus={doingStatesStatus}
			waitStates={waitStates}
			stateMappings={stateMappings}
			cycleTimeDefinitions={cycleTimeDefinitions}
			stalenessThresholdDays={stalenessThresholdDays}
			blockedStalenessThresholdDays={blockedStalenessThresholdDays}
		/>
	);
};

export default PortfolioMetricsView;

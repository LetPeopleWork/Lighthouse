import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

interface PortfolioMetricsViewProps {
	portfolio: IPortfolio;
}

const PortfolioMetricsView: React.FC<PortfolioMetricsViewProps> = ({
	portfolio,
}) => {
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const [waitStates, setWaitStates] = useState<string[]>([]);
	const [stateMappings, setStateMappings] = useState<IStateMapping[]>([]);
	const [hasBlockedConfig, setHasBlockedConfig] = useState(false);
	const [stalenessThresholdDays, setStalenessThresholdDays] = useState<
		number | undefined
	>(undefined);
	const [cycleTimeDefinitions, setCycleTimeDefinitions] = useState<
		ICycleTimeDefinition[]
	>([]);
	const { portfolioMetricsService, portfolioService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		const fetchPortfolioSettings = async () => {
			try {
				const settings = await portfolioService.getPortfolioSettings(
					portfolio.id,
				);
				setDoingStates(settings.doingStates);
				setWaitStates(settings.waitStates ?? []);
				setStateMappings(settings.stateMappings);
				setHasBlockedConfig(
					settings.blockedStates.length > 0 || settings.blockedTags.length > 0,
				);
				setStalenessThresholdDays(settings.stalenessThresholdDays);
				setCycleTimeDefinitions(settings.cycleTimeDefinitions ?? []);
			} catch (err) {
				console.error("Error fetching portfolio settings:", err);
			}
		};

		fetchPortfolioSettings();
	}, [portfolio.id, portfolioService]);

	return (
		<BaseMetricsView
			entity={portfolio}
			metricsService={portfolioMetricsService}
			title={featuresTerm}
			defaultDateRange={90}
			hasBlockedConfig={hasBlockedConfig}
			doingStates={doingStates}
			waitStates={waitStates}
			stateMappings={stateMappings}
			cycleTimeDefinitions={cycleTimeDefinitions}
			stalenessThresholdDays={stalenessThresholdDays}
		/>
	);
};

export default PortfolioMetricsView;

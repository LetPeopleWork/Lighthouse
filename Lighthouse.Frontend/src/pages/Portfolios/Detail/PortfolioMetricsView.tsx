import type React from "react";
import { useContext, useEffect, useState } from "react";
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
	const [hasBlockedConfig, setHasBlockedConfig] = useState(false);
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
				setHasBlockedConfig(
					settings.blockedStates.length > 0 || settings.blockedTags.length > 0,
				);
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
		/>
	);
};

export default PortfolioMetricsView;

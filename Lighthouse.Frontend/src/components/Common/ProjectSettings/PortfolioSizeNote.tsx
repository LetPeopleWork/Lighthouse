import { Alert } from "@mui/material";
import Grid from "@mui/material/Grid";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

interface PortfolioSizeNoteProps {
	readonly portfolioId: number;
}

/** Says when the Portfolio holds more Features than its pages handle quickly, or that its size could not be read. Never blocks anything. */
const PortfolioSizeNote: React.FC<PortfolioSizeNoteProps> = ({
	portfolioId,
}) => {
	const { portfolioService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const size = useQuery({
		queryKey: ["portfolioSize", portfolioId],
		queryFn: () => portfolioService.getSize(portfolioId),
		enabled: portfolioId > 0,
		retry: false,
	});

	const portfolio = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const features = getTerm(TERMINOLOGY_KEYS.FEATURES);

	let message: string | null = null;
	if (size.isError) {
		message = `Couldn't check this ${portfolio}'s size.`;
	} else if (size.data?.isOverThreshold) {
		message = `This ${portfolio} holds ${size.data.featureCount} ${features} (as of its last update). Above ${size.data.threshold}, its pages and forecasts get slower. If you don't plan with all of them, narrow the query.`;
	}

	if (message === null) {
		return null;
	}

	return (
		<Grid size={{ xs: 12 }}>
			<Alert severity="info">{message}</Alert>
		</Grid>
	);
};

export default PortfolioSizeNote;

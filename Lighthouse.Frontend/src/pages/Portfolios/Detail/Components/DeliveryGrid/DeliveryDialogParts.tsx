import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Typography,
} from "@mui/material";
import type React from "react";
import { FeatureGrid } from "../../../../../components/Common/FeatureGrid";
import type { IFeature } from "../../../../../models/Feature";

export const LoadingSpinner: React.FC = () => (
	<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
		<CircularProgress />
	</Box>
);

export const SchemaLoadError: React.FC = () => (
	<Alert severity="warning">
		Failed to load rule schema. Please try again.
	</Alert>
);

export const PremiumFeatureNotice: React.FC<{ message: string }> = ({
	message,
}) => (
	<Alert severity="info" data-testid="premium-feature-notice">
		{message}
	</Alert>
);

export const ValidationButton: React.FC<{
	validatingRules: boolean;
	rulesLength: number;
	onClick: () => void;
}> = ({ validatingRules, rulesLength, onClick }) => (
	<Button
		variant="outlined"
		onClick={onClick}
		disabled={validatingRules || rulesLength === 0}
	>
		{validatingRules && <CircularProgress size={20} sx={{ mr: 1 }} />}
		Validate Rules
	</Button>
);

export const MatchedFeaturesAlert: React.FC<{ count: number }> = ({
	count,
}) => (
	<Alert severity="success" sx={{ flex: 1 }}>
		<span data-testid="matched-count">{count} feature(s) matched</span>
	</Alert>
);

export const MatchedFeaturesGrid: React.FC<{
	features: IFeature[];
	featuresTerm: string;
	portfolioId: number;
}> = ({ features, featuresTerm, portfolioId }) => (
	<Box sx={{ mt: 2 }}>
		<Typography variant="subtitle2" sx={{ mb: 1 }}>
			Matched {featuresTerm}:
		</Typography>
		<Box sx={{ height: 200 }}>
			<FeatureGrid
				features={features}
				selectedFeatureIds={features.map((f) => f.id)}
				storageKey={`delivery-matched-features-${portfolioId}`}
				mode="readonly"
			/>
		</Box>
	</Box>
);

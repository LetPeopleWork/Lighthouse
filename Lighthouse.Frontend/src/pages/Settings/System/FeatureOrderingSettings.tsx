import { Box, FormControlLabel, Switch, Typography } from "@mui/material";
import type React from "react";
import { useContext, useState } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import { useFeatureOrdering } from "../../../hooks/useFeatureOrdering";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

interface FeatureOrderingSettingsProps {
	isPremium: boolean;
}

/**
 * Settings → System → the switch that hands ordering ownership to this instance, and the help text
 * that says what giving it back does (AC-2.5, AC-5.5).
 */
const FeatureOrderingSettings: React.FC<FeatureOrderingSettingsProps> = ({
	isPremium,
}) => {
	const { policy, refresh } = useFeatureOrdering();
	const [isSaving, setIsSaving] = useState(false);

	const { settingsService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	const thisInstanceOwnsTheOrder = policy === "ManualOrder";

	const onToggle = async () => {
		const next = thisInstanceOwnsTheOrder ? "SourceOrder" : "ManualOrder";

		setIsSaving(true);
		try {
			await settingsService.updateFeatureOrdering(next);
			await refresh();
		} finally {
			setIsSaving(false);
		}
	};

	return (
		<Box>
			<LicenseTooltip
				canUseFeature={isPremium}
				premiumExtraInfo=""
				defaultTooltip=""
			>
				<FormControlLabel
					data-testid="feature-ordering-toggle"
					control={
						<Switch
							checked={thisInstanceOwnsTheOrder}
							disabled={!isPremium || isSaving}
							onChange={onToggle}
							color="primary"
						/>
					}
					label={`Let Lighthouse own the order of your ${featuresTerm}`}
				/>
			</LicenseTooltip>

			<Typography
				variant="body2"
				color="text.secondary"
				data-testid="feature-ordering-help-text"
			>
				{`While this is on, Lighthouse forecasts your ${featuresTerm} in the order you gave them, and a refresh from your work tracking system no longer re-sequences it. Turning it off hands the order straight back to your work tracking system — the places you chose are kept, so turning it on again restores them.`}
			</Typography>
		</Box>
	);
};

export default FeatureOrderingSettings;

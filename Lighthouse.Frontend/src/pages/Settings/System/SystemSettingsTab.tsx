import Box from "@mui/material/Box";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { TerminologyConfiguration } from "../../../components/TerminologyConfiguration";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import RefreshSettingUpdater from "../Refresh/RefreshSettingUpdater";
import BehaviourSettingsTable from "./BehaviourSettingsTable";
import BlackoutSettings from "./BlackoutSettings";

const SystemSettingsTab: React.FC = () => {
	const [optionalFeatures, setOptionalFeatures] = useState<IOptionalFeature[]>(
		[],
	);

	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);

	const { optionalFeatureService, licensingService } =
		useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	const fetchOptionalFeatures = useCallback(async () => {
		const optionalFeatureData = await optionalFeatureService.getAllFeatures();
		if (optionalFeatureData) {
			setOptionalFeatures(optionalFeatureData);
		}
	}, [optionalFeatureService]);

	const fetchLicenseStatus = useCallback(async () => {
		try {
			const licenseData = await licensingService.getLicenseStatus();
			setLicenseStatus(licenseData);
		} catch (error) {
			console.error("Failed to fetch license status:", error);
			setLicenseStatus(null);
		}
	}, [licensingService]);

	const onToggleOptionalFeature = async (toggledFeature: IOptionalFeature) => {
		// Matched on the key, because the backend keys these rows by it and hands every one of them the
		// same id - matching on the id would move every switch on the page at once.
		const updatedFeatures = optionalFeatures.map((feature) =>
			feature.key === toggledFeature.key
				? { ...feature, enabled: !feature.enabled }
				: feature,
		);
		setOptionalFeatures(updatedFeatures);

		try {
			await optionalFeatureService.updateFeature({
				...toggledFeature,
				enabled: !toggledFeature.enabled,
			});
		} catch {
			await fetchOptionalFeatures();
		}
	};

	useEffect(() => {
		fetchOptionalFeatures();
		fetchLicenseStatus();
	}, [fetchOptionalFeatures, fetchLicenseStatus]);

	return (
		<Box sx={{ mb: 4 }}>
			<InputGroup
				title="Blackout Periods & Recurring Rules"
				initiallyExpanded={true}
			>
				<BlackoutSettings isPremium={canUsePremiumFeatures} />
			</InputGroup>

			{optionalFeatures.length > 0 && (
				<InputGroup title="Behaviour Settings" initiallyExpanded={true}>
					<BehaviourSettingsTable
						settings={optionalFeatures}
						canUsePremiumFeatures={canUsePremiumFeatures}
						onToggle={onToggleOptionalFeature}
					/>
				</InputGroup>
			)}

			<InputGroup title="Terminology Configuration" initiallyExpanded={true}>
				<TerminologyConfiguration />
			</InputGroup>

			<InputGroup title={`${teamTerm} Refresh`}>
				<RefreshSettingUpdater title={teamTerm} settingName="Team" />
			</InputGroup>
			<InputGroup title={`${featureTerm} Refresh`}>
				<RefreshSettingUpdater title={featureTerm} settingName="Feature" />
			</InputGroup>
		</Box>
	);
};

export default SystemSettingsTab;

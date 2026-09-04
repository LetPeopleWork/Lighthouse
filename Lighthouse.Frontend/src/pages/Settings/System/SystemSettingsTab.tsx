import BiotechIcon from "@mui/icons-material/Biotech";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Switch from "@mui/material/Switch";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { TerminologyConfiguration } from "../../../components/TerminologyConfiguration";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { resolveTerms } from "../../../services/Terminology/resolveTerms";
import { useTerminology } from "../../../services/TerminologyContext";
import RefreshSettingUpdater from "../Refresh/RefreshSettingUpdater";
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
				<BlackoutSettings
					isPremium={licenseStatus?.canUsePremiumFeatures ?? false}
				/>
			</InputGroup>

			{optionalFeatures.length > 0 && (
				<InputGroup title="Behaviour Settings" initiallyExpanded={true}>
					<TableContainer>
						<Table data-testid="optional-features-table">
							<TableHead>
								<TableRow>
									<TableCell>Name</TableCell>
									<TableCell>Description</TableCell>
									<TableCell>Enabled</TableCell>
								</TableRow>
							</TableHead>
							<TableBody>
								{optionalFeatures.map((feature) => (
									<LicenseTooltip
										key={feature.key}
										canUseFeature={
											!feature.isPremium ||
											(licenseStatus?.canUsePremiumFeatures ?? false)
										}
										premiumExtraInfo=""
										defaultTooltip=""
									>
										<TableRow
											key={feature.key}
											data-testid={`feature-row-${feature.key}`}
										>
											<TableCell>
												<Box sx={{ display: "flex", alignItems: "center" }}>
													{resolveTerms(feature.name, getTerm)}
													{feature.isPreview && (
														<Tooltip title="This feature is in preview and may change or be removed in future versions">
															<Chip
																icon={<BiotechIcon />}
																label="Preview"
																size="small"
																color="warning"
																sx={{ ml: 1 }}
																data-testid={`${feature.key}-preview-indicator`}
															/>
														</Tooltip>
													)}
												</Box>
											</TableCell>
											<TableCell>
												{resolveTerms(feature.description, getTerm)}
											</TableCell>
											<TableCell>
												<Switch
													checked={feature.enabled}
													data-testid={`${feature.key}-toggle`}
													disabled={
														feature.isPremium &&
														!(licenseStatus?.canUsePremiumFeatures ?? false)
													}
													onChange={() => onToggleOptionalFeature(feature)}
													color="primary"
												/>
											</TableCell>
										</TableRow>
									</LicenseTooltip>
								))}
							</TableBody>
						</Table>
					</TableContainer>
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

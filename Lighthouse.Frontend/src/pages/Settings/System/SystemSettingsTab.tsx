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
import { useRbac } from "../../../hooks/useRbac";
import {
	type EncryptionKeyState,
	KEY_CUSTODY_WORDING,
} from "../../../models/Encryption/EncryptionKeyState";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import RefreshSettingUpdater from "../Refresh/RefreshSettingUpdater";
import BlackoutSettings from "./BlackoutSettings";
import FeatureOrderingSettings from "./FeatureOrderingSettings";

const EncryptionKeySection: React.FC<{ keyState: EncryptionKeyState }> = ({
	keyState,
}) => (
	<InputGroup title="Secret Encryption Key" initiallyExpanded={true}>
		<TableContainer>
			<Table data-testid="encryption-key-state">
				<TableBody>
					<TableRow>
						<TableCell>Key source</TableCell>
						<TableCell data-testid="encryption-key-custody">
							{KEY_CUSTODY_WORDING[keyState.custody]}
						</TableCell>
					</TableRow>
					<TableRow>
						<TableCell>Active key</TableCell>
						<TableCell data-testid="encryption-active-key-id">
							{keyState.activeKeyId}
						</TableCell>
					</TableRow>
				</TableBody>
			</Table>
		</TableContainer>
	</InputGroup>
);

const SystemSettingsTab: React.FC = () => {
	const [optionalFeatures, setOptionalFeatures] = useState<IOptionalFeature[]>(
		[],
	);

	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);

	// Where the key came from and what it is called is instance security posture, so it is read from
	// the surface only a System Administrator can reach, never from the system information response
	// that any signed-in viewer - including one inside an embedded frame - can already see.
	const [keyState, setKeyState] = useState<EncryptionKeyState | null>(null);

	const { optionalFeatureService, licensingService, encryptionService } =
		useContext(ApiServiceContext);

	const { isSystemAdmin } = useRbac();

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

	const fetchKeyState = useCallback(async () => {
		if (!isSystemAdmin) {
			setKeyState(null);
			return;
		}

		try {
			setKeyState(await encryptionService.getKeyState());
		} catch {
			setKeyState(null);
		}
	}, [encryptionService, isSystemAdmin]);

	const onToggleOptionalFeature = async (toggledFeature: IOptionalFeature) => {
		const updatedFeatures = optionalFeatures.map((feature) =>
			feature.id === toggledFeature.id
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
		fetchKeyState();
	}, [fetchOptionalFeatures, fetchLicenseStatus, fetchKeyState]);

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
				<InputGroup title="Optional Features" initiallyExpanded={true}>
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
										key={feature.id}
										canUseFeature={
											!feature.isPremium ||
											(licenseStatus?.canUsePremiumFeatures ?? false)
										}
										premiumExtraInfo=""
										defaultTooltip=""
									>
										<TableRow
											key={feature.id}
											data-testid={`feature-row-${feature.key}`}
										>
											<TableCell>
												<Box sx={{ display: "flex", alignItems: "center" }}>
													{feature.name}
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
											<TableCell>{feature.description}</TableCell>
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

			{keyState !== null && <EncryptionKeySection keyState={keyState} />}

			<InputGroup title={`${featureTerm} Order`} initiallyExpanded={true}>
				<FeatureOrderingSettings
					isPremium={licenseStatus?.canUsePremiumFeatures ?? false}
				/>
			</InputGroup>

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

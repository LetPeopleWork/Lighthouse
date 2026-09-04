import BiotechIcon from "@mui/icons-material/Biotech";
import WorkspacePremiumIcon from "@mui/icons-material/WorkspacePremium";
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
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { resolveTerms } from "../../../services/Terminology/resolveTerms";
import { useTerminology } from "../../../services/TerminologyContext";

export interface BehaviourSettingsTableProps {
	settings: IOptionalFeature[];
	canUsePremiumFeatures: boolean;
	onToggle: (setting: IOptionalFeature) => void;
}

const BehaviourSettingsTable: React.FC<BehaviourSettingsTableProps> = ({
	settings,
	canUsePremiumFeatures,
	onToggle,
}) => {
	const { getTerm } = useTerminology();

	return (
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
					{settings.map((setting) => {
						const isReachable = !setting.isPremium || canUsePremiumFeatures;

						return (
							<LicenseTooltip
								key={setting.key}
								canUseFeature={isReachable}
								premiumExtraInfo=""
								defaultTooltip=""
							>
								<TableRow data-testid={`feature-row-${setting.key}`}>
									<TableCell>
										<Box sx={{ display: "flex", alignItems: "center" }}>
											{resolveTerms(setting.name, getTerm)}
											{setting.isPreview && (
												<Tooltip title="This feature is in preview and may change or be removed in future versions">
													<Chip
														icon={<BiotechIcon />}
														label="Preview"
														size="small"
														color="warning"
														sx={{ ml: 1 }}
														data-testid={`${setting.key}-preview-indicator`}
													/>
												</Tooltip>
											)}
											{setting.isPremium && (
												<Tooltip title="This setting requires a premium license">
													<Chip
														icon={<WorkspacePremiumIcon />}
														label="Premium"
														size="small"
														color="primary"
														sx={{ ml: 1 }}
														data-testid={`${setting.key}-premium-indicator`}
													/>
												</Tooltip>
											)}
										</Box>
									</TableCell>
									<TableCell>
										{resolveTerms(setting.description, getTerm)}
									</TableCell>
									<TableCell>
										<Switch
											checked={setting.enabled}
											data-testid={`${setting.key}-toggle`}
											disabled={!isReachable}
											onChange={() => onToggle(setting)}
											color="primary"
										/>
									</TableCell>
								</TableRow>
							</LicenseTooltip>
						);
					})}
				</TableBody>
			</Table>
		</TableContainer>
	);
};

export default BehaviourSettingsTable;

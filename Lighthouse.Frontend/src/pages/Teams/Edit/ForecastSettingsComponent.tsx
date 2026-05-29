import {
	FormControlLabel,
	Link,
	Switch,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import type { DeliveryRuleGroupMode } from "../../../components/Common/DeliveryRuleBuilder/types";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import ReloadDependentDataAction from "../../../components/Common/StateMappings/ReloadDependentDataAction";
import ForecastFilterEditor from "../../../components/Teams/ForecastFilterEditor/ForecastFilterEditor";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { SaveState } from "../../../hooks/useModifySettings";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItemRuleCondition } from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

const PREMIUM_DOCS_HREF = "/docs/premium-features#forecast-filter";
const RULE_SET_SCHEMA_VERSION = 1;

interface RuleSetData {
	rules: IWorkItemRuleCondition[];
	mode: DeliveryRuleGroupMode;
}

const parseRuleSetFromJson = (json: string | null | undefined): RuleSetData => {
	if (!json || json.trim() === "") {
		return { rules: [], mode: "and" };
	}
	try {
		const parsed = JSON.parse(json) as {
			conditions?: IWorkItemRuleCondition[];
			mode?: string;
		};
		const mode: DeliveryRuleGroupMode =
			parsed.mode?.toLowerCase() === "or" ? "or" : "and";
		return { rules: parsed.conditions ?? [], mode };
	} catch {
		return { rules: [], mode: "and" };
	}
};

const serializeRuleSetToJson = (data: RuleSetData): string => {
	return JSON.stringify({
		version: RULE_SET_SCHEMA_VERSION,
		mode: data.mode,
		conditions: data.rules,
	});
};

interface PremiumGatedForecastFilterProps {
	teamId: number;
	rules: IWorkItemRuleCondition[];
	mode: DeliveryRuleGroupMode;
	onRulesChange: (rules: IWorkItemRuleCondition[]) => void;
	onModeChange: (mode: DeliveryRuleGroupMode) => void;
	saveState: SaveState;
}

const PremiumGatedForecastFilter: React.FC<PremiumGatedForecastFilterProps> = ({
	teamId,
	rules,
	mode,
	onRulesChange,
	onModeChange,
	saveState,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? true;
	const { getTerm } = useTerminology();
	const { teamService } = useContext(ApiServiceContext);
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const heading = `Exclude Items for ${throughputTerm}`;
	const reloadThroughput = () => {
		void teamService.updateTeamData(teamId);
	};

	return (
		<Grid size={{ xs: 12 }}>
			<LicenseTooltip
				canUseFeature={isPremium}
				defaultTooltip=""
				premiumExtraInfo={`${heading} excludes selected work items from the ${throughputTerm.toLowerCase()} data used for forecasts.`}
			>
				<Typography
					variant="h6"
					component="h3"
					sx={{ display: "inline-block" }}
				>
					{heading}
				</Typography>
			</LicenseTooltip>
			{isPremium ? (
				<>
					<ForecastFilterEditor
						teamId={teamId}
						rules={rules}
						mode={mode}
						onChange={onRulesChange}
						onModeChange={onModeChange}
					/>
					<ReloadDependentDataAction
						visible={saveState === "saved"}
						label={`Reload ${throughputTerm.toLowerCase()} now`}
						onReload={reloadThroughput}
					/>
				</>
			) : (
				<Typography variant="body2" sx={{ mt: 1 }}>
					Available with a <Link href={PREMIUM_DOCS_HREF}>premium license</Link>
					.
				</Typography>
			)}
		</Grid>
	);
};

interface ForecastSettingsComponentProps {
	teamSettings: ITeamSettings | null;
	isDefaultSettings: boolean;
	onTeamSettingsChange: (
		key: keyof ITeamSettings,
		value: string | number | boolean | Date | null,
	) => void;
	saveState?: SaveState;
}

const ForecastSettingsComponent: React.FC<ForecastSettingsComponentProps> = ({
	teamSettings,
	isDefaultSettings,
	onTeamSettingsChange,
	saveState = "idle",
}) => {
	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);

	const handleDateChange = (name: keyof ITeamSettings, newDate: string) => {
		onTeamSettingsChange(name, new Date(newDate));
	};

	return (
		<InputGroup title={"Forecast Configuration"}>
			{!isDefaultSettings && (
				<Grid size={{ xs: 12 }}>
					<FormControlLabel
						control={
							<Switch
								checked={teamSettings?.useFixedDatesForThroughput ?? false}
								onChange={(e) =>
									onTeamSettingsChange(
										"useFixedDatesForThroughput",
										e.target.checked,
									)
								}
							/>
						}
						label={`Use Fixed Dates for ${throughputTerm}`}
					/>
				</Grid>
			)}

			{teamSettings?.useFixedDatesForThroughput ? (
				<Grid size={{ xs: 12, md: 12 }}>
					<TextField
						label="Start Date"
						type="date"
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								max: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000)
									.toISOString()
									.slice(0, 10),
							},
						}}
						defaultValue={teamSettings.throughputHistoryStartDate
							.toISOString()
							.slice(0, 10)}
						onChange={(e) =>
							handleDateChange("throughputHistoryStartDate", e.target.value)
						}
					/>
					<TextField
						label="End Date"
						type="date"
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								min: new Date(
									teamSettings.throughputHistoryStartDate.getTime() +
										10 * 24 * 60 * 60 * 1000,
								)
									.toISOString()
									.slice(0, 10),
								max: new Date().toISOString().slice(0, 10),
							},
						}}
						defaultValue={teamSettings.throughputHistoryEndDate
							.toISOString()
							.slice(0, 10)}
						onChange={(e) =>
							handleDateChange("throughputHistoryEndDate", e.target.value)
						}
					/>
				</Grid>
			) : (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={`${throughputTerm} History`}
						type="number"
						fullWidth
						margin="normal"
						value={teamSettings?.throughputHistory ?? ""}
						onChange={(e) =>
							onTeamSettingsChange(
								"throughputHistory",
								Number.parseInt(e.target.value, 10),
							)
						}
					/>
				</Grid>
			)}

			{!isDefaultSettings &&
				teamSettings &&
				(() => {
					const currentRuleSet = parseRuleSetFromJson(
						teamSettings.forecastFilterRuleSetJson,
					);
					const persistRuleSet = (next: RuleSetData) => {
						onTeamSettingsChange(
							"forecastFilterRuleSetJson",
							next.rules.length === 0 ? null : serializeRuleSetToJson(next),
						);
					};
					return (
						<PremiumGatedForecastFilter
							teamId={teamSettings.id}
							rules={currentRuleSet.rules}
							mode={currentRuleSet.mode}
							onRulesChange={(rules) =>
								persistRuleSet({ rules, mode: currentRuleSet.mode })
							}
							onModeChange={(mode) =>
								persistRuleSet({ rules: currentRuleSet.rules, mode })
							}
							saveState={saveState}
						/>
					);
				})()}
		</InputGroup>
	);
};

export default ForecastSettingsComponent;

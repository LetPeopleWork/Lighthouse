import {
	FormControlLabel,
	Link,
	Switch,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import ForecastFilterEditor from "../../../components/Teams/ForecastFilterEditor/ForecastFilterEditor";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItemRuleCondition } from "../../../models/WorkItemRules";
import { useTerminology } from "../../../services/TerminologyContext";

const PREMIUM_DOCS_HREF = "/docs/premium-features#forecast-filter";
const RULE_SET_SCHEMA_VERSION = 1;

const parseRulesFromJson = (
	json: string | null | undefined,
): IWorkItemRuleCondition[] => {
	if (!json || json.trim() === "") {
		return [];
	}
	try {
		const parsed = JSON.parse(json) as {
			conditions?: IWorkItemRuleCondition[];
		};
		return parsed.conditions ?? [];
	} catch {
		return [];
	}
};

const serializeRulesToJson = (rules: IWorkItemRuleCondition[]): string => {
	return JSON.stringify({
		version: RULE_SET_SCHEMA_VERSION,
		conditions: rules,
	});
};

interface PremiumGatedForecastFilterProps {
	teamId: number;
	rules: IWorkItemRuleCondition[];
	onRulesChange: (rules: IWorkItemRuleCondition[]) => void;
}

const PremiumGatedForecastFilter: React.FC<PremiumGatedForecastFilterProps> = ({
	teamId,
	rules,
	onRulesChange,
}) => {
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? true;

	return (
		<Grid size={{ xs: 12 }}>
			<Typography variant="h6" component="h3">
				Forecast Filter (Premium)
			</Typography>
			{isPremium ? (
				<ForecastFilterEditor
					teamId={teamId}
					rules={rules}
					onChange={onRulesChange}
				/>
			) : (
				<Typography variant="body2">
					Forecast Filter is a Premium feature.{" "}
					<Link href={PREMIUM_DOCS_HREF}>Learn more.</Link>
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
}

const ForecastSettingsComponent: React.FC<ForecastSettingsComponentProps> = ({
	teamSettings,
	isDefaultSettings,
	onTeamSettingsChange,
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
								// Max date is 10 days from today
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
								// Min date should be 10 days after start date
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
							.slice(0, 10)} // Convert date to yyyy-MM-dd format
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

			{!isDefaultSettings && teamSettings && (
				<PremiumGatedForecastFilter
					teamId={teamSettings.id}
					rules={parseRulesFromJson(teamSettings.forecastFilterRuleSetJson)}
					onRulesChange={(rules) =>
						onTeamSettingsChange(
							"forecastFilterRuleSetJson",
							rules.length === 0 ? null : serializeRulesToJson(rules),
						)
					}
				/>
			)}
		</InputGroup>
	);
};

export default ForecastSettingsComponent;

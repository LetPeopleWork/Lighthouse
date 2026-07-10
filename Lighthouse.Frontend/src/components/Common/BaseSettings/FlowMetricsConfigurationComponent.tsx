import WarningIcon from "@mui/icons-material/Warning";
import {
	Button,
	Checkbox,
	FormControlLabel,
	Switch,
	TextField,
	Tooltip,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import {
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
import { useRbac } from "../../../hooks/useRbac";
import {
	BLOCKED_RULE_SET_SCHEMA_VERSION,
	type IBaseSettings,
	parseBlockedRuleSet,
	serializeBlockedRuleSet,
} from "../../../models/Common/BaseSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type {
	IWorkItemRuleCondition,
	IWorkItemRuleSchema,
} from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { DeliveryRuleBuilder } from "../DeliveryRuleBuilder/DeliveryRuleBuilder";
import type { DeliveryRuleGroupMode } from "../DeliveryRuleBuilder/types";
import InputGroup from "../InputGroup/InputGroup";

interface FlowMetricsConfigurationComponentProps<T extends IBaseSettings> {
	settings: T;
	onSettingsChange: (
		key: keyof T,
		value: number | boolean | string | string[] | Date | null,
	) => void;
	showFeatureWip?: boolean;
	stalenessSeedDefault: number;
	blockedStalenessSeedDefault?: number;
}

const FlowMetricsConfigurationComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
	showFeatureWip = false,
	stalenessSeedDefault,
	blockedStalenessSeedDefault = 5,
}: FlowMetricsConfigurationComponentProps<T>) => {
	const [isSleEnabled, setIsSleEnabled] = useState(false);
	const [isWipLimitEnabled, setIsWipLimitEnabled] = useState(false);
	const [isFeatureWipEnabled, setIsFeatureWipEnabled] = useState(false);
	// Lazy-initialized once from the settings this component mounted with (the
	// owning page only renders it after the settings fetch resolves, and remounts
	// on owner switch — see the resync-effect comment below for why this must NOT
	// also be recomputed on every settings change).
	const [isBlockedItemsEnabled, setIsBlockedItemsEnabled] = useState(
		() =>
			(parseBlockedRuleSet(settings.blockedRuleSetJson)?.conditions.length ??
				0) > 0 ||
			(settings.blockedTags && settings.blockedTags.length > 0) ||
			(settings.blockedStates && settings.blockedStates.length > 0),
	);
	const [isStalenessEnabled, setIsStalenessEnabled] = useState(false);
	const [isBlockedStalenessEnabled, setIsBlockedStalenessEnabled] =
		useState(false);
	const [isBaselineEnabled, setIsBaselineEnabled] = useState(false);
	const [probabilityInputValue, setProbabilityInputValue] =
		useState<string>("");

	const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);

	const { teamService, deliveryService } = useContext(ApiServiceContext);
	const { isTeamAdmin, isPortfolioAdmin } = useRbac();
	const [blockedSchema, setBlockedSchema] =
		useState<IWorkItemRuleSchema | null>(null);

	// Teams pass showFeatureWip; portfolios do not — this is the owner discriminator
	// that already flows into the shared component from both settings pages.
	const ownerIsTeam = showFeatureWip;
	const ownerId = settings.id;
	const canEditBlockedRules = ownerIsTeam
		? isTeamAdmin(ownerId)
		: isPortfolioAdmin(ownerId);

	const blockedRuleSet = useMemo(
		() =>
			parseBlockedRuleSet(settings.blockedRuleSetJson) ?? {
				version: BLOCKED_RULE_SET_SCHEMA_VERSION,
				mode: "and" as DeliveryRuleGroupMode,
				conditions: [],
			},
		[settings.blockedRuleSetJson],
	);

	const { getTerm } = useTerminology();
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);

	useEffect(() => {
		setIsSleEnabled(
			settings.serviceLevelExpectationProbability > 50 &&
				settings.serviceLevelExpectationRange >= 0,
		);
		setIsWipLimitEnabled(settings.systemWIPLimit > 0);
		setIsFeatureWipEnabled(
			showFeatureWip &&
				"featureWIP" in settings &&
				Number(settings.featureWIP) > 0,
		);
		// isBlockedItemsEnabled is deliberately NOT resynced here — see its
		// lazy useState initializer above. Clearing rule rows down to zero
		// (persistBlockedRuleSet) mutates settings.blockedRuleSetJson/blockedTags/
		// blockedStates the same way an explicit "disable" does, so recomputing
		// this flag from settings on every change would hide the whole rule
		// editor mid-edit the moment a config admin deletes the last row before
		// adding a replacement — exactly the flow the BlockedItems E2E walking
		// skeleton exercises.
		setIsStalenessEnabled(settings.stalenessThresholdDays > 0);
		setIsBlockedStalenessEnabled(settings.blockedStalenessThresholdDays > 0);
		setIsBaselineEnabled(
			settings.processBehaviourChartBaselineStartDate != null &&
				settings.processBehaviourChartBaselineEndDate != null,
		);

		// Initialize probability input value with current setting
		setProbabilityInputValue(
			settings.serviceLevelExpectationProbability.toString(),
		);
	}, [settings, showFeatureWip]);

	// Fetch the rule schema for the blocked rule builder, mirroring how
	// ForecastFilterEditor consumes the shared DeliveryRuleBuilder. Only config
	// admins ever fetch/render the editor.
	useEffect(() => {
		if (!isBlockedItemsEnabled || !canEditBlockedRules) {
			return;
		}

		let cancelled = false;
		const request = ownerIsTeam
			? teamService.getForecastFilterSchema(ownerId)
			: deliveryService.getRuleSchema(ownerId);
		request
			.then((data) => {
				if (!cancelled) {
					setBlockedSchema(data);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setBlockedSchema(null);
				}
			});

		return () => {
			cancelled = true;
		};
	}, [
		isBlockedItemsEnabled,
		canEditBlockedRules,
		ownerIsTeam,
		ownerId,
		teamService,
		deliveryService,
	]);

	// Cleanup timeout on unmount
	useEffect(() => {
		return () => {
			if (debounceTimeoutRef.current) {
				clearTimeout(debounceTimeoutRef.current);
			}
		};
	}, []);

	const handleWipLimitEnableChange = (checked: boolean) => {
		setIsWipLimitEnabled(checked);

		if (checked) {
			onSettingsChange("systemWIPLimit", 1);
		} else {
			onSettingsChange("systemWIPLimit", 0);
		}
	};

	const handleWipLimitChange = (value: string) => {
		const newLimit = Number.parseInt(value, 10);
		if (!Number.isNaN(newLimit)) {
			onSettingsChange("systemWIPLimit", newLimit);
		}
	};

	const handleFeatureWipEnableChange = (checked: boolean) => {
		setIsFeatureWipEnabled(checked);

		if ("featureWIP" in settings) {
			if (checked) {
				onSettingsChange("featureWIP" as keyof T, 1);
			} else {
				onSettingsChange("featureWIP" as keyof T, 0);
				onSettingsChange("automaticallyAdjustFeatureWIP" as keyof T, false);
			}
		}
	};

	const handleFeatureWipChange = (value: string) => {
		const newFeatureWip = Number.parseInt(value, 10);

		if (!Number.isNaN(newFeatureWip) && "featureWIP" in settings) {
			onSettingsChange("featureWIP" as keyof T, newFeatureWip);
		}
	};

	const handleSleEnableChange = (checked: boolean) => {
		setIsSleEnabled(checked);

		// Clear any pending debounced updates
		if (debounceTimeoutRef.current) {
			clearTimeout(debounceTimeoutRef.current);
			debounceTimeoutRef.current = null;
		}

		if (checked) {
			onSettingsChange("serviceLevelExpectationProbability", 70);
			onSettingsChange("serviceLevelExpectationRange", 10);
			setProbabilityInputValue("70");
		} else {
			onSettingsChange("serviceLevelExpectationProbability", 0);
			onSettingsChange("serviceLevelExpectationRange", 0);
			setProbabilityInputValue("0");
		}
	};

	const debouncedProbabilityChange = useCallback(
		(value: string) => {
			if (debounceTimeoutRef.current) {
				clearTimeout(debounceTimeoutRef.current);
			}

			debounceTimeoutRef.current = setTimeout(() => {
				const numValue = Number.parseInt(value, 10);
				if (!Number.isNaN(numValue)) {
					onSettingsChange("serviceLevelExpectationProbability", numValue);
				}
			}, 300); // 300ms delay for responsive user experience
		},
		[onSettingsChange],
	);

	const handleProbabilityChange = (value: string) => {
		setProbabilityInputValue(value);
		debouncedProbabilityChange(value);
	};

	const handleRangeChange = (value: string) => {
		const numValue = Number.parseInt(value, 10);

		if (!Number.isNaN(numValue)) {
			onSettingsChange("serviceLevelExpectationRange", numValue);
		}
	};

	const persistBlockedRuleSet = (
		conditions: IWorkItemRuleCondition[],
		mode: DeliveryRuleGroupMode,
	) => {
		if (conditions.length === 0) {
			// Clear legacy fields alongside the rule set so the backend's
			// GetEffectiveRuleSetJson doesn't synthesize rules from leftover
			// BlockedTags/BlockedStates when BlockedRuleSetJson is null.
			onSettingsChange("blockedRuleSetJson" as keyof T, null);
			onSettingsChange("blockedTags" as keyof T, []);
			onSettingsChange("blockedStates" as keyof T, []);
		} else {
			onSettingsChange(
				"blockedRuleSetJson" as keyof T,
				serializeBlockedRuleSet({
					version: BLOCKED_RULE_SET_SCHEMA_VERSION,
					mode,
					conditions,
				}),
			);
		}
	};

	const handleBlockedRulesChange = (conditions: IWorkItemRuleCondition[]) => {
		persistBlockedRuleSet(conditions, blockedRuleSet.mode);
	};

	const handleBlockedRulesModeChange = (mode: DeliveryRuleGroupMode) => {
		persistBlockedRuleSet(blockedRuleSet.conditions, mode);
	};

	const handleBlockedItemsEnableChange = (checked: boolean) => {
		setIsBlockedItemsEnabled(checked);

		if (!checked) {
			// Clear the rule-based definition; legacy fields remain expand-only until the
			// backend cleanup migration but are emptied so no stale blocked signal lingers.
			onSettingsChange("blockedRuleSetJson" as keyof T, null);
			onSettingsChange("blockedTags" as keyof T, []);
			onSettingsChange("blockedStates" as keyof T, []);
		}
	};

	const handleStalenessEnableChange = (checked: boolean) => {
		setIsStalenessEnabled(checked);

		onSettingsChange(
			"stalenessThresholdDays" as keyof T,
			checked ? stalenessSeedDefault : 0,
		);
	};

	const handleStalenessThresholdChange = (value: string) => {
		const newThreshold = Number.parseInt(value, 10);
		if (!Number.isNaN(newThreshold)) {
			onSettingsChange("stalenessThresholdDays" as keyof T, newThreshold);
		}
	};

	const handleBlockedStalenessEnableChange = (checked: boolean) => {
		setIsBlockedStalenessEnabled(checked);

		onSettingsChange(
			"blockedStalenessThresholdDays" as keyof T,
			checked ? blockedStalenessSeedDefault : 0,
		);
	};

	const handleBlockedStalenessThresholdChange = (value: string) => {
		const newThreshold = Number.parseInt(value, 10);
		if (!Number.isNaN(newThreshold)) {
			onSettingsChange(
				"blockedStalenessThresholdDays" as keyof T,
				newThreshold,
			);
		}
	};

	const handleBaselineToggle = (enabled: boolean) => {
		setIsBaselineEnabled(enabled);

		if (enabled) {
			const endDate = new Date();
			endDate.setDate(endDate.getDate() - 1);
			const startDate = new Date();
			startDate.setDate(startDate.getDate() - 15);
			onSettingsChange(
				"processBehaviourChartBaselineStartDate" as keyof T,
				startDate,
			);
			onSettingsChange(
				"processBehaviourChartBaselineEndDate" as keyof T,
				endDate,
			);
		} else {
			onSettingsChange(
				"processBehaviourChartBaselineStartDate" as keyof T,
				null,
			);
			onSettingsChange("processBehaviourChartBaselineEndDate" as keyof T, null);
		}
	};

	const handleBaselineClear = () => {
		setIsBaselineEnabled(false);
		onSettingsChange("processBehaviourChartBaselineStartDate" as keyof T, null);
		onSettingsChange("processBehaviourChartBaselineEndDate" as keyof T, null);
	};

	return (
		<InputGroup title={"Flow Metrics Configuration"} initiallyExpanded={false}>
			{/* System WIP Limit Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isWipLimitEnabled}
							onChange={(e) => handleWipLimitEnableChange(e.target.checked)}
						/>
					}
					label={`Set System ${wipTerm} Limit`}
				/>
			</Grid>
			{isWipLimitEnabled && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={`${wipTerm} Limit`}
						type="number"
						fullWidth
						margin="normal"
						value={settings.systemWIPLimit}
						slotProps={{
							htmlInput: {
								min: 1,
								step: 1,
							},
						}}
						helperText="Must be at least 1"
						onChange={(e) => handleWipLimitChange(e.target.value)}
					/>
				</Grid>
			)}

			{/* Feature WIP Configuration */}
			{showFeatureWip && (
				<>
					<Grid size={{ xs: 12 }}>
						<FormControlLabel
							control={
								<Checkbox
									checked={isFeatureWipEnabled}
									onChange={(e) =>
										handleFeatureWipEnableChange(e.target.checked)
									}
								/>
							}
							label={`Set ${featureTerm} ${wipTerm}`}
						/>
					</Grid>
					{isFeatureWipEnabled && (
						<>
							<Grid size={{ xs: 12 }}>
								<TextField
									label={`${featureTerm} ${wipTerm}`}
									type="number"
									fullWidth
									margin="normal"
									value={"featureWIP" in settings ? settings.featureWIP : 0}
									slotProps={{
										htmlInput: {
											min: 1,
											step: 1,
										},
									}}
									helperText="Must be at least 1"
									onChange={(e) => handleFeatureWipChange(e.target.value)}
								/>
							</Grid>
							<Grid size={{ xs: 12 }}>
								<FormControlLabel
									control={
										<Checkbox
											checked={
												"automaticallyAdjustFeatureWIP" in settings
													? Boolean(settings.automaticallyAdjustFeatureWIP)
													: false
											}
											onChange={(e) => {
												onSettingsChange(
													"automaticallyAdjustFeatureWIP" as keyof T,
													e.target.checked,
												);
											}}
										/>
									}
									label={`Automatically Adjust ${featureTerm} ${wipTerm} based on actual ${wipTerm}`}
								/>
							</Grid>
						</>
					)}
				</>
			)}

			{/* Service Level Expectation Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isSleEnabled}
							onChange={(e) => handleSleEnableChange(e.target.checked)}
						/>
					}
					label={`Set ${serviceLevelExpectationTerm}`}
				/>
			</Grid>
			{isSleEnabled && (
				<>
					<Grid size={{ xs: 12 }}>
						<TextField
							label="Probability (%)"
							type="number"
							fullWidth
							margin="normal"
							value={probabilityInputValue}
							slotProps={{
								htmlInput: {
									min: 50,
									max: 95,
									step: 1,
								},
							}}
							helperText="Must be between 50 and 95"
							onChange={(e) => handleProbabilityChange(e.target.value)}
						/>
					</Grid>
					<Grid size={{ xs: 12 }}>
						<TextField
							label="Range (in days)"
							type="number"
							fullWidth
							margin="normal"
							value={settings.serviceLevelExpectationRange}
							slotProps={{
								htmlInput: {
									min: 1,
									step: 1,
								},
							}}
							helperText="Must be at least 1"
							onChange={(e) => handleRangeChange(e.target.value)}
						/>
					</Grid>
				</>
			)}

			{/* Blocked Items Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isBlockedItemsEnabled}
							onChange={(e) => handleBlockedItemsEnableChange(e.target.checked)}
						/>
					}
					label={`Configure ${blockedTerm} ${workItemsTerm}`}
				/>
			</Grid>

			{/* Blocked Rule Set — reuses the shared DeliveryRuleBuilder (third consumer,
			    mirroring ForecastFilterEditor). Only config admins see/edit the rules. */}
			{isBlockedItemsEnabled && canEditBlockedRules && blockedSchema && (
				<Grid size={{ xs: 12 }}>
					<InputGroup
						title={`${blockedTerm} ${workItemsTerm} Rules`}
						initiallyExpanded={true}
					>
						<Grid size={{ xs: 12 }}>
							<DeliveryRuleBuilder
								rules={blockedRuleSet.conditions}
								onChange={handleBlockedRulesChange}
								fields={blockedSchema.fields}
								operators={blockedSchema.operators}
								maxRules={blockedSchema.maxRules}
								maxValueLength={blockedSchema.maxValueLength}
								mode={blockedRuleSet.mode}
								onModeChange={handleBlockedRulesModeChange}
								title={`Mark ${workItemsTerm.toLowerCase()} as ${blockedTerm.toLowerCase()} where…`}
								emptyStateMessage={`Add at least one rule to mark ${workItemsTerm.toLowerCase()} as ${blockedTerm.toLowerCase()}.`}
							/>
						</Grid>
					</InputGroup>
				</Grid>
			)}

			{/* Staleness Threshold Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isStalenessEnabled}
							onChange={(e) => handleStalenessEnableChange(e.target.checked)}
						/>
					}
					label="Set Staleness Threshold"
				/>
			</Grid>
			{isStalenessEnabled && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label="Staleness Threshold (days)"
						type="number"
						fullWidth
						margin="normal"
						value={settings.stalenessThresholdDays}
						slotProps={{
							htmlInput: {
								min: 0,
								max: 365,
								step: 1,
							},
						}}
						onChange={(e) => handleStalenessThresholdChange(e.target.value)}
					/>
				</Grid>
			)}

			{/* Blocked Staleness Threshold Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isBlockedStalenessEnabled}
							onChange={(e) =>
								handleBlockedStalenessEnableChange(e.target.checked)
							}
						/>
					}
					label={`Set Blocked ${workItemsTerm} Staleness Threshold`}
				/>
			</Grid>
			{isBlockedStalenessEnabled && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={`Blocked ${workItemsTerm} Staleness Threshold (days)`}
						type="number"
						fullWidth
						margin="normal"
						value={settings.blockedStalenessThresholdDays}
						slotProps={{
							htmlInput: {
								min: 0,
								max: 365,
								step: 1,
							},
						}}
						onChange={(e) =>
							handleBlockedStalenessThresholdChange(e.target.value)
						}
					/>
				</Grid>
			)}

			{/* Process Behaviour Chart Baseline Configuration */}
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Switch
							checked={isBaselineEnabled}
							onChange={(e) => handleBaselineToggle(e.target.checked)}
						/>
					}
					label="Set Baseline for Process Behaviour Chart"
				/>
				{isBaselineEnabled &&
					settings.doneItemsCutoffDays > 0 &&
					settings.processBehaviourChartBaselineStartDate != null &&
					settings.processBehaviourChartBaselineStartDate <
						new Date(
							Date.now() - settings.doneItemsCutoffDays * 24 * 60 * 60 * 1000,
						) && (
						<Tooltip title="Work items older than the cutoff may be trimmed. This can affect done/resolved-item metrics (e.g., throughput and cycle time) and may make the baseline misleading.">
							<WarningIcon
								color="warning"
								aria-label="Baseline cutoff warning"
								sx={{ ml: 1, verticalAlign: "middle" }}
							/>
						</Tooltip>
					)}
			</Grid>
			{isBaselineEnabled && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label="PBC Baseline Start"
						type="date"
						sx={{ mr: 2 }}
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								max: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000)
									.toISOString()
									.slice(0, 10),
							},
						}}
						defaultValue={
							settings.processBehaviourChartBaselineStartDate
								?.toISOString()
								.slice(0, 10) ?? ""
						}
						onChange={(e) =>
							onSettingsChange(
								"processBehaviourChartBaselineStartDate" as keyof T,
								new Date(e.target.value),
							)
						}
					/>
					<TextField
						label="PBC Baseline End"
						type="date"
						slotProps={{
							inputLabel: { shrink: true },
							htmlInput: {
								min: settings.processBehaviourChartBaselineStartDate
									? new Date(
											settings.processBehaviourChartBaselineStartDate.getTime() +
												14 * 24 * 60 * 60 * 1000,
										)
											.toISOString()
											.slice(0, 10)
									: undefined,
								max: new Date().toISOString().slice(0, 10),
							},
						}}
						defaultValue={
							settings.processBehaviourChartBaselineEndDate
								?.toISOString()
								.slice(0, 10) ?? ""
						}
						onChange={(e) =>
							onSettingsChange(
								"processBehaviourChartBaselineEndDate" as keyof T,
								new Date(e.target.value),
							)
						}
					/>
					<Button
						variant="outlined"
						color="warning"
						size="small"
						sx={{ ml: 2, verticalAlign: "bottom", mb: "3px" }}
						onClick={handleBaselineClear}
						aria-label="Clear Baseline"
					>
						Clear Baseline
					</Button>
				</Grid>
			)}
		</InputGroup>
	);
};

export default FlowMetricsConfigurationComponent;

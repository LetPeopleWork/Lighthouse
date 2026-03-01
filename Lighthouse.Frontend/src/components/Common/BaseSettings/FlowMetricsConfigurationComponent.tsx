import WarningIcon from "@mui/icons-material/Warning";
import {
	Box,
	Button,
	Checkbox,
	FormControlLabel,
	Switch,
	TextField,
	Tooltip,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import { useCallback, useEffect, useRef, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface FlowMetricsConfigurationComponentProps<T extends IBaseSettings> {
	settings: T;
	onSettingsChange: (
		key: keyof T,
		value: number | boolean | string[] | Date | null,
	) => void;
	showFeatureWip?: boolean;
}

const FlowMetricsConfigurationComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
	showFeatureWip = false,
}: FlowMetricsConfigurationComponentProps<T>) => {
	const [isSleEnabled, setIsSleEnabled] = useState(false);
	const [isWipLimitEnabled, setIsWipLimitEnabled] = useState(false);
	const [isFeatureWipEnabled, setIsFeatureWipEnabled] = useState(false);
	const [isBlockedItemsEnabled, setIsBlockedItemsEnabled] = useState(false);
	const [isBaselineEnabled, setIsBaselineEnabled] = useState(false);
	const [probabilityInputValue, setProbabilityInputValue] =
		useState<string>("");

	const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);

	const { getTerm } = useTerminology();
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const tagTerm = getTerm(TERMINOLOGY_KEYS.TAG);
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
		setIsBlockedItemsEnabled(
			(settings.blockedTags && settings.blockedTags.length > 0) ||
				(settings.blockedStates && settings.blockedStates.length > 0),
		);
		setIsBaselineEnabled(
			settings.processBehaviourChartBaselineStartDate != null &&
				settings.processBehaviourChartBaselineEndDate != null,
		);

		// Initialize probability input value with current setting
		setProbabilityInputValue(
			settings.serviceLevelExpectationProbability.toString(),
		);
	}, [settings, showFeatureWip]);

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

	const handleAddBlockedTag = (tag: string) => {
		if (tag.trim()) {
			const newTags = [...(settings.blockedTags || []), tag.trim()];
			onSettingsChange("blockedTags" as keyof T, newTags);
		}
	};

	const handleRemoveBlockedTag = (tag: string) => {
		const newTags = (settings.blockedTags || []).filter((item) => item !== tag);
		onSettingsChange("blockedTags" as keyof T, newTags);
	};

	const handleAddBlockedState = (state: string) => {
		if (state.trim()) {
			const newStates = [...(settings.blockedStates || []), state.trim()];
			onSettingsChange("blockedStates" as keyof T, newStates);
		}
	};

	const handleRemoveBlockedState = (state: string) => {
		const newStates = (settings.blockedStates || []).filter(
			(item) => item !== state,
		);
		onSettingsChange("blockedStates" as keyof T, newStates);
	};

	const handleBlockedItemsEnableChange = (checked: boolean) => {
		setIsBlockedItemsEnabled(checked);

		if (!checked) {
			// Clear both blocked tags and blocked states when disabled
			onSettingsChange("blockedTags" as keyof T, []);
			onSettingsChange("blockedStates" as keyof T, []);
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

	// Get only "Doing" states for blocked states suggestions
	const doingStatesSuggestions = settings.doingStates ?? [];

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

			{/* Blocked Tags Configuration */}
			{isBlockedItemsEnabled && (
				<Grid size={{ xs: 12 }}>
					<InputGroup
						title={`${blockedTerm} ${tagTerm}s`}
						initiallyExpanded={true}
					>
						<Grid size={{ xs: 12 }}>
							<ItemListManager
								title={`${blockedTerm} ${tagTerm}`}
								items={settings.blockedTags || []}
								onAddItem={handleAddBlockedTag}
								onRemoveItem={handleRemoveBlockedTag}
								suggestions={[]}
								isLoading={false}
							/>
						</Grid>
					</InputGroup>
				</Grid>
			)}

			{/* Blocked States Configuration */}
			{isBlockedItemsEnabled && (
				<Grid size={{ xs: 12 }}>
					<InputGroup title={`${blockedTerm} States`} initiallyExpanded={false}>
						<Grid size={{ xs: 12 }}>
							<Box
								sx={{
									display: "flex",
									alignItems: "center",
									mb: 2,
									color: "warning.main",
									padding: 1,
									borderRadius: 1,
								}}
							>
								<WarningIcon sx={{ mr: 1, fontSize: 20 }} />
								<Typography variant="body2">
									We do not recommend using states for identifying blocked
									items. Preferably you use Tags.
								</Typography>
							</Box>
							<ItemListManager
								title={`${blockedTerm} State`}
								items={settings.blockedStates || []}
								onAddItem={handleAddBlockedState}
								onRemoveItem={handleRemoveBlockedState}
								suggestions={doingStatesSuggestions}
								isLoading={false}
							/>
						</Grid>
					</InputGroup>
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

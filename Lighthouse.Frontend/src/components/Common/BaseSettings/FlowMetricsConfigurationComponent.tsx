import { Checkbox, FormControlLabel, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import { useEffect, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import InputGroup from "../InputGroup/InputGroup";

interface FlowMetricsConfigurationComponentProps<T extends IBaseSettings> {
	settings: T;
	onSettingsChange: (key: keyof T, value: number | boolean) => void;
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
	}, [settings, showFeatureWip]);

	const handleWipLimitEnableChange = (checked: boolean) => {
		setIsWipLimitEnabled(checked);

		if (!checked) {
			onSettingsChange("systemWIPLimit", 0);
		} else {
			onSettingsChange("systemWIPLimit", 1);
		}
	};

	const handleWipLimitChange = (value: string) => {
		const newLimit = Number.parseInt(value, 10);
		onSettingsChange("systemWIPLimit", newLimit);
	};

	const handleFeatureWipEnableChange = (checked: boolean) => {
		setIsFeatureWipEnabled(checked);

		if ("featureWIP" in settings) {
			if (!checked) {
				onSettingsChange("featureWIP" as keyof T, 0);
				onSettingsChange("automaticallyAdjustFeatureWIP" as keyof T, false);
			} else {
				onSettingsChange("featureWIP" as keyof T, 1);
			}
		}
	};

	const handleFeatureWipChange = (value: string) => {
		const newFeatureWip = Number.parseInt(value, 10);

		if ("featureWIP" in settings) {
			onSettingsChange("featureWIP" as keyof T, newFeatureWip);
		}
	};

	const handleSleEnableChange = (checked: boolean) => {
		setIsSleEnabled(checked);

		if (checked) {
			onSettingsChange("serviceLevelExpectationProbability", 70);
			onSettingsChange("serviceLevelExpectationRange", 10);
		} else {
			onSettingsChange("serviceLevelExpectationProbability", 0);
			onSettingsChange("serviceLevelExpectationRange", 0);
		}
	};

	const handleProbabilityChange = (value: string) => {
		const numValue = Number.parseInt(value, 10);
		onSettingsChange("serviceLevelExpectationProbability", numValue);
	};

	const handleRangeChange = (value: string) => {
		const numValue = Number.parseInt(value, 10);
		onSettingsChange("serviceLevelExpectationRange", numValue);
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
					label="Set System WIP Limit"
				/>
			</Grid>
			{isWipLimitEnabled && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label="WIP Limit"
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
							label="Set Feature WIP"
						/>
					</Grid>
					{isFeatureWipEnabled && (
						<>
							<Grid size={{ xs: 12 }}>
								<TextField
									label="Feature WIP"
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
									label="Automatically Adjust Feature WIP based on actual WIP"
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
					label="Set Service Level Expectation"
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
							value={settings.serviceLevelExpectationProbability}
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
		</InputGroup>
	);
};

export default FlowMetricsConfigurationComponent;

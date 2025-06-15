import { Checkbox, FormControlLabel, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import { useCallback, useEffect, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import InputGroup from "../InputGroup/InputGroup";

interface FlowMetricsConfigurationComponentProps<T extends IBaseSettings> {
	settings: T | null;
	onSettingsChange: <K extends keyof T>(key: K, value: T[K]) => void;
}

const FlowMetricsConfigurationComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
}: FlowMetricsConfigurationComponentProps<T>) => {
	// SLE states
	const [isSleEnabled, setIsSleEnabled] = useState(false);
	const [inputProbability, setInputProbability] = useState<string>("");
	const [inputRange, setInputRange] = useState<string>("");

	// WIP Limit states
	const [isWipLimitEnabled, setIsWipLimitEnabled] = useState(false);
	const [inputWipLimit, setInputWipLimit] = useState<string>("");

	// Initialize WIP Limit state
	useEffect(() => {
		const isCurrentlyEnabled = Boolean((settings?.systemWipLimit ?? 0) > 0);
		setIsWipLimitEnabled(isCurrentlyEnabled);
		setInputWipLimit(String(settings?.systemWipLimit ?? 0));
	}, [settings?.systemWipLimit]);

	// Initialize SLE states
	useEffect(() => {
		const isCurrentlyEnabled = Boolean(
			(settings?.serviceLevelExpectationProbability ?? 0) > 0 &&
				(settings?.serviceLevelExpectationRange ?? 0) > 0,
		);
		setIsSleEnabled(isCurrentlyEnabled);
		setInputProbability(
			String(settings?.serviceLevelExpectationProbability ?? 0),
		);
		setInputRange(String(settings?.serviceLevelExpectationRange ?? 0));
	}, [
		settings?.serviceLevelExpectationProbability,
		settings?.serviceLevelExpectationRange,
	]);

	// WIP Limit handlers
	const handleWipLimitEnableChange = (checked: boolean) => {
		setIsWipLimitEnabled(checked);

		if (checked) {
			setInputWipLimit("5");
			onSettingsChange("systemWipLimit" as keyof T, 5 as T[keyof T]);
		} else {
			setInputWipLimit("0");
			onSettingsChange("systemWipLimit" as keyof T, 0 as T[keyof T]);
		}
	};

	const handleWipLimitChange = (value: string) => {
		setInputWipLimit(value);
	};

	const debouncedValidateWipLimit = useCallback(
		(value: string) => {
			const numValue = Number.parseInt(value, 10);

			if (!Number.isNaN(numValue)) {
				let validValue = numValue;

				if (isWipLimitEnabled && numValue < 1) validValue = 1;

				onSettingsChange("systemWipLimit" as keyof T, validValue as T[keyof T]);
			}
		},
		[onSettingsChange, isWipLimitEnabled],
	);

	// SLE handlers
	const handleSleEnableChange = (checked: boolean) => {
		setIsSleEnabled(checked);

		if (checked) {
			setInputProbability("80");
			setInputRange("10");
			onSettingsChange(
				"serviceLevelExpectationProbability" as keyof T,
				80 as T[keyof T],
			);
			onSettingsChange(
				"serviceLevelExpectationRange" as keyof T,
				10 as T[keyof T],
			);
		} else {
			setInputProbability("0");
			setInputRange("0");
			onSettingsChange(
				"serviceLevelExpectationProbability" as keyof T,
				0 as T[keyof T],
			);
			onSettingsChange(
				"serviceLevelExpectationRange" as keyof T,
				0 as T[keyof T],
			);
		}
	};

	const handleProbabilityChange = (value: string) => {
		setInputProbability(value);
	};

	const handleRangeChange = (value: string) => {
		setInputRange(value);
	};

	const debouncedValidateProbability = useCallback(
		(value: string) => {
			const numValue = Number.parseInt(value, 10);

			if (!Number.isNaN(numValue)) {
				let validValue = numValue;

				if (isSleEnabled) {
					// Validate only if enabled
					if (numValue < 50) validValue = 50;
					if (numValue > 95) validValue = 95;
				}

				onSettingsChange(
					"serviceLevelExpectationProbability" as keyof T,
					validValue as T[keyof T],
				);
			}
		},
		[onSettingsChange, isSleEnabled],
	);

	const debouncedValidateRange = useCallback(
		(value: string) => {
			const numValue = Number.parseInt(value, 10);

			if (!Number.isNaN(numValue)) {
				let validValue = numValue;

				if (isSleEnabled && numValue < 1) validValue = 1;

				onSettingsChange(
					"serviceLevelExpectationRange" as keyof T,
					validValue as T[keyof T],
				);
			}
		},
		[onSettingsChange, isSleEnabled],
	);

	// Debounced validation effects
	useEffect(() => {
		const timer = setTimeout(() => {
			if (inputWipLimit) {
				debouncedValidateWipLimit(inputWipLimit);
			}
		}, 1000); // 1-second delay

		return () => clearTimeout(timer);
	}, [inputWipLimit, debouncedValidateWipLimit]);

	useEffect(() => {
		const timer = setTimeout(() => {
			if (inputProbability) {
				debouncedValidateProbability(inputProbability);
			}
		}, 1000); // 1-second delay

		return () => clearTimeout(timer);
	}, [inputProbability, debouncedValidateProbability]);

	useEffect(() => {
		const timer = setTimeout(() => {
			if (inputRange) {
				debouncedValidateRange(inputRange);
			}
		}, 1000); // 1-second delay

		return () => clearTimeout(timer);
	}, [inputRange, debouncedValidateRange]);

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
						value={inputWipLimit}
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
							value={inputProbability}
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
							value={inputRange}
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

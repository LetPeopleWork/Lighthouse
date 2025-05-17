import { Checkbox, FormControlLabel, TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import { useCallback, useEffect, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import InputGroup from "../InputGroup/InputGroup";

interface ServiceLevelExpectationConfigurationComponentProps<
	T extends IBaseSettings,
> {
	settings: T | null;
	onSettingsChange: <K extends keyof T>(key: K, value: T[K]) => void;
}

const ServiceLevelExpectationConfigurationComponent = <
	T extends IBaseSettings,
>({
	settings,
	onSettingsChange,
}: ServiceLevelExpectationConfigurationComponentProps<T>) => {
	const [isEnabled, setIsEnabled] = useState(false);

	useEffect(() => {
		const isCurrentlyEnabled = Boolean(
			(settings?.serviceLevelExpectationProbability ?? 0) > 0 &&
				(settings?.serviceLevelExpectationRange ?? 0) > 0,
		);
		setIsEnabled(isCurrentlyEnabled);
	}, [
		settings?.serviceLevelExpectationProbability,
		settings?.serviceLevelExpectationRange,
	]);

	const handleEnableChange = (checked: boolean) => {
		setIsEnabled(checked);

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

	const [inputProbability, setInputProbability] = useState<string>("");
	const [inputRange, setInputRange] = useState<string>("");

	useEffect(() => {
		setInputProbability(
			String(settings?.serviceLevelExpectationProbability ?? 0),
		);
		setInputRange(String(settings?.serviceLevelExpectationRange ?? 0));
	}, [
		settings?.serviceLevelExpectationProbability,
		settings?.serviceLevelExpectationRange,
	]);

	const debouncedValidateProbability = useCallback(
		(value: string) => {
			const numValue = Number.parseInt(value, 10);

			if (!Number.isNaN(numValue)) {
				let validValue = numValue;

				if (isEnabled) {
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
		[onSettingsChange, isEnabled],
	);

	const debouncedValidateRange = useCallback(
		(value: string) => {
			const numValue = Number.parseInt(value, 10);

			if (!Number.isNaN(numValue)) {
				let validValue = numValue;

				if (isEnabled && numValue < 1) validValue = 1;

				onSettingsChange(
					"serviceLevelExpectationRange" as keyof T,
					validValue as T[keyof T],
				);
			}
		},
		[onSettingsChange, isEnabled],
	);

	const handleProbabilityChange = (value: string) => {
		setInputProbability(value);
	};

	const handleRangeChange = (value: string) => {
		setInputRange(value);
	};

	// Use debounced validation effects
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
		<InputGroup title={"Service Level Expectation"} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Checkbox
							checked={isEnabled}
							onChange={(e) => handleEnableChange(e.target.checked)}
						/>
					}
					label="Set Service Level Expectation"
				/>
			</Grid>
			{isEnabled && (
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

export default ServiceLevelExpectationConfigurationComponent;

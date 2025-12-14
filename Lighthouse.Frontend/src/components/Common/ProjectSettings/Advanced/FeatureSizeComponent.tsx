import { FormControlLabel, Switch, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import InputGroup from "../../InputGroup/InputGroup";
import ItemListManager from "../../ItemListManager/ItemListManager";

interface FeatureSizeComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: string | number | boolean | string[],
	) => void;
}

const FeatureSizeComponent: React.FC<FeatureSizeComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
}) => {
	const [statesSuggestions, setStatesSuggestions] = useState<string[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const { suggestionService } = useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		const fetchStates = async () => {
			setIsLoading(true);
			try {
				const availableStates = await suggestionService.getStatesForProjects();
				const allStates = [
					...(availableStates.toDoStates || []),
					...(availableStates.doingStates || []),
					...(availableStates.doneStates || []),
				];

				setStatesSuggestions(Array.from(new Set(allStates)));
			} catch (error) {
				console.error("Failed to fetch states:", error);
			} finally {
				setIsLoading(false);
			}
		};

		fetchStates();
	}, [suggestionService]);

	const handleAddOverrideChildCountState = (
		overrideChildCountState: string,
	) => {
		if (overrideChildCountState.trim()) {
			const newStates = projectSettings
				? [
						...(projectSettings.overrideRealChildCountStates || []),
						overrideChildCountState.trim(),
					]
				: [overrideChildCountState.trim()];

			onProjectSettingsChange("overrideRealChildCountStates", newStates);
		}
	};

	const handleRemoveOverrideChildCountState = (
		overrideChildCountState: string,
	) => {
		const newStates = projectSettings
			? (projectSettings.overrideRealChildCountStates || []).filter(
					(item) => item !== overrideChildCountState,
				)
			: [];

		onProjectSettingsChange("overrideRealChildCountStates", newStates);
	};

	return (
		<InputGroup title={`Default ${featureTerm} Size`} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormControlLabel
					control={
						<Switch
							checked={
								projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems
							}
							onChange={(e) =>
								onProjectSettingsChange(
									"usePercentileToCalculateDefaultAmountOfWorkItems",
									e.target.checked,
								)
							}
						/>
					}
					label={`Use Historical ${featureTerm} Size To Calculate Default`}
				/>
			</Grid>

			{projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems ? (
				<>
					<Grid size={{ xs: 12 }}>
						<TextField
							label={`${featureTerm} Size Percentile`}
							type="number"
							fullWidth
							margin="normal"
							value={projectSettings?.defaultWorkItemPercentile || ""}
							slotProps={{
								htmlInput: {
									max: 95,
									min: 50,
								},
							}}
							onChange={(e) =>
								onProjectSettingsChange(
									"defaultWorkItemPercentile",
									Number.parseInt(e.target.value, 10),
								)
							}
						/>
					</Grid>
					<Grid size={{ xs: 12 }}>
						<TextField
							label={`History in Days`}
							type="number"
							fullWidth
							margin="normal"
							value={projectSettings?.percentileHistoryInDays || "90"}
							slotProps={{
								htmlInput: {
									min: 30,
								},
							}}
							onChange={(e) =>
								onProjectSettingsChange(
									"percentileHistoryInDays",
									e.target.value,
								)
							}
						/>
					</Grid>
				</>
			) : (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={`Default Number of ${workItemsTerm} per ${featureTerm}`}
						type="number"
						fullWidth
						margin="normal"
						value={projectSettings?.defaultAmountOfWorkItemsPerFeature ?? ""}
						onChange={(e) =>
							onProjectSettingsChange(
								"defaultAmountOfWorkItemsPerFeature",
								Number.parseInt(e.target.value, 10),
							)
						}
					/>
				</Grid>
			)}

			<Grid size={{ xs: 12 }}>
				<TextField
					label="Size Estimate Field"
					fullWidth
					margin="normal"
					value={projectSettings?.sizeEstimateField ?? ""}
					onChange={(e) =>
						onProjectSettingsChange("sizeEstimateField", e.target.value)
					}
				/>
			</Grid>

			<Grid size={{ xs: 12 }}>
				<Typography variant="body1">
					{`Use Default Size instead of real Child ${workItemsTerm} for ${featuresTerm} in these
					States:`}
				</Typography>
				<ItemListManager
					title="Size Override State"
					items={projectSettings?.overrideRealChildCountStates ?? []}
					onAddItem={handleAddOverrideChildCountState}
					onRemoveItem={handleRemoveOverrideChildCountState}
					suggestions={statesSuggestions}
					isLoading={isLoading}
				/>
			</Grid>
		</InputGroup>
	);
};

export default FeatureSizeComponent;

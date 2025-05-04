import { FormControlLabel, Switch, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import ItemListManager from "../../../components/Common/ItemListManager/ItemListManager";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface AdvancedInputsComponentProps {
	projectSettings: IProjectSettings | null;
	onProjectSettingsChange: (
		key: keyof IProjectSettings,
		value: string | number | boolean | string[],
	) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
}) => {
	const [statesSuggestions, setStatesSuggestions] = useState<string[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const { suggestionService } = useContext(ApiServiceContext);

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
		<>
			<InputGroup title={"Unparented Work Items"} initiallyExpanded={false}>
				<Grid size={{ xs: 12 }}>
					<TextField
						label="Unparented Work Items Query"
						fullWidth
						multiline
						rows={4}
						margin="normal"
						value={projectSettings?.unparentedItemsQuery ?? ""}
						onChange={(e) =>
							onProjectSettingsChange("unparentedItemsQuery", e.target.value)
						}
					/>
				</Grid>
			</InputGroup>

			<InputGroup title={"Default Feature Size"} initiallyExpanded={false}>
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
						label="Use Historical Feature Size To Calculate Default"
					/>
				</Grid>

				{projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems ? (
					<>
						<Grid size={{ xs: 12 }}>
							<TextField
								label="Feature Size Percentile"
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
								label="Historical Features Work Item Query"
								fullWidth
								multiline
								rows={4}
								margin="normal"
								value={projectSettings?.historicalFeaturesWorkItemQuery || ""}
								onChange={(e) =>
									onProjectSettingsChange(
										"historicalFeaturesWorkItemQuery",
										e.target.value,
									)
								}
							/>
						</Grid>
					</>
				) : (
					<Grid size={{ xs: 12 }}>
						<TextField
							label="Default Number of Items per Feature"
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
						Use Default Size instead of real Child Items for Features in these
						States:
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
		</>
	);
};

export default AdvancedInputsComponent;

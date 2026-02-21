import { FormControlLabel, Switch, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import FormSelectField from "../FormSelectField/FormSelectField";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface EstimationFieldComponentProps {
	estimationFieldDefinitionId: number | null;
	onEstimationFieldChange: (value: number | null) => void;
	estimationUnit?: string | null;
	onEstimationUnitChange?: (value: string) => void;
	useNonNumericEstimation?: boolean;
	onUseNonNumericEstimationChange?: (value: boolean) => void;
	estimationCategoryValues?: string[];
	onAddCategoryValue?: (value: string) => void;
	onRemoveCategoryValue?: (value: string) => void;
	onReorderCategoryValues?: (values: string[]) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const EstimationFieldComponent: React.FC<EstimationFieldComponentProps> = ({
	estimationFieldDefinitionId,
	onEstimationFieldChange,
	estimationUnit,
	onEstimationUnitChange,
	useNonNumericEstimation = false,
	onUseNonNumericEstimationChange,
	estimationCategoryValues = [],
	onAddCategoryValue,
	onRemoveCategoryValue,
	onReorderCategoryValues,
	additionalFieldDefinitions = [],
}) => {
	return (
		<InputGroup title="Estimation" initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<FormSelectField
					label="Estimation Field"
					value={estimationFieldDefinitionId ?? ""}
					onChange={(value) => {
						onEstimationFieldChange(value);
					}}
					options={additionalFieldDefinitions.map((field) => ({
						id: field.id,
						label: field.displayName,
					}))}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Estimation Unit"
					fullWidth
					margin="normal"
					value={estimationUnit ?? ""}
					onChange={(e) => onEstimationUnitChange?.(e.target.value)}
					helperText='Optional label for chart axes (e.g., "Points", "Days", "T-Shirt")'
				/>
			</Grid>
			{onUseNonNumericEstimationChange && (
				<Grid size={{ xs: 12 }}>
					<FormControlLabel
						control={
							<Switch
								checked={useNonNumericEstimation}
								onChange={(e) =>
									onUseNonNumericEstimationChange(e.target.checked)
								}
							/>
						}
						label="Use Non-Numeric Estimation"
					/>
				</Grid>
			)}
			{useNonNumericEstimation &&
				onAddCategoryValue &&
				onRemoveCategoryValue && (
					<Grid size={{ xs: 12 }}>
						<Typography variant="body1">Estimation Categories</Typography>
						<ItemListManager
							title="Estimation Category"
							items={estimationCategoryValues}
							onAddItem={onAddCategoryValue}
							onRemoveItem={onRemoveCategoryValue}
							onReorderItems={onReorderCategoryValues}
						/>
					</Grid>
				)}
		</InputGroup>
	);
};

export default EstimationFieldComponent;

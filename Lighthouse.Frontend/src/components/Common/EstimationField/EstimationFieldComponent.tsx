import Grid from "@mui/material/Grid";
import type React from "react";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import FormSelectField from "../FormSelectField/FormSelectField";
import InputGroup from "../InputGroup/InputGroup";

interface EstimationFieldComponentProps {
	estimationFieldDefinitionId: number | null;
	onEstimationFieldChange: (value: number | null) => void;
	additionalFieldDefinitions?: IAdditionalFieldDefinition[];
}

const EstimationFieldComponent: React.FC<EstimationFieldComponentProps> = ({
	estimationFieldDefinitionId,
	onEstimationFieldChange,
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
		</InputGroup>
	);
};

export default EstimationFieldComponent;

import { FormControlLabel, Switch, TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type {
	EstimationDefaultName,
	IWorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ConnectionDefaultFieldSelect from "../ConnectionDefault/ConnectionDefaultFieldSelect";
import { followsTheConnection } from "../ConnectionDefault/connectionDefaultEntry";
import FormSelectField from "../FormSelectField/FormSelectField";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

/** A Team or Portfolio whose Estimation can follow its connection's default instead of its own. */
interface FollowableEstimation {
	settings: IBaseSettings | null;
	connection: IWorkTrackingSystemConnection | null | undefined;
	theDefault: EstimationDefaultName;
	onFollow: () => void;
	onOwn: (field: number | null) => void;
}

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
	/** Shown in place, without a group of its own, and only its field until a field is chosen. */
	inline?: boolean;
	followable?: FollowableEstimation;
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
	inline = false,
	followable,
}) => {
	// While the connection's default is followed, the unit and categories are the default's, not this one's.
	const follows =
		!!followable &&
		followsTheConnection(
			followable.settings,
			followable.connection,
			followable.theDefault,
			followable.settings?.estimationSource,
		);
	const showsTheDetails =
		!follows && (!inline || estimationFieldDefinitionId !== null);

	const fieldSelect = followable ? (
		<ConnectionDefaultFieldSelect
			label="Estimation Field"
			settings={followable.settings}
			connection={followable.connection}
			theDefault={followable.theDefault}
			defaultNameTheSettingsCarry={followable.settings?.estimationDefaultName}
			source={followable.settings?.estimationSource}
			ownField={estimationFieldDefinitionId}
			additionalFieldDefinitions={additionalFieldDefinitions}
			onFollow={followable.onFollow}
			onOwn={followable.onOwn}
		/>
	) : (
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
	);

	const editor = (
		<>
			<Grid size={{ xs: 12 }}>{fieldSelect}</Grid>
			{showsTheDetails && (
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
			)}
			{showsTheDetails && onUseNonNumericEstimationChange && (
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
			{showsTheDetails &&
				useNonNumericEstimation &&
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
		</>
	);

	return inline ? (
		editor
	) : (
		<InputGroup title="Estimation" initiallyExpanded={false}>
			{editor}
		</InputGroup>
	);
};

export default EstimationFieldComponent;

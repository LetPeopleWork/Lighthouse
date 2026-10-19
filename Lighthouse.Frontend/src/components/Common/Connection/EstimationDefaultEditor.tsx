import type React from "react";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IEstimationDefault } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import EstimationFieldComponent from "../EstimationField/EstimationFieldComponent";

const NoEstimation: IEstimationDefault = {
	additionalFieldDefinitionId: null,
	unit: null,
	useNonNumericEstimation: false,
	estimationCategoryValues: [],
};

interface EstimationDefaultEditorProps {
	bundle: IEstimationDefault | null | undefined;
	/** The field shown as chosen, which is null when the bundle's field is no longer on the connection. */
	chosenField: number | null;
	additionalFields: IAdditionalFieldDefinition[];
	onChange: (bundle: IEstimationDefault | null) => void;
}

/** A connection's Estimation default, edited with the same editor a Team or Portfolio uses for its own. */
const EstimationDefaultEditor: React.FC<EstimationDefaultEditorProps> = ({
	bundle: held,
	chosenField,
	additionalFields,
	onChange,
}) => {
	const bundle = held ?? NoEstimation;
	const change = (part: Partial<IEstimationDefault>) =>
		onChange({ ...bundle, ...part });

	return (
		<EstimationFieldComponent
			inline
			estimationFieldDefinitionId={chosenField}
			onEstimationFieldChange={(fieldId) =>
				fieldId === null
					? onChange(null)
					: change({ additionalFieldDefinitionId: fieldId })
			}
			estimationUnit={bundle.unit}
			onEstimationUnitChange={(unit) => change({ unit: unit || null })}
			useNonNumericEstimation={bundle.useNonNumericEstimation}
			onUseNonNumericEstimationChange={(useNonNumericEstimation) =>
				change({ useNonNumericEstimation })
			}
			estimationCategoryValues={bundle.estimationCategoryValues}
			onAddCategoryValue={(value) =>
				change({
					estimationCategoryValues: [
						...bundle.estimationCategoryValues,
						value.trim(),
					],
				})
			}
			onRemoveCategoryValue={(value) =>
				change({
					estimationCategoryValues: bundle.estimationCategoryValues.filter(
						(kept) => kept !== value,
					),
				})
			}
			onReorderCategoryValues={(estimationCategoryValues) =>
				change({ estimationCategoryValues })
			}
			additionalFieldDefinitions={additionalFields}
		/>
	);
};

export default EstimationDefaultEditor;

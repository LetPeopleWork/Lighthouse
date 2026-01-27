import type React from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	SingleWipTextField,
	useWipDialogState,
	useWipSaveHandlers,
	WipSettingDialog,
	WipSettingIconButton,
} from "./WipSettingDialog";

type FeatureWipQuickSettingProps = {
	featureWip: number;
	onSave: (featureWip: number) => Promise<void>;
	disabled?: boolean;
};

const FeatureWipQuickSetting: React.FC<FeatureWipQuickSettingProps> = ({
	featureWip: initialFeatureWip,
	onSave,
	disabled = false,
}) => {
	const { getTerm } = useTerminology();

	const {
		open,
		value: featureWip,
		setValue: setFeatureWip,
		handleOpen,
		handleClose,
	} = useWipDialogState({ initialValue: initialFeatureWip });

	const { handleKeyDown, handleDialogClose } = useWipSaveHandlers({
		currentValue: featureWip,
		initialValue: initialFeatureWip,
		onSave,
		onClose: handleClose,
	});

	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);

	const getItemTypeTerm = (count: number): string => {
		return count === 1 ? featureTerm : featuresTerm;
	};

	const getTooltipText = (): string => {
		if (initialFeatureWip <= 0) {
			return `${featureTerm} ${wipTerm}: Not set`;
		}
		return `${featureTerm} ${wipTerm}: ${initialFeatureWip} ${getItemTypeTerm(initialFeatureWip)}`;
	};

	return (
		<>
			<WipSettingIconButton
				tooltipText={getTooltipText()}
				isUnset={initialFeatureWip <= 0}
				disabled={disabled}
				onClick={() => handleOpen(disabled)}
			/>
			<WipSettingDialog
				open={open}
				onClose={handleDialogClose}
				title={`${featureTerm} ${wipTerm} Limit`}
				onKeyDown={handleKeyDown}
			>
				<SingleWipTextField
					label={`${featureTerm} ${wipTerm}`}
					value={featureWip}
					onChange={setFeatureWip}
				/>
			</WipSettingDialog>
		</>
	);
};

export default FeatureWipQuickSetting;

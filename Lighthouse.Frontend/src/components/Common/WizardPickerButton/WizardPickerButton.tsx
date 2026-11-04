import { Button, type ButtonProps, Tooltip } from "@mui/material";
import type React from "react";
import {
	type IDataRetrievalWizard,
	isWizardUsable,
} from "../../../models/DataRetrievalWizard/DataRetrievalWizard";

interface WizardPickerButtonProps {
	wizard: IDataRetrievalWizard;
	authenticationMethodKey: string | undefined;
	onClick: () => void;
	variant: ButtonProps["variant"];
	sx?: ButtonProps["sx"];
}

const WizardPickerButton: React.FC<WizardPickerButtonProps> = ({
	wizard,
	authenticationMethodKey,
	onClick,
	variant,
	sx,
}) => {
	if (isWizardUsable(wizard, authenticationMethodKey)) {
		return (
			<Button variant={variant} onClick={onClick} sx={sx}>
				{wizard.name}
			</Button>
		);
	}

	// A disabled button fires no pointer events, so the tooltip sits on a wrapper.
	return (
		<Tooltip title={wizard.unservedSignIns?.reason ?? ""}>
			<span style={{ display: "inline-block" }}>
				<Button variant={variant} disabled sx={sx}>
					{wizard.name}
				</Button>
			</span>
		</Tooltip>
	);
};

export default WizardPickerButton;

import type React from "react";

export interface ReloadDependentDataActionProps {
	visible: boolean;
	label: string;
	onReload: () => void;
}

const NOT_IMPLEMENTED =
	"ReloadDependentDataAction not yet implemented — RED scaffold (DISTILL)";

const ReloadDependentDataAction: React.FC<
	ReloadDependentDataActionProps
> = () => {
	throw new Error(NOT_IMPLEMENTED);
};

export default ReloadDependentDataAction;

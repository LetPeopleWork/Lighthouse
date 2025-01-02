import type React from "react";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface WorkItemTypesComponentProps {
	workItemTypes: string[];
	onAddWorkItemType: (type: string) => void;
	onRemoveWorkItemType: (type: string) => void;
}

const WorkItemTypesComponent: React.FC<WorkItemTypesComponentProps> = ({
	workItemTypes,
	onAddWorkItemType,
	onRemoveWorkItemType,
}) => {
	return (
		<InputGroup title={"Work Item Types"}>
			<ItemListManager
				title="Work Item Type"
				items={workItemTypes}
				onAddItem={onAddWorkItemType}
				onRemoveItem={onRemoveWorkItemType}
			/>
		</InputGroup>
	);
};

export default WorkItemTypesComponent;

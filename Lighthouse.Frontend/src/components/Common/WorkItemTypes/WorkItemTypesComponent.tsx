import type React from "react";
import { useContext, useEffect, useState } from "react";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface WorkItemTypesComponentProps {
	workItemTypes: string[];
	onAddWorkItemType: (type: string) => void;
	onRemoveWorkItemType: (type: string) => void;
	isForTeam?: boolean;
}

const WorkItemTypesComponent: React.FC<WorkItemTypesComponentProps> = ({
	workItemTypes,
	onAddWorkItemType,
	onRemoveWorkItemType,
	isForTeam = true,
}) => {
	const [suggestions, setSuggestions] = useState<string[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const { suggestionService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchWorkItemTypes = async () => {
			setIsLoading(true);
			try {
				let availableTypes: string[];
				if (isForTeam) {
					availableTypes = await suggestionService.getWorkItemTypesForTeams();
				} else {
					availableTypes =
						await suggestionService.getWorkItemTypesForProjects();
				}
				setSuggestions(availableTypes);
			} catch (error) {
				console.error("Failed to fetch work item types:", error);
			} finally {
				setIsLoading(false);
			}
		};

		fetchWorkItemTypes();
	}, [suggestionService, isForTeam]);

	return (
		<InputGroup title={"Work Item Types"}>
			<ItemListManager
				title="Work Item Type"
				items={workItemTypes}
				onAddItem={onAddWorkItemType}
				onRemoveItem={onRemoveWorkItemType}
				suggestions={suggestions}
				isLoading={isLoading}
			/>
		</InputGroup>
	);
};

export default WorkItemTypesComponent;

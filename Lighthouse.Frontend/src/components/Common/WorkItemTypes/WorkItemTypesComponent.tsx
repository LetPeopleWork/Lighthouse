import type React from "react";
import { useContext, useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
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
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);

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
				console.error(`Failed to fetch ${workItemTerm} types:`, error);
			} finally {
				setIsLoading(false);
			}
		};

		fetchWorkItemTypes();
	}, [suggestionService, isForTeam, workItemTerm]);

	return (
		<InputGroup title={`${workItemTerm} Types`}>
			<ItemListManager
				title={`${workItemTerm} Type`}
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

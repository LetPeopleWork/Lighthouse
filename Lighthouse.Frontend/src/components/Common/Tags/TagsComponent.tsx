import { useContext, useEffect, useState } from "react";
import type React from "react";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface TagsComponentProps {
	tags: string[];
	onAddTag: (tag: string) => void;
	onRemoveTag: (tag: string) => void;
}

const TagsComponent: React.FC<TagsComponentProps> = ({
	tags,
	onAddTag,
	onRemoveTag,
}) => {
	const [suggestions, setSuggestions] = useState<string[]>([]);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const { suggestionService: tagService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchTags = async () => {
			setIsLoading(true);
			try {
				const availableTags = await tagService.getTags();
				setSuggestions(availableTags);
			} catch (error) {
				console.error("Failed to fetch tags:", error);
			} finally {
				setIsLoading(false);
			}
		};

		fetchTags();
	}, [tagService]);

	return (
		<InputGroup title={"Tags"}>
			<ItemListManager
				title="Tag"
				items={tags}
				onAddItem={onAddTag}
				onRemoveItem={onRemoveTag}
				suggestions={suggestions}
				isLoading={isLoading}
			/>
		</InputGroup>
	);
};

export default TagsComponent;

import { Box, Link } from "@mui/material";
import type React from "react";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import { getWorkItemName } from "../../../utils/featureName";

interface ParentWorkItemCellProps {
	parentReference: string | null | undefined;
	parentMap: Map<string, ParentWorkItem>;
}

export const NO_PARENT_TEXT = "No Parent";

/** The text a person reads for a parent: its title when known, else its reference, else "No Parent". */
export const parentWorkItemText = (
	parentReference: string | null | undefined,
	parentMap: Map<string, ParentWorkItem>,
): string => {
	if (!parentReference) return NO_PARENT_TEXT;
	const parentInfo = parentMap.get(parentReference);
	return parentInfo
		? getWorkItemName(parentInfo.name, parentInfo.referenceId)
		: parentReference;
};

const ParentWorkItemCell: React.FC<ParentWorkItemCellProps> = ({
	parentReference,
	parentMap,
}) => {
	if (!parentReference) {
		return <Box sx={{ color: "text.secondary" }}>{NO_PARENT_TEXT}</Box>;
	}

	const parentInfo = parentMap.get(parentReference);

	if (parentInfo) {
		return (
			<Box>
				<Link
					href={parentInfo.url}
					target="_blank"
					rel="noopener noreferrer"
					sx={{ textDecoration: "none" }}
				>
					{parentWorkItemText(parentReference, parentMap)}
				</Link>
			</Box>
		);
	}

	// If we have a reference but couldn't find the parent info, just show the reference
	return <Box>{parentReference}</Box>;
};

export default ParentWorkItemCell;

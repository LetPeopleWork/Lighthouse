import { Box, Link } from "@mui/material";
import type React from "react";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import { getWorkItemName } from "../../../utils/featureName";

interface ParentWorkItemCellProps {
	parentReference: string | null | undefined;
	parentMap: Map<string, ParentWorkItem>;
}

const ParentWorkItemCell: React.FC<ParentWorkItemCellProps> = ({
	parentReference,
	parentMap,
}) => {
	if (!parentReference) {
		return <Box sx={{ color: "text.secondary" }}>No Parent</Box>;
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
					{getWorkItemName(parentInfo.name, parentInfo.referenceId)}
				</Link>
			</Box>
		);
	}

	// If we have a reference but couldn't find the parent info, just show the reference
	return <Box>{parentReference}</Box>;
};

export default ParentWorkItemCell;

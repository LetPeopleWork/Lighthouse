import { Link, TableCell, TableRow } from "@mui/material";
import type React from "react";
import type {
	IRefinementRow,
	RefinementRowCategory,
} from "../../../../models/Refinement/Refinement";

const categoryLabels: Record<RefinementRowCategory, string> = {
	ToDo: "To Do",
	Doing: "Doing",
};

const formatAge = (days: number): string =>
	days === 1 ? "1 day" : `${days} days`;

interface RefinementRowProps {
	workItem: IRefinementRow;
}

const RefinementRow: React.FC<Readonly<RefinementRowProps>> = ({
	workItem,
}) => (
	<TableRow>
		<TableCell>
			{workItem.url ? (
				<Link href={workItem.url} target="_blank" rel="noopener noreferrer">
					{workItem.referenceId}
				</Link>
			) : (
				workItem.referenceId
			)}
		</TableCell>
		<TableCell>{workItem.name}</TableCell>
		<TableCell>{workItem.state}</TableCell>
		<TableCell>{categoryLabels[workItem.stateCategory]}</TableCell>
		<TableCell>
			{workItem.workItemAge === null ? null : formatAge(workItem.workItemAge)}
		</TableCell>
	</TableRow>
);

export default RefinementRow;

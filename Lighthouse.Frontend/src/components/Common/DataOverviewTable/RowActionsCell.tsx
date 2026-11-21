import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import InfoIcon from "@mui/icons-material/Info";
import { Box, IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import { Link, useNavigate } from "react-router";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import { reportFailedNavigation } from "../../../utils/navigation";
import RowActionsMenu from "./RowActionsMenu";

interface RowActionsCellProps {
	row: IFeatureOwner;
	api: "teams" | "portfolios";
	isMobile: boolean;
	isTablet: boolean;
	canEdit: boolean;
	canClone: boolean;
	canDelete: boolean;
	onClone: (item: IFeatureOwner) => void;
	onDelete: (item: IFeatureOwner) => void;
}

interface ActionIconButtonProps {
	label: string;
	isTablet: boolean;
	icon: React.ReactElement;
	to?: string;
	onClick?: () => void;
}

const ActionIconButton: React.FC<ActionIconButtonProps> = ({
	label,
	isTablet,
	icon,
	to,
	onClick,
}) => {
	const theme = useTheme();
	const linkProps = to ? { component: Link, to } : {};

	return (
		<Tooltip title={label}>
			<IconButton
				{...linkProps}
				onClick={onClick}
				size={isTablet ? "small" : "medium"}
				aria-label={label}
				sx={{
					color: theme.palette.primary.main,
					transition: "transform 0.2s",
					"&:hover": { transform: "scale(1.1)" },
				}}
			>
				{icon}
			</IconButton>
		</Tooltip>
	);
};

const RowActionsCell: React.FC<RowActionsCellProps> = ({
	row,
	api,
	isMobile,
	isTablet,
	canEdit,
	canClone,
	canDelete,
	onClone,
	onDelete,
}) => {
	const navigate = useNavigate();
	const iconSize = isTablet ? "small" : "medium";

	if (isMobile) {
		return (
			<RowActionsMenu
				rowName={row.name}
				onDetails={() => reportFailedNavigation(navigate(`/${api}/${row.id}`))}
				onEdit={
					canEdit
						? () => reportFailedNavigation(navigate(`/${api}/edit/${row.id}`))
						: undefined
				}
				onClone={canClone ? () => onClone(row) : undefined}
				onDelete={canDelete ? () => onDelete(row) : undefined}
			/>
		);
	}

	return (
		<Box
			sx={{
				display: "flex",
				justifyContent: "flex-end",
				gap: isTablet ? 0 : 1,
				width: "100%",
			}}
		>
			<ActionIconButton
				label="Details"
				isTablet={isTablet}
				to={`/${api}/${row.id}`}
				icon={<InfoIcon fontSize={iconSize} />}
			/>
			{canEdit && (
				<ActionIconButton
					label="Edit"
					isTablet={isTablet}
					to={`/${api}/edit/${row.id}`}
					icon={<EditIcon fontSize={iconSize} />}
				/>
			)}
			{canClone && (
				<ActionIconButton
					label="Clone"
					isTablet={isTablet}
					onClick={() => onClone(row)}
					icon={<ContentCopyIcon fontSize={iconSize} />}
				/>
			)}
			{canDelete && (
				<ActionIconButton
					label="Delete"
					isTablet={isTablet}
					onClick={() => onDelete(row)}
					icon={
						<DeleteIcon fontSize={iconSize} data-testid="delete-item-button" />
					}
				/>
			)}
		</Box>
	);
};

export default RowActionsCell;
